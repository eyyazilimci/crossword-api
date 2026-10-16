# Used only by Render's build servers (Render has no native .NET runtime). Not needed for local development.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
RUN dotnet publish src/Crossword.Api/Crossword.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# tzdata for the Europe/Istanbul day boundary, ICU for Turkish casing (tr-TR).
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libicu-dev \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app .
# Render passes the port in $PORT.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Crossword.Api.dll"]
