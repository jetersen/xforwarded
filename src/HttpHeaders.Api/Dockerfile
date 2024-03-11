FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot

WORKDIR /home/app

COPY --chown=${APP_UID}:${APP_UID} ./publish .

CMD ["./HttpHeaders.Api"]
