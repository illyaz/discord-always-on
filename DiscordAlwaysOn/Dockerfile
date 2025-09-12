FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY . .
RUN apk add --no-cache clang build-base zlib-dev
RUN dotnet publish "DiscordAlwaysOn.csproj" -c Release \
    -o /app/publish \
    -p:PublishAot=true \
    -p:StripSymbols=true \
    -p:OptimizationPreference=Size

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS final
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./DiscordAlwaysOn"]
