cd C:\Windows\System32\App
dotnet restore -s -s https://www.nuget.org/api/v2/
dotnet publish -p:Version=$"1.0" --no-restore -c Release
docker stop core-counter || true
docker rm core-counter || true
docker rmi core-counter-image || true
docker build -t core-counter-image -f Dockerfile .
docker run -it --restart unless-stopped --name core-counter core-counter-image
