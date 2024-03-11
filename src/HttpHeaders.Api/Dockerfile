FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot AS build

WORKDIR /src

COPY ./*.csproj ./

RUN dotnet restore

COPY . ./

RUN dotnet publish -r linux-x64 -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot

WORKDIR /home/app

COPY --from=build --chown=${APP_UID}:${APP_UID} /app/publish .

CMD ["/home/app/HttpHeaders.Api"]
