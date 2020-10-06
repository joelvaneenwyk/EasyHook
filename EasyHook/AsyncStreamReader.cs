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
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace EasyHook
{
    public delegate void UserCallBack(string data);

    public class AsyncStreamReader : IDisposable
    {
        internal const int DefaultBufferSize = 1024; // Byte buffer size

        private const int MinBufferSize = 128;
        // Record the number of valid bytes in the byteBuffer, for a few checks.

        // This is the maximum number of chars we can get from one call to 
        // ReadBuffer.  Used so ReadBuffer can tell when to copy data into
        // a user's char[] directly, instead of our internal char[].
        private int _maxCharsPerBuffer;
        private bool bLastCarriageReturn;
        private byte[] byteBuffer;

        // Internal Cancel operation
        private bool cancelOperation;
        private char[] charBuffer;

        // Cache the last position scanned in sb when searching for lines.
        private int currentLinePos;
        private Decoder decoder;
        private Encoding encoding;
        private ManualResetEvent eofEvent;
        private readonly Queue messageQueue;

        // Store a backpointer to the process class, to check for user callbacks
        private RemoteHookProcess process;
        private StringBuilder sb;

        private Stream stream;

        // Delegate to call user function.
        private UserCallBack userCallBack;

        internal AsyncStreamReader(
            RemoteHookProcess process, Stream stream, UserCallBack callback, Encoding encoding)
            : this(process, stream, callback, encoding, DefaultBufferSize)
        {
        }

        /// <summary>
        /// Creates a new AsyncStreamReader for the given stream.  The
        /// character encoding is set by encoding and the buffer size,
        /// in number of 16-bit characters, is set by bufferSize.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="stream"></param>
        /// <param name="callback"></param>
        /// <param name="encoding"></param>
        /// <param name="bufferSize"></param>
        internal AsyncStreamReader(
            RemoteHookProcess process, Stream stream, UserCallBack callback, Encoding encoding,
            int bufferSize)
        {
            Debug.Assert(
                process != null && stream != null && encoding != null && callback != null,
                "Invalid arguments!");
            Debug.Assert(stream.CanRead, "Stream must be readable!");
            Debug.Assert(bufferSize > 0, "Invalid buffer size!");

            Init(process, stream, callback, encoding, bufferSize);
            this.messageQueue = new Queue();
        }

        public virtual Stream BaseStream => this.stream;

        public virtual Encoding CurrentEncoding => this.encoding;

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.stream != null)
                {
                    this.stream.Close();
                }
            }

            if (this.stream != null)
            {
                this.stream = null;
                this.encoding = null;
                this.decoder = null;
                this.byteBuffer = null;
                this.charBuffer = null;
            }

            if (this.eofEvent != null)
            {
                this.eofEvent.Close();
                this.eofEvent = null;
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
                this.sb = new StringBuilder(DefaultBufferSize);
                this.stream.BeginRead(this.byteBuffer, 0, this.byteBuffer.Length, ReadBuffer, null);
            }
            else
            {
                FlushMessageQueue();
            }
        }

        internal void CancelOperation()
        {
            this.cancelOperation = true;
        }

        // Wait until we hit EOF. This is called from RemoteHookProcess.WaitForExit
        // We will lose some information if we don't do this.
        internal void WaitUtilEOF()
        {
            if (this.eofEvent != null)
            {
                this.eofEvent.WaitOne();
                this.eofEvent.Close();
                this.eofEvent = null;
            }
        }

        private void FlushMessageQueue()
        {
            while (true)
            {
                // When we call BeginReadLine, we also need to flush the queue
                // So there could be a ---- between the ReadBuffer and BeginReadLine
                // We need to take lock before DeQueue.
                if (this.messageQueue.Count > 0)
                {
                    lock (this.messageQueue)
                    {
                        if (this.messageQueue.Count > 0)
                        {
                            string s = (string) this.messageQueue.Dequeue();
                            // skip if the read is the read is cancelled
                            // this might happen inside UserCallBack
                            // However, continue to drain the queue
                            if (!this.cancelOperation)
                            {
                                this.userCallBack(s);
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Read lines stored in StringBuilder and the buffer we just read into. 
        // A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //

        private void GetLinesFromStringBuilder()
        {
            int currentIndex = this.currentLinePos;
            int lineStart = 0;
            int len = this.sb.Length;

            // skip a beginning '\n' character of new block if last block ended 
            // with '\r'
            if (this.bLastCarriageReturn && (len > 0) && this.sb[0] == '\n')
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
                    if ((ch == '\r') && (lineStart < len) && (this.sb[lineStart] == '\n'))
                    {
                        lineStart++;
                        currentIndex++;
                    }

                    lock (this.messageQueue)
                    {
                        this.messageQueue.Enqueue(s);
                    }
                }

                currentIndex++;
            }

            // Protect length as IndexOutOfRangeException was being thrown when less than a
            // character's worth of bytes was read at the beginning of a line.
            if (len > 0 && this.sb[len - 1] == '\r')
            {
                this.bLastCarriageReturn = true;
            }

            // Keep the rest characaters which can't form a new line in string builder.
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

        private void Init(
            RemoteHookProcess process, Stream stream, UserCallBack callback, Encoding encoding,
            int bufferSize)
        {
            this.process = process;
            this.stream = stream;
            this.encoding = encoding;
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
            this.eofEvent = new ManualResetEvent(false);
            this.sb = null;
            this.bLastCarriageReturn = false;
        }

        // This is the async callback function. Only one thread could/should call this.
        private void ReadBuffer(IAsyncResult ar)
        {
            int byteLen;

            try
            {
                byteLen = this.stream.EndRead(ar);
            }
            catch (IOException)
            {
                // We should ideally consume errors from operations getting cancelled
                // so that we don't crash the unsuspecting parent with an unhandled exc. 
                // This seems to come in 2 forms of exceptions (depending on platform and scenario), 
                // namely OperationCanceledException and IOException (for errorcode that we don't 
                // map explicitly).   
                byteLen = 0; // Treat this as EOF
            }
            catch (OperationCanceledException)
            {
                // We should consume any OperationCanceledException from child read here  
                // so that we don't crash the parent with an unhandled exc
                byteLen = 0; // Treat this as EOF
            }

            if (byteLen == 0)
            {
                // We're at EOF, we won't call this function again from here on.
                lock (this.messageQueue)
                {
                    if (this.sb.Length != 0)
                    {
                        this.messageQueue.Enqueue(this.sb.ToString());
                        this.sb.Length = 0;
                    }

                    this.messageQueue.Enqueue(null);
                }

                try
                {
                    // UserCallback could throw, we should still set the eofEvent 
                    FlushMessageQueue();
                }
                finally
                {
                    this.eofEvent.Set();
                }
            }
            else
            {
                int charLen = this.decoder.GetChars(
                    this.byteBuffer, 0, byteLen, this.charBuffer, 0);
                this.sb.Append(this.charBuffer, 0, charLen);
                GetLinesFromStringBuilder();
                this.stream.BeginRead(this.byteBuffer, 0, this.byteBuffer.Length, ReadBuffer, null);
            }
        }
    }
}
