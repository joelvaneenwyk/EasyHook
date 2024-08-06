# EasyHook (File: Tools/CMake/EasyHook.cmake)
#
# Copyright (c) 2009 Christoph Husse & Copyright (c) 2015 Justin Stenning
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#
# Please visit https://easyhook.github.io for more information
# about the project and latest updates.

set(TARGET_NAME EasyHook)

add_library(${TARGET_NAME} SHARED
		"${CMAKE_SOURCE_DIR}/EasyHook/COMClassInfo.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/Config.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/Debugging.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/DllImport.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/GACWrap.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/HelperServiceInterface.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/InjectionLoader.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/LocalHook.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/NativeMethods.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/RemoteAsyncStreamReader.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/RemoteHook.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/RemoteHookProcess.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/ServiceMgmt.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/WOW64Bypass.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/Domain/DomainIdentifier.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/-DummyCore.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/ChannelProperties.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/ConnectionManager.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/DomainConnectionEndPoint.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/DuplexChannel.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/DuplexChannelEndPointObject.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/DuplexChannelReadyEventHandler.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/DuplexChannelState.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/EndPointConfigurationData.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/EndPointObject.cs"
		"${CMAKE_SOURCE_DIR}/EasyHook/IPC/SimplexChannel.cs"
		)

# Set designer and XAML properties.
csharp_set_designer_cs_properties(
		"${CMAKE_SOURCE_DIR}/EasyHook/Properties/AssemblyInfo.cs"
)

# Set CLR assembly properties.
set_target_properties(${TARGET_NAME} PROPERTIES
		LINKER_LANGUAGE CSharp
		VS_GLOBAL_ROOTNAMESPACE ${TARGET_NAME}
		)

target_compile_options(${TARGET_NAME} PRIVATE "/platform:x64" )

# Set the .NET Framework version for the target.
set_property(TARGET ${TARGET_NAME} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.6.1")

set_property(TARGET ${TARGET_NAME} PROPERTY VS_DOTNET_REFERENCES
		"Microsoft.CSharp"
		"PresentationCore"
		"PresentationFramework"
		"System"
		"System.Xaml"
		"System.Data"
		"System.Linq"
		"System.Windows"
		"System.Windows.Forms"
		"System.Numerics"
		"System.Drawing"
		"WindowsBase"
)

set_property(TARGET ${TARGET_NAME} PROPERTY VS_PACKAGE_REFERENCES
		"JetBrains.Annotations_2022.3.1"
)

target_compile_options(${TARGET_NAME} PUBLIC "/unsafe")

# Setup installer.
install(TARGETS ${TARGET_NAME} EXPORT ${TARGET_NAME}Config
		ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBRARY_DIR}
		LIBRARY DESTINATION ${CMAKE_INSTALL_LIBRARY_DIR}
		RUNTIME DESTINATION ${CMAKE_INSTALL_BINARY_DIR}
		INCLUDES DESTINATION ${CMAKE_INSTALL_INCLUDE_DIR}
		)

# Export config.
# INSTALL(EXPORT ${TARGET_NAME} DESTINATION ${CMAKE_INSTALL_EXPORT_DIR})
# EXPORT(TARGETS ${TARGET_NAME} FILE ${TARGET_NAME}.cmake)
