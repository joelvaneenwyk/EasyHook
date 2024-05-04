// EasyHook (File: EasyHook\AsyncStreamReader.cs)
//
// Copyright (c) 2009 Christoph Husse & Copyright (c) 2015 Justin Stenning
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// Please visit https://easyhook.github.io for more information
// about the project and latest updates.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace EasyHook
{
    /// <summary>
    /// Custom callback for every time we get a line from process output.
    /// </summary>
    /// <param name="data"></param>
    public delegate void UserCallBack(string data);

    /// <summary>
    /// Asynchronously read from and cache data from a stream.
    /// </summary>
    public class RemoteAsyncStreamReader : IDisposable
    {
        /// <summary>
        /// Byte buffer size.
        /// </summary>
        internal const int DefaultBufferSize = 1024;

        /// <summary>
        /// Record the number of valid bytes in the byteBuffer, for a few checks.
        /// </summary>
        private const int MinBufferSize = 128;

        /// <summary>
        /// This is the maximum number of chars we can get from one call to
        /// ReadBuffer.  Used so ReadBuffer can tell when to copy data into
        /// a user's char[] directly, instead of our internal char[].
        /// </summary>
        private int _maxCharsPerBuffer;

        private bool bLastCarriageReturn;
        private byte[] byteBuffer;

        // Internal Cancel operation
        private bool cancelOperation;
        private char[] charBuffer;

        // Cache the last position scanned in sb when searching for lines.
        private int currentLinePos;
        private Decoder decoder;

        /// <summary>
        /// Default here to signaled because there is nothing to wait for. Only reset
        /// to false if we have something to wait for.
        /// </summary>
        private readonly ManualResetEvent _endStreamEvent = new ManualResetEvent(true);

        private readonly Queue<string> _messageEventQueue = new Queue<string>();

        private StringBuilder sb;

        private readonly Stream stream;

        /// <summary>
        /// Delegate to call user function.
        /// </summary>
        private readonly UserCallBack userCallBack;

        internal RemoteAsyncStreamReader(
            Stream stream, UserCallBack callback, Encoding encoding)
            : this(stream, callback, encoding, DefaultBufferSize)
        {
        }

        /// <summary>
        /// Creates a new AsyncStreamReader for the given stream.  The
        /// character encoding is set by encoding and the buffer size,
        /// in number of 16-bit characters, is set by bufferSize.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="callback"></param>
        /// <param name="encoding"></param>
        /// <param name="bufferSize"></param>
        internal RemoteAsyncStreamReader(
            Stream stream, UserCallBack callback, Encoding encoding,
            int bufferSize)
        {
            Debug.Assert(
                stream != null && encoding != null && callback != null,
                "Invalid arguments!");
            Debug.Assert(stream.CanRead, "Stream must be readable!");
            Debug.Assert(bufferSize > 0, "Invalid buffer size!");

            this.stream = stream;
            this.userCallBack = callback;
            this.decoder = encoding.GetDecoder();

            if (bufferSize < MinBufferSize)
            {
                bufferSize = MinBufferSize;
            }

            this.byteBuffer = new byte[bufferSize];
            this._maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            this.charBuffer = new char[this._maxCharsPerBuffer];
            this.cancelOperation = false;
            this.sb = null;
            this.bLastCarriageReturn = false;
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Close the stream and dispose of everything.
        /// </summary>
        public virtual void Close() => Dispose(true);

        /// <summary>
        /// Close the stream and trigger events.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.stream?.Close();
                }
            }
            finally
            {
                // If anyone is waiting on this have them stop.
                this._endStreamEvent.Set();
            }
        }

        // User calls BeginRead to start the asynchronous read
        internal void BeginReadLine()
        {
            if (this.cancelOperation)
            {
                this.cancelOperation = false;
            }

            if (this.sb == null)
            {
                // We are expecting to reset this later
                this._endStreamEvent.Reset();
                this.sb = new StringBuilder(DefaultBufferSize);
                this.stream?.BeginRead(this.byteBuffer, 0, this.byteBuffer.Length, ReadBuffer, null);
            }
            else
            {
                FlushMessageQueue();
            }
        }

        internal void CancelOperation() => this.cancelOperation = true;

        /// <summary>
        /// Wait until we hit EOF. This is called from RemoteHookProcess.WaitForExit
        /// We will lose some information if we don't do this
        /// </summary>
        internal void WaitUtilEOF() => this._endStreamEvent.WaitOne();

        private void FlushMessageQueue()
        {
            int length;

            do
            {
                string message = null;

                lock (this._messageEventQueue)
                {
                    length = this._messageEventQueue.Count;
                    if (length > 0)
                    {
                        message = this._messageEventQueue.Dequeue();
                    }
                }

                // skip if the read is the read is canceled
                // this might happen inside UserCallBack
                // However, continue to drain the queue
                if (!this.cancelOperation && !string.IsNullOrEmpty(message))
                {
                    this.userCallBack(message);
                }
            } while (length > 0);

            while (length > 0)
            {
            }
        }

        /// <summary>
        /// Read lines stored in StringBuilder and the buffer we just read into.
        /// A line is defined as a sequence of characters followed by
        /// a carriage return ('\r'), a line feed ('\n'), or a carriage return
        /// immediately followed by a line feed. The resulting string does not
        /// contain the terminating carriage return and/or line feed. The returned
        /// value is null if the end of the input stream has been reached.
        /// </summary>
        private void GetLinesFromStringBuilder()
        {
            int currentIndex = this.currentLinePos;
            int lineStart = 0;
            int len = this.sb.Length;

            // skip a beginning '\n' character of new block if last block ended
            // with '\r'
            if (this.bLastCarriageReturn && len > 0 && this.sb[0] == '\n')
            {
                currentIndex = 1;
                lineStart = 1;
                this.bLastCarriageReturn = false;
            }

            while (currentIndex < len)
            {
                char ch = this.sb[currentIndex];
                // Note the following common line feed chars:
                // \n - UNIX   \r\n - DOS   \r - Mac
                if (ch == '\r' || ch == '\n')
                {
                    string s = this.sb.ToString(lineStart, currentIndex - lineStart);
                    lineStart = currentIndex + 1;
                    // skip the "\n" character following "\r" character
                    if (ch == '\r' && lineStart < len && this.sb[lineStart] == '\n')
                    {
                        lineStart++;
                        currentIndex++;
                    }

                    this._messageEventQueue.Enqueue(s);
                }

                currentIndex++;
            }

            // Protect length as IndexOutOfRangeException was being thrown when less than a
            // character's worth of bytes was read at the beginning of a line.
            if (len > 0 && this.sb[len - 1] == '\r')
            {
                this.bLastCarriageReturn = true;
            }

            // Keep the rest characters which can't form a new line in string builder.
            if (lineStart < len)
            {
                if (lineStart == 0)
                {
                    // we found no breaklines, in this case we cache the position
                    // so next time we don't have to restart from the beginning
                    this.currentLinePos = currentIndex;
                }
                else
                {
                    this.sb.Remove(0, lineStart);
                    this.currentLinePos = 0;
                }
            }
            else
            {
                this.sb.Length = 0;
                this.currentLinePos = 0;
            }

            FlushMessageQueue();
        }

        /// <summary>
        /// This is the async callback function. Only one thread could/should call this.
        /// </summary>
        /// <param name="ar"></param>
        private void ReadBuffer(IAsyncResult ar)
        {
            int byteLen = 0;

            try
            {
                byteLen = this.stream?.EndRead(ar) ?? 0;
            }
            catch (Exception)
            {
                this.cancelOperation = true;
            }

            if (byteLen == 0 || this.cancelOperation)
            {
                try
                {
                    // We're at EOF, we won't call this function again from here on.
                    if (this.sb.Length != 0)
                    {
                        this._messageEventQueue.Enqueue(this.sb.ToString());
                        this.sb.Length = 0;
                    }

                    // UserCallback could throw, we should still set the eofEvent
                    FlushMessageQueue();
                }
                finally
                {
                    this._endStreamEvent.Set();
                }
            }
            else
            {
                try
                {
                    int charLen = this.decoder.GetChars(
                        this.byteBuffer, 0, byteLen, this.charBuffer, 0);
                    this.sb.Append(this.charBuffer, 0, charLen);
                    GetLinesFromStringBuilder();

                    this._endStreamEvent.Reset();
                    this.stream?.BeginRead(this.byteBuffer, 0, this.byteBuffer.Length, ReadBuffer, null);
                }
                catch
                {
                    // Make sure we free anyone waiting
                    this._endStreamEvent.Set();
                }
            }
        }
    }
}
