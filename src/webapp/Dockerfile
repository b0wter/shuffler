FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ../contracts/ ./contracts
COPY ../spotify/ ./spotify
COPY ../webapp/ ./webapp

WORKDIR /source/contracts
RUN dotnet restore
WORKDIR /source/spotify
RUN dotnet restore
WORKDIR /source/webapp
RUN dotnet restore
RUN dotnet publish -c release -o /app

# final stage/image
#
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# Der folgende Ordner muss erstellt werden, damit die Datenbank erzeugt werden kann
RUN mkdir -p /app/shuffler
COPY --from=build /app ./
RUN ls -la /app
EXPOSE 80/tcp
HEALTHCHECK CMD curl --fail http://localhost:80/health
ENTRYPOINT ["dotnet", "Shuffler.WebApp.App.dll"]
