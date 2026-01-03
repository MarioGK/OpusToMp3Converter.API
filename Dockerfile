FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OpusToMp3.slnx .
COPY src/OpusToMp3.Api/OpusToMp3.Api.csproj src/OpusToMp3.Api/
RUN dotnet restore src/OpusToMp3.Api/OpusToMp3.Api.csproj

COPY src/OpusToMp3.Api/ src/OpusToMp3.Api/
RUN dotnet publish src/OpusToMp3.Api/OpusToMp3.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OpusToMp3.Api.dll"]
