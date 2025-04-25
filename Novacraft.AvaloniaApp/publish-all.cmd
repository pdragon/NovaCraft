call publish.cmd linux-x64
@rem copy nativeLibs/libdl.so ./publish/libdl.so
call publish.cmd osx-x64
call publish.cmd win-x64
@pause