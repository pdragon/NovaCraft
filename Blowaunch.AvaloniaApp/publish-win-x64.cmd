"c:\Program Files\dotnet\dotnet" restore -r win-x64
"c:\Program Files\dotnet\dotnet" build --no-restore -r win-x64 --configuration Release -p:PublishSingleFile=true
"c:\Program Files\dotnet\dotnet" publish --no-build -r win-x64 --configuration Release -p:PublishSingleFile=true --output d:/blow/win
@pause