#!/usr/bin/env bash
set -euo pipefail
rid=${1:?Specify linux-x64 or linux-arm64}
architecture=${2:?Specify amd64 or arm64}
dotnet publish src/HttpHeaders.Api/HttpHeaders.Api.csproj -t:PublishContainer -c Release -r "$rid" --warnaserror -p:ContainerImageTag="test-$architecture"
bash tests/container.sh "xforwarded:test-$architecture" "$architecture"
