FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble-aot AS build

ARG TARGETARCH
WORKDIR /src
COPY global.json ./
COPY src/HttpHeaders.Api/*.csproj ./src/HttpHeaders.Api/
RUN dotnet restore src/HttpHeaders.Api/HttpHeaders.Api.csproj -a "$TARGETARCH"
COPY src/HttpHeaders.Api/ ./src/HttpHeaders.Api/
RUN dotnet publish src/HttpHeaders.Api/HttpHeaders.Api.csproj --no-restore --warnaserror -a "$TARGETARCH" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0.14-noble-chiseled
WORKDIR /home/app
COPY --from=build --chown=${APP_UID}:${APP_UID} /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["/home/app/HttpHeaders.Api"]
