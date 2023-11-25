rem if %1 is not defined goto :EOF
"c:\Program Files\dotnet\dotnet" restore -r %1
"c:\Program Files\dotnet\dotnet" build --no-restore -r %1 --configuration Release -p:PublishSingleFile=true --self-contained
"c:\Program Files\dotnet\dotnet" publish --no-build -r %1 --configuration Release -p:PublishSingleFile=true --output ./publish/%1
@rem "c:\Program Files\dotnet\dotnet" build --no-restore -r %1 --configuration Release -p:PublishSingleFile=false --self-contained
@rem "c:\Program Files\dotnet\dotnet" publish --no-build -r %1 --configuration Release -p:PublishSingleFile=false --output ./publish/%1
call clean.cmd %1
C:\Windows\System32\WindowsPowerShell\v1.0\powershell Compress-Archive -LiteralPath .\publish\%1 -DestinationPath .\publish\%1.zip
:EOF
