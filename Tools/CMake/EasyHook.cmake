# Create assembly info with current version.
# CONFIGURE_FILE("${CMAKE_SOURCE_DIR}/src/AssemblyInfo.cs.template" "${CMAKE_BINARY_DIR}/Config/${PROJECT_NAME}/AssemblyInfo.cs")

SET(TARGET_NAME EasyHook)
# Add shared library project.
ADD_LIBRARY(${TARGET_NAME} SHARED
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
# CSHARP_SET_DESIGNER_CS_PROPERTIES(
#     "${CMAKE_SOURCE_DIR}/EasyHook/Properties/AssemblyInfo.cs"
# )

# Define dependencies.
# TARGET_LINK_LIBRARIES(${TARGET_NAME}
#     PUBLIC CommonLib
# )

# Set CLR assembly properties.
SET_TARGET_PROPERTIES(${TARGET_NAME} PROPERTIES
    VS_DOTNET_REFERENCES "System;System.Data;System.Runtime.Remoting;System.Xml"
    VS_GLOBAL_ROOTNAMESPACE ${TARGET_NAME}
)
target_compile_options(${TARGET_NAME} PUBLIC "/unsafe")

# Setup installer.
INSTALL(TARGETS ${TARGET_NAME} EXPORT ${TARGET_NAME}Config
    ARCHIVE DESTINATION ${CMAKE_INSTALL_LIBRARY_DIR}
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBRARY_DIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINARY_DIR}
    INCLUDES DESTINATION ${CMAKE_INSTALL_INCLUDE_DIR}
)

# Export config.
# INSTALL(EXPORT ${TARGET_NAME} DESTINATION ${CMAKE_INSTALL_EXPORT_DIR})
# EXPORT(TARGETS ${TARGET_NAME} FILE ${TARGET_NAME}.cmake)
