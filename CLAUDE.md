# NipNip — Georgian Affiliate Marketing Platform (Backend API)

## What is this

An open affiliate marketplace for Georgia. Merchants register, set a commission rate, deposit balance. Creators browse merchants, instantly grab a unique tracking link — no approval needed, open marketplace. When a creator drives a sale, commission is calculated and deducted from merchant's balance. Creators get paid monthly. Platform takes 3% on top.

## Tech stack

- .NET 8, C#
- PostgreSQL with EF Core (code-first migrations, Npgsql provider)
- Clerk for auth (JWT validation)
- Cloudflare R2 for file uploads (S3-compatible)
- No Redis, no RabbitMQ, no Docker, no background jobs — MVP simplicity

## Solution structure

```
NipNip.sln
├── src/
│   ├── NipNip.Api/                      # Entry point — Program.cs, middleware, DI
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Middleware/
│   │       ├── ExceptionHandlingMiddleware.cs   # Catches custom exceptions → ProblemDetails
│   │       └── ClerkJwtMiddleware.cs            # Validates Clerk JWT, sets HttpContext.User
│   │
│   ├── NipNip.Shared/                   # Cross-cutting: custom exceptions, base types, helpers
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ConflictException.cs
│   │   │   ├── InsufficientBalanceException.cs
│   │   │   └── ForbiddenException.cs
│   │   └── Extensions/
│   │
│   ├── NipNip.Data/                     # DbContext, entity configurations, migrations
│   │   ├── AppDbContext.cs              # Single DbContext with ALL entities
│   │   ├── Entities/                    # Entity classes (POCOs)
│   │   │   ├── Merchant.cs
│   │   │   ├── Creator.cs
│   │   │   ├── Click.cs
│   │   │   ├── Conversion.cs
│   │   │   └── Payout.cs
│   │   ├── Enums/
│   │   │   ├── ConversionSource.cs
│   │   │   ├── ConversionStatus.cs
│   │   │   └── PayoutStatus.cs
│   │   ├── Configurations/              # IEntityTypeConfiguration<T> per entity
│   │   │   ├── MerchantConfiguration.cs
│   │   │   ├── CreatorConfiguration.cs
│   │   │   ├── ClickConfiguration.cs
│   │   │   ├── ConversionConfiguration.cs
│   │   │   └── PayoutConfiguration.cs
│   │   └── Migrations/
│   │
│   ├── NipNip.Modules.Merchants/        # Merchant module
│   │   ├── MerchantController.cs
│   │   ├── MerchantService.cs           # Concrete class, no interface
│   │   ├── DTOs/
│   │   │   ├── RegisterMerchantRequest.cs
│   │   │   ├── UpdateMerchantRequest.cs
│   │   │   ├── MerchantResponse.cs
│   │   │   └── MerchantDashboardResponse.cs
│   │   └── Extensions/
│   │       ├── MerchantMappingExtensions.cs   # .ToDto() extension methods
│   │       └── MerchantServiceExtensions.cs   # DI registration
│   │
│   ├── NipNip.Modules.Creators/         # Creator module
│   │   ├── CreatorController.cs
│   │   ├── CreatorService.cs
│   │   ├── DTOs/
│   │   │   ├── RegisterCreatorRequest.cs
│   │   │   ├── UpdateCreatorRequest.cs
│   │   │   ├── CreatorResponse.cs
│   │   │   └── CreatorDashboardResponse.cs
│   │   └── Extensions/
│   │       ├── CreatorMappingExtensions.cs
│   │       └── CreatorServiceExtensions.cs
│   │
│   ├── NipNip.Modules.Tracking/         # Click tracking + conversion tracking
│   │   ├── RedirectController.cs        # GET /r/{creatorSlug}/{merchantSlug}
│   │   ├── ConversionController.cs      # POST /api/conversions/track + /manual
│   │   ├── TrackingService.cs           # Click logging, conversion processing
│   │   ├── DTOs/
│   │   │   ├── TrackConversionRequest.cs
│   │   │   ├── ManualConversionRequest.cs
│   │   │   └── ConversionResponse.cs
│   │   └── Extensions/
│   │       ├── ConversionMappingExtensions.cs
│   │       └── TrackingServiceExtensions.cs
│   │
│   └── NipNip.Modules.Payouts/          # Payout management
│       ├── PayoutController.cs
│       ├── PayoutService.cs
│       ├── DTOs/
│       │   ├── PayoutResponse.cs
│       │   └── ProcessPayoutRequest.cs
│       └── Extensions/
│           ├── PayoutMappingExtensions.cs
│           └── PayoutServiceExtensions.cs
```

## Architecture rules — FOLLOW THESE STRICTLY

### Pattern
- **Controllers** — always use `[ApiController]` with controllers, NEVER minimal APIs
- **Services** — concrete classes only, NO interfaces for services
- **Dependency flow**: Controller → Service → DbContext. Controllers never touch DbContext directly. Services inject AppDbContext directly, no repository pattern.
- **DTOs** — request and response records. Entities are never exposed to controllers or API responses.
- **Mapping** — static `ToDto()` / `ToEntity()` extension methods on the entity/DTO. No AutoMapper, no Mapster.
- **Validation** — manual validation in service methods. No FluentValidation, no data annotations for validation. Data annotations are fine for Swagger docs only.
- **Error handling** — services throw custom exceptions (NotFoundException, ConflictException, etc.). ExceptionHandlingMiddleware catches them and returns ProblemDetails with correct HTTP status codes. Controllers never catch exceptions.
- **No repository pattern** — services query DbContext directly with LINQ
- **No MediatR** — direct service method calls
- **No CQRS** — read and write in the same service

### Module rules
- Each module is its own project with: Controller, Service, DTOs/, Extensions/
- Each module registers its own services via an extension method: `builder.Services.AddMerchantModule()`
- Program.cs calls all module registrations
- Modules reference NipNip.Data and NipNip.Shared
- Modules do NOT reference each other. If tracking needs merchant data, it queries DbContext directly — it doesn't call MerchantService.

### Naming conventions
- Controllers: `MerchantController`, `CreatorController`
- Services: `MerchantService`, `TrackingService`
- DTOs: `RegisterMerchantRequest`, `MerchantResponse`
- Extension methods: `MerchantMappingExtensions`, `MerchantServiceExtensions`
- Custom exceptions: `NotFoundException`, `ConflictException`

## Domain model

### Merchant
```
Id                  Guid PK, auto-generated
ClerkUserId         string, unique — from Clerk JWT "sub" claim
Name                string, required
Slug                string, unique — lowercase, alphanumeric + hyphens
LogoUrl             string, nullable
WebsiteUrl          string, nullable — null for Instagram-only sellers
InstagramHandle     string, nullable
Description         string, nullable
CommissionPercent   decimal(18,2) — e.g. 8.00 means 8%
Balance             decimal(18,2) — prepaid, commissions deducted from here
ApiKey              string, unique — auto-generated on registration (Guid.NewGuid().ToString("N"))
IsActive            bool, default true
CreatedAt           DateTimeOffset, UTC
```

### Creator
```
Id                  Guid PK
ClerkUserId         string, unique
Name                string, required
Slug                string, unique
AvatarUrl           string, nullable
InstagramHandle     string, nullable
TiktokHandle        string, nullable
IsActive            bool, default true
CreatedAt           DateTimeOffset, UTC
```

### Click
```
Id                  Guid PK
MerchantId          Guid FK → Merchant
CreatorId           Guid FK → Creator
RefCode             string — "{creatorSlug}_{merchantSlug}"
IpAddress           string, nullable
UserAgent           string, nullable
ClickedAt           DateTimeOffset, UTC
```

### Conversion
```
Id                  Guid PK
MerchantId          Guid FK → Merchant
CreatorId           Guid FK → Creator
ClickId             Guid FK → Click, nullable (null for manual reports)
OrderId             string — composite unique with MerchantId
OrderAmount         decimal(18,2)
CommissionAmount    decimal(18,2) — OrderAmount * Merchant.CommissionPercent / 100
Currency            string, default "GEL"
Source              enum: JsSnippet, WooCommercePlugin, ManualReport, Api
Status              enum: Pending, Confirmed, Rejected, Paid
CreatedAt           DateTimeOffset, UTC
```

### Payout
```
Id                  Guid PK
CreatorId           Guid FK → Creator
Amount              decimal(18,2)
Currency            string, default "GEL"
Status              enum: Pending, Processing, Completed
PeriodStart         DateTimeOffset, UTC
PeriodEnd           DateTimeOffset, UTC
PaidAt              DateTimeOffset, nullable
```

## API endpoints

### RedirectController (public, no auth)
- `GET /r/{creatorSlug}/{merchantSlug}` — log click, set cookie `_nn_ref={creatorSlug}_{merchantSlug}` (30 days, SameSite=None, Secure), redirect to merchant's WebsiteUrl with `?ref=` appended. If merchant has no WebsiteUrl, return 404.

### ConversionController (mixed auth)
- `POST /api/conversions/track` — public, authenticated by `X-Merchant-Key` header. Body: `{ ref, orderId, amount, currency }`. Deduplicate on MerchantId + OrderId. Parse ref to find creator + merchant. Calculate commission. If merchant balance >= commission, set status Confirmed and deduct. Otherwise set Pending.
- `POST /api/conversions/manual` — merchant auth required. Body: `{ creatorSlug, orderAmount, orderId?, currency? }`. Same logic but Source = ManualReport.

### MerchantController (auth required unless noted)
- `GET /api/merchants` — public, paginated list of active merchants
- `GET /api/merchants/{slug}` — public, single merchant profile
- `POST /api/merchants` — register as merchant
- `PUT /api/merchants/{id}` — update own profile
- `GET /api/merchants/me/dashboard` — stats: total clicks, conversions, balance spent, top creators. Date range filter via query params.

### CreatorController (auth required unless noted)
- `GET /api/creators/{slug}` — public
- `POST /api/creators` — register as creator
- `PUT /api/creators/{id}` — update own profile
- `GET /api/creators/me/dashboard` — stats: total clicks, conversions, total earned, top merchants. Date range filter.

### PayoutController (auth required)
- `GET /api/payouts/me` — creator's payout history
- `POST /api/payouts` — admin only, trigger monthly payout processing

## Auth

Clerk JWT validation in middleware:
1. Read `Authorization: Bearer {token}`
2. Validate against Clerk JWKS
3. Extract `sub` claim as ClerkUserId
4. Set on HttpContext.User

Public endpoints: `GET /r/...`, `POST /api/conversions/track`, `GET /api/merchants`, `GET /api/merchants/{slug}`, `GET /api/creators/{slug}`

Merchant-specific endpoints: check that the authenticated ClerkUserId matches a Merchant entity.
Creator-specific endpoints: check that the authenticated ClerkUserId matches a Creator entity.
A user can be both merchant and creator (same ClerkUserId, different entities).

## Exception → HTTP status mapping

```
NotFoundException             → 404
ConflictException             → 409 (duplicate slug, duplicate order)
InsufficientBalanceException  → 422
ForbiddenException            → 403
ArgumentException             → 400
Everything else               → 500
```

All error responses use ProblemDetails format.

## Business rules

- Commission = OrderAmount × Merchant.CommissionPercent / 100
- Commission deducted from merchant Balance immediately on Confirmed conversion
- If balance < commission, conversion stays Pending, merchant needs to top up
- Duplicate conversions (same MerchantId + OrderId) silently return 200 OK
- Click logging must not block the redirect — save click asynchronously if possible, redirect immediately
- All money is `decimal`, never `float` or `double`
- All timestamps are `DateTimeOffset` in UTC
- Slugs: lowercase, alphanumeric + hyphens, validated on creation
- API keys: generated as `Guid.NewGuid().ToString("N")` on merchant registration

## CORS

- Allow frontend origin for all endpoints
- Allow any origin for `POST /api/conversions/track` (merchant websites call this)
- Allow any origin for `GET /r/...`

## What NOT to build

- No product catalog — merchants are the product
- No campaign/partnership system — open marketplace, one commission rate per merchant
- No payment gateway — payouts are manual bank transfers
- No email/push notifications
- No rate limiting
- No caching
- No background jobs
- No unit tests initially
- No Swagger UI customization — default is fine
- No Docker, no docker-compose
- No health checks
- No logging beyond default .NET logging