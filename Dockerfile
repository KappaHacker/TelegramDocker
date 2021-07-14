FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY bin/Release/net5.0/publish TelegramDocker/
WORKDIR /TelegramDocker
COPY gg.json ./
ENTRYPOINT ["dotnet", "TelegramDocker.dll"]