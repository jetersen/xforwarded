#!/usr/bin/env bash
set -euo pipefail
image=${1:-xforwarded:test}
container=$(docker run -d --read-only --cap-drop=ALL --security-opt=no-new-privileges --memory=128m --cpus=1 -p 127.0.0.1::8080 -e Diagnostics__RedactedHeaders__0=X-Custom-Secret "$image")
trap 'docker rm -f "$container" >/dev/null' EXIT
[[ $(docker inspect --format '{{.Config.User}}' "$container") == 1654 ]]
port=$(docker port "$container" 8080/tcp)
python3 tests/smoke.py "http://$port"
