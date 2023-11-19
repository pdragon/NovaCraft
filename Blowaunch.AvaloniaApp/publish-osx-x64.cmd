"c:\Program Files\dotnet\dotnet" restore -r osx-x64
"c:\Program Files\dotnet\dotnet" build --no-restore -r osx-x64 --configuration Release -p:PublishSingleFile=true
"c:\Program Files\dotnet\dotnet" publish --no-build -r osx-x64 --configuration Release -p:PublishSingleFile=true --output d:/blow/osx-x64
@pause