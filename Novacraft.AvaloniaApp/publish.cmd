del .\publish\%1.zip
@"c:\Program Files\dotnet\dotnet" restore -r %1
@"c:\Program Files\dotnet\dotnet" build --no-restore -r %1 --configuration Release -p:PublishSingleFile=true --self-contained
@"c:\Program Files\dotnet\dotnet" publish --no-build -r %1 --configuration Release -p:PublishSingleFile=true --output ./publish/%1
if %1==linux-x64 copy .\nativeLibs\libdl.so .\publish\%1\libdl.so
@call clean.cmd %1
@C:\Windows\System32\WindowsPowerShell\v1.0\powershell Compress-Archive -LiteralPath .\publish\%1 -DestinationPath .\publish\%1.zip