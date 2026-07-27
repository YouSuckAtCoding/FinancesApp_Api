# Phantom-token Gateway

HTTP API (API Gateway v2) + .NET 8 Lambda authorizer that fronts `FinancesApp_Api`.

## How a request flows

```
Browser ──cookie: X-Access-Token=opaqueRef──▶ HTTP API
                                                │  Lambda authorizer fires
                                                ▼
                  read cookie ──▶ POST Identity.Api /exchange  (X-Internal-Secret)
                                                │   hash → DynamoDB → mint JWT
                                                ▼
              returns { isAuthorized:true, context:{ authHeader:"Bearer eyJ..." } }
                                                │
        Integration param-mapping: Authorization = $context.authorizer.authHeader
                                                ▼
                       proxy ──▶ FinancesApp_Api (validates JWT as today)
```

The browser only ever holds the opaque ref. The real JWT exists only between gateway and
backend. This Lambda replaces the `JwtService.ExchangeReferenceForJwt` in-app stand-in.

## Layout

- `src/Authorizer/` — the .NET 8 authorizer Lambda (`Function.cs`).
- `template.yaml` — SAM template: HTTP API + authorizer + HTTP_PROXY integration + header injection.

## Deploy (step 3–4)

Prereqs: AWS SAM CLI, .NET 8 SDK, Docker (SAM uses it to build the managed runtime).

```bash
cd Gateway
sam build
sam deploy --guided
```

`--guided` prompts for the parameters (saved to `samconfig.toml` for next time):

| Parameter | Value |
|---|---|
| `IdentityApiBaseUrl` | Base URL of Identity.Api, e.g. `https://identity.internal` (no trailing path) |
| `BackendBaseUrl` | FinancesApp_Api URL **with `{proxy}`**, e.g. `https://backend.internal/{proxy}` |
| `InternalSecret` | Same value as Identity.Api's `InternalSecret` config |
| `AuthorizerCacheTtlSeconds` | `0` to start (instant revocation); raise to 60–300 once it works |

The stack output `GatewayUrl` is the public entry point.

## Cut over (step 5)

1. Point the frontend at `GatewayUrl` instead of calling `FinancesApp_Api` directly.
2. Lock the backend so it only accepts traffic from the gateway (VPC link / security group /
   shared header check) — otherwise a caller could still hit the backend with a self-minted JWT.

## Tune / harden (step 6)

- **Authorizer caching**: bump `AuthorizerCacheTtlSeconds`. Cache key is the whole `Cookie`
  header. Tradeoff: a revoked ref still works until the cached result expires.
- **Secret storage**: move `InternalSecret` from a CloudFormation parameter to AWS Secrets
  Manager / SSM Parameter Store and read it in the Lambda.
- **Private networking**: once Identity.Api is private, drop the shared-secret reliance and
  reach it over a VPC link instead of public HTTPS.
- **Cold starts**: arm64 + 256 MB is the cheap default. If p99 latency matters, add
  provisioned concurrency on the authorizer.
