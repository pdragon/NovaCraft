"c:\Program Files\dotnet\dotnet" restore -r linux-x64
"c:\Program Files\dotnet\dotnet" build --no-restore -r linux-x64 --configuration Release -p:PublishSingleFile=true
"c:\Program Files\dotnet\dotnet" publish --no-build -r linux-x64 --configuration Release -p:PublishSingleFile=true --output d:/blow/linux
@pause