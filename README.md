# Cargo Delivery Calculator

An ASP.NET Core Web API that estimates the economics of delivering an LNG cargo to each candidate destination and recommends the one with the highest margin.

## Requirements

- .NET 10 SDK
- Docker (PostgreSQL for the app; the integration tests start their own)

## Running

```bash
docker compose up -d                            # PostgreSQL on localhost:5432
dotnet run --project CargoDeliveryCalculator
```

The API listens on `http://localhost:5224`. In the Development environment (the default launch profile), interactive API docs are served by Scalar at `http://localhost:5224/scalar/v1` and the OpenAPI document at `/openapi/v1.json`.

The connection string is `ConnectionStrings:DevConnection` in `appsettings.json` and matches `docker-compose.yml`. In Development, pending EF Core migrations are applied automatically at startup. In other environments, apply them yourself:

```bash
dotnet tool install --global dotnet-ef                                   # once
dotnet ef database update --project CargoDeliveryCalculator
```

## Testing

```bash
dotnet test                                                        # all tests
dotnet test --filter "FullyQualifiedName~RecommendationItTests"    # one fixture
```

The test project (`CargoDeliveryCalculator.Tests`, NUnit) contains:

- unit tests for the calculation service, with the price repository mocked;
- integration tests that run the API in memory via `WebApplicationFactory` against a throwaway PostgreSQL container ([Testcontainers](https://dotnet.testcontainers.org/)), so Docker must be running;
- tests of the EF Core model that check the generated DDL without connecting to a database.

## API

### Index prices

Monthly prices for the supported indexes: `Hh`, `Ttf`, `Nbp`, `Jkm`, `Brent` (case-insensitive in the URL).

| Method | Route | Body | Response |
|---|---|---|---|
| `PUT` | `/api/dynamics/indexes/{index}/{year}/{month}` | price as a bare JSON number, e.g. `3.00` | `201 Created` for a new price, `200 OK` when overwriting |
| `GET` | `/api/indexes/{index}/{year}/{month}` | — | the price, e.g. `3.00` |

Prices are stored in PostgreSQL, one row per index and year. Nothing is pre-loaded: load the prices a recommendation needs before calling it. Changing a price that an active cargo depends on adds a new estimate version for that cargo (see [Active cargos](#active-cargos)).

```bash
curl -X PUT http://localhost:5224/api/dynamics/indexes/hh/2026/11  -H "Content-Type: application/json" -d 3.00
curl -X PUT http://localhost:5224/api/dynamics/indexes/ttf/2026/11 -H "Content-Type: application/json" -d 11.00
curl -X PUT http://localhost:5224/api/dynamics/indexes/jkm/2026/12 -H "Content-Type: application/json" -d 12.00
```

### Recommendation

`POST /api/recommendation`

```json
{
  "cargo": {
    "cargoId": "C-001",
    "loadDate": "2026-11-10",
    "loadedVolume": 160000,
    "conversionFactor": 22.0,
    "purchaseFormula": { "slope": 1.15, "index": "Hh", "constant": 2.50 }
  },
  "shipping": { "dailyHireRate": 60000, "boilOffRate": 0.001 },
  "destinations": [
    {
      "name": "NWEurope",
      "saleFormula": { "slope": 1.0, "index": "Ttf", "constant": -0.40 },
      "ladenVoyageDurationInDays": 14,
      "regasFee": 0.45,
      "canalFee": 0
    },
    {
      "name": "Japan",
      "saleFormula": { "slope": 1.0, "index": "Jkm", "constant": 0 },
      "ladenVoyageDurationInDays": 25,
      "regasFee": 0,
      "canalFee": 600000
    }
  ]
}
```

The response echoes the cargo and shipping, names the `bestDestination`, and lists an `estimate` per destination, sorted by margin (highest first). For the request above (abridged):

```json
{
  "bestDestination": { "name": "Japan", "...": "..." },
  "estimates": [
    {
      "destination": { "name": "Japan", "...": "..." },
      "estimate": {
        "deliveredEnergy": 3432000, "salePrice": 12.0, "revenue": 41184000,
        "purchaseCost": 20944000, "shippingCost": 3000000, "regasCost": 0,
        "canalFee": 600000, "margin": 16640000, "netback": 10.6773
      }
    },
    {
      "destination": { "name": "NWEurope", "...": "..." },
      "estimate": {
        "deliveredEnergy": 3470720, "salePrice": 10.6, "revenue": 36789632,
        "purchaseCost": 20944000, "shippingCost": 1680000, "regasCost": 1561824,
        "canalFee": 0, "margin": 12603808, "netback": 9.5306
      }
    }
  ]
}
```

### Active cargos

An active cargo is a cargo committed to a single destination. Its estimate is stored and versioned: version 1 is created on activation, and a new version is added whenever something it depends on changes.

| Method | Route | Body | Response |
|---|---|---|---|
| `POST` | `/api/recommendation/active` | like a recommendation request, but with a single `destination` object instead of `destinations` | the first version (version 1) |
| `POST` | `/api/dynamics/discharge/cargos/{cargoId}` | delivered energy in MMBtu as a bare JSON number, e.g. `3000000` | the new version |

Each version is returned with its number and the UTC time it was stored:

```json
{
  "estimate": { "deliveredEnergy": 3432000, "salePrice": 12, "revenue": 41184000, "purchaseCost": 20944000, "purchasePrice": 5.95, "shippingCost": 3000000, "regasCost": 0, "canalFee": 600000, "margin": 16640000, "netback": 10.6773 },
  "version": 1,
  "creationDateTime": "2026-10-13T15:17:39.399251"
}
```

A new version is added when:

- the load-month price or the delivery-month sale price the cargo uses is changed (setting the same value again adds nothing);
- the discharge is reported: the measured energy replaces the boil-off estimate of delivered energy, here and in every later version.

### Estimate history

Read the stored estimates of an active cargo. Each version has the shape shown under [Active cargos](#active-cargos); a returned `creationDateTime` can be passed back as `dateTime`.

| Method | Route | Response |
|---|---|---|
| `GET` | `/api/estimate/cargos/{cargoId}/versions/latest` | the most recent version |
| `GET` | `/api/estimate/cargos/{cargoId}/versions/{version}` | that version |
| `GET` | `/api/estimate/cargos/{cargoId}/versions?dateTime=2026-11-20T10:00:00` | the version that was current at that time; `dateTime` is ISO `yyyy-MM-ddTHH:mm:ss` (fractional seconds optional) and read as UTC |
| `GET` | `/api/estimate/cargos/{cargoId}/versions/range?fromDateTime=2026-11-01T00:00:00&toDateTime=2026-12-01T00:00:00` | the versions created from `fromDateTime` (inclusive) to `toDateTime` (exclusive), oldest first; same format as `dateTime`; an empty list if none |
| `GET` | `/api/estimate/cargos/{cargoId}/versions/{versionA}/{versionB}` | the difference between two versions, `versionA` before `versionB` (see below) |

The difference holds both versions, the time between them as `elapsedTime` (`H:mm:ss`, total hours, so `26:05:03` for a gap over a day), and for each estimate field an `{ oldValue, newValue, difference }` object, or `null` if the field did not change. Reporting a discharge, for example (abridged):

```json
{
  "oldVersion": { "estimate": { … }, "version": 1, "creationDateTime": "2026-10-13T15:17:39.399251" },
  "newVersion": { "estimate": { … }, "version": 2, "creationDateTime": "2026-10-13T15:17:39.852947" },
  "elapsedTime": "0:00:00",
  "deliveredEnergy": { "oldValue": 3432000, "newValue": 3000000, "difference": -432000 },
  "salePrice": null,
  "revenue": { "oldValue": 41184000, "newValue": 36000000, "difference": -5184000 },
  …
}
```

## How estimates are calculated

For each destination:

| Quantity | Formula |
|---|---|
| Loaded energy | `loadedVolume × conversionFactor` |
| Delivery date | `loadDate + ladenVoyageDurationInDays` |
| Delivered energy | `loadedEnergy × (1 − boilOffRate × ladenVoyageDurationInDays)` |
| Purchase cost | `loadedEnergy × (slope × index price in the load month + constant)` |
| Sale price | `slope × index price in the delivery month + constant` |
| Revenue | `deliveredEnergy × salePrice` |
| Shipping cost | `2 × dailyHireRate × ladenVoyageDurationInDays` (the return ballast leg is assumed to take as long as the laden leg) |
| Regas cost | `deliveredEnergy × regasFee` |
| Margin | `revenue − shippingCost − regasCost − canalFee − purchaseCost` |
| Netback | `(revenue − shippingCost − regasCost − canalFee) / loadedEnergy` |

The destination with the highest margin is recommended.

## Validation and errors

Errors are returned as [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) with the reason in `title`.

| Status | When |
|---|---|
| `400` | `cargoId` longer than 64 characters; `loadDate` is not ISO `yyyy-MM-dd`; `dateTime`, `fromDateTime` or `toDateTime` missing or not ISO `yyyy-MM-ddTHH:mm:ss`; `fromDateTime` not before `toDateTime`; estimate version not positive; `versionA` not before `versionB`; discharged energy negative; cargo already active; more than one destination for an active cargo; unknown index; month outside 1–12; year before 1990; `loadedVolume`, `conversionFactor`, `dailyHireRate` or `ladenVoyageDurationInDays` not positive; `slope`, `regasFee` or `canalFee` negative; `boilOffRate` outside 0–1; total boil-off (`boilOffRate × ladenVoyageDurationInDays`) of 100% or more; empty `destinations` |
| `404` | a required index price has not been set (`no Nbp value for 2026` when the index has no price for that year at all, `no Ttf value for 12-2026` when the year exists but that month was never set); no active cargo with that `cargoId`; the requested estimate version does not exist, or no estimate existed yet at the requested time |
| `500` | unexpected error |
