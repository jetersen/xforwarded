# xforwarded

HTTP request diagnostics built with .NET 10 NativeAOT. The SDK is selected in
`global.json`; the runtime patch and container configuration are selected in the project.
The .NET SDK builds containers directly, using a stable Ubuntu 24.04 (Noble)
runtime-deps base instead of the old nightly Jammy image. No Dockerfile is needed.
The chiseled runtime-deps image includes the libraries needed by this invariant
globalization NativeAOT app, with no managed runtime, shell, or package manager.

## Diagnostic routes

All diagnostic routes return JSON containing `remoteIp`, `remotePort`, `host`,
`scheme`, and `headers` (header values are arrays).

| GET route | Behavior |
| --- | --- |
| `/` | Direct connection and request values |
| `/forwardedHeaders` | Apply X-Forwarded-For, X-Forwarded-Host and X-Forwarded-Proto |
| `/forwardedPort` | Apply the existing custom forwarded-port middleware |
| `/forwardedHeadersAndPort` | Apply forwarded headers, then custom forwarded port |
| `/readiness`, `/liveness` | HTTP 200, plain text `OK` |

Forwarded-header routes intentionally trust every peer and consume only the
rightmost forwarded entry (the framework's default one-hop limit). The custom
port middleware prefers a parsed port from `X-Original-Forwarded-For`, then
`X-Forwarded-Port`. These are diagnostic observations, not authentication or
trusted identity. Put the service behind access-controlled ingress; it has no
built-in authentication or TLS termination. Do not globally enable
`ASPNETCORE_FORWARDEDHEADERS_ENABLED`, which would change the route comparison.

Unlike the original unfiltered response, common credential headers now return
`[REDACTED]`: Authorization, Proxy-Authorization, Cookie, Set-Cookie, X-Api-Key,
Api-Key, X-Auth-Token, X-Forwarded-Access-Token, X-Amzn-Oidc-Accesstoken and
X-Amzn-Oidc-Data. Matching is case-insensitive and does not alter the request.
Add deployment-specific names with `Diagnostics__RedactedHeaders__0`,
`Diagnostics__RedactedHeaders__1`, etc. Built-in redactions cannot be disabled.
Other headers remain visible, including forwarding, CloudFront and identity
metadata. This denylist cannot identify every custom secret header; configure
additional names at ingress/deployment time. Avoid sending credentials as other
header values, URLs or host names.

## Build and test

Requires the SDK selected by `global.json`, Docker, Bash, Python 3 and the
[NativeAOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
(`clang` and `zlib1g-dev` on Ubuntu 24.04). Run on the target architecture from
the repository root:

```sh
dotnet build -c Release --warnaserror
# In one terminal:
Diagnostics__RedactedHeaders__0=X-Custom-Secret dotnet run --no-build -c Release --project src/HttpHeaders.Api --no-launch-profile --urls http://127.0.0.1:8080
# In another:
python3 tests/smoke.py http://127.0.0.1:8080

# amd64:
bash tests/build-container.sh linux-x64 amd64
# On an ARM64 machine instead:
bash tests/build-container.sh linux-arm64 arm64
```

`dotnet publish -t:PublishContainer` compiles the NativeAOT app and constructs
its container image. The project pins `runtime-deps:10.0.14-noble-chiseled`,
user 1654 and HTTP port 8080. The SDK writes the image to the local Docker engine
for HTTP regression tests; CI never transfers image archives between jobs.

CI builds and tests Linux amd64 on `ubuntu-24.04` and Linux arm64 on
`ubuntu-24.04-arm`, without emulation or cross-compiling NativeAOT. PR jobs have
read-only repository permission, no registry login and no image push.

After both checks pass on a push to `main`, separate native publishing jobs
build and test the app again, then publish that native output directly through
the SDK to `ghcr.io/jetersen/xforwarded` as `sha-<full commit>-amd64` and
`sha-<full commit>-arm64`. They also test each registry image by digest. Only the
small digest reference files are passed to the manifest job. Once both images
pass, it creates and verifies `main` and `sha-<full commit>` multi-platform
manifests. Docker/Kubernetes automatically selects the node's architecture.
Deploy by immutable multi-platform manifest digest.

## Deployment contract

- HTTP port **8080**, exposed on all interfaces by the container default
  `ASPNETCORE_HTTP_PORTS=8080`. Override with that variable or `ASPNETCORE_URLS`.
- Readiness: `GET /readiness`; liveness: `GET /liveness` on the HTTP port.
- Non-root user/group **1654** (`app`); supports read-only root filesystem,
  all capabilities dropped and `allowPrivilegeEscalation: false`.
- No database, persistent storage, secrets, external services, or outbound
  network access required. Request data is returned to callers; keep ingress
  access-controlled and configure redaction for any custom credential headers.
- Invariant globalization, no ICU/time-zone data dependency. Configuration may
  be supplied via environment variables; no writable application directory needed.
- The container test uses a 128 MiB memory limit and one CPU. Size production
  requests/limits from expected traffic and measurements, not this smoke test.
- CloudFront, ingress, identity-aware access, DNS, certificates and optional
  viewer mTLS belong in separately managed infrastructure. Health probes must
  reach the service directly without requiring viewer authentication.

## Primary references

- [.NET 10 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
- [SDK container publishing configuration](https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration)
- [Supported runtime-deps images](https://github.com/dotnet/dotnet-docker/blob/main/README.runtime-deps.md)
- [Chiseled images](https://github.com/dotnet/dotnet-docker/blob/main/documentation/ubuntu-chiseled.md)
- [Forwarded headers options](https://github.com/dotnet/aspnetcore/blob/v10.0.14/src/Middleware/HttpOverrides/src/ForwardedHeadersOptions.cs)
