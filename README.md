# NipNip — Georgian Affiliate Marketing Platform

Open affiliate marketplace for Georgia. Merchants register and set a commission rate. Creators browse merchants and grab unique tracking links instantly — no approval needed. When a creator drives a sale, commission is calculated and deducted from the merchant's balance. Creators get paid monthly.

## Tech Stack

- .NET 8, C#
- PostgreSQL + EF Core (code-first)
- Clerk (JWT auth)
- Cloudflare R2 (file uploads)

## Projects

| Project | Purpose |
|---|---|
| `NipNip.Api` | Entry point — middleware, DI wiring |
| `NipNip.Data` | DbContext, entities, migrations |
| `NipNip.Shared` | Custom exceptions, shared utilities |
| `NipNip.Modules.Merchants` | Merchant registration, profiles, dashboard |
| `NipNip.Modules.Creators` | Creator registration, profiles, dashboard |
| `NipNip.Modules.Tracking` | Click tracking, conversion reporting |
| `NipNip.Modules.Payouts` | Payout management |

## Getting Started

1. Copy `appsettings.json` to `appsettings.Local.json` and fill in connection string, Clerk keys, and R2 credentials.
2. Apply migrations: `dotnet ef database update --project src/NipNip.Data --startup-project src/NipNip.Api`
3. Run: `dotnet run --project src/NipNip.Api`
