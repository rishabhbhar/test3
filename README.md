# Auth, Inventory & Orders Microservices

.NET 8 Web API microservices with SQL Server, JWT authentication, and role-based
access control (ADMIN / USER), built as three independently deployable services.

## Architecture

```
Microservices/
├── Microservices.sln
├── docker-compose.yml
├── Database/
│   ├── auth_db.sql
│   ├── inventory_db.sql
│   └── order_db.sql
└── src/
    ├── Common/              # Shared library: JWT auth wiring, exception middleware,
    │                        # role constants, Admin-or-internal-service authorization
    ├── AuthService/         # Issues JWTs. Owns auth_db.
    ├── InventoryService/    # Product CRUD + stock. Owns inventory_db.
    └── OrderService/        # Order orchestration. Owns order_db. Calls InventoryService over HTTP.
```

Each service owns its own database (database-per-service) and is independently
runnable/deployable. `Common` is a shared library (not a running service) referenced
by all three so the JWT validation logic, exception handling, and role constants stay
consistent - it is **not** a shared database or a bypass of service boundaries.

```
                  ┌──────────────┐
   register/login │ AuthService  │  issues JWT (HS256, roles: ADMIN/USER)
   ───────────────▶  (auth_db)   │
                  └──────────────┘
                         │ JWT (shared secret/issuer/audience)
                         ▼
   ┌─────────────┐   validates JWT    ┌──────────────────┐
   │ OrderService│───────────────────▶│ InventoryService │
   │ (order_db)  │  HTTP: GET product,│   (inventory_db) │
   │             │  reduce/restore    │                   │
   └─────────────┘  stock             └──────────────────┘
```

## Services & Endpoints

### Auth Service (`:5001`)
| Method | Route               | Access        |
|--------|---------------------|---------------|
| POST   | `/api/auth/register`| Public        |
| POST   | `/api/auth/login`   | Public        |
| GET    | `/api/auth/me`      | Any valid JWT |

### Inventory Service (`:5002`)
| Method | Route                              | Access                              |
|--------|--------------------------------------|--------------------------------------|
| POST   | `/api/products`                     | ADMIN                               |
| GET    | `/api/products` (paginated)         | Any valid JWT                       |
| GET    | `/api/products/{id}`                | Any valid JWT                       |
| PUT    | `/api/products/{id}`                | ADMIN                               |
| DELETE | `/api/products/{id}`                | ADMIN                               |
| POST   | `/api/products/{id}/reduce_stock`   | ADMIN **or** internal service call  |
| POST   | `/api/products/{id}/restore_stock`  | ADMIN **or** internal service call  |

### Order Service (`:5003`)
| Method | Route                       | Access             |
|--------|------------------------------|---------------------|
| POST   | `/api/orders`                | Any valid JWT (USER)|
| GET    | `/api/orders/my-orders`      | Any valid JWT       |
| GET    | `/api/orders/{id}`           | Owner or ADMIN      |
| PATCH  | `/api/orders/{id}/cancel`    | Owner or ADMIN      |
| GET    | `/api/orders` (paginated)    | ADMIN (bonus, beyond the base spec, to satisfy "Admin can see all orders") |

All routes above except `register`/`login` require `Authorization: Bearer {token}`.

## Design Decisions Worth Knowing About

### 1. Why does `reduce_stock` accept more than just Admin tokens?
The requirements state "Only Admin can add/update stock" **and** that a regular USER
placing an order must trigger a stock reduction. Those two requirements only both hold
if the *direct* HTTP caller distinction is between "an end user hitting the endpoint
themselves" (must be Admin) vs. "a trusted backend service calling on the system's
behalf" (allowed regardless of the placing user's role).

This is implemented with a custom authorization policy (`AdminOrInternalServiceHandler`
in `Common`) that succeeds if **either**:
- the caller is an authenticated ADMIN (normal JWT role check), **or**
- the request carries a valid `X-Internal-Api-Key` header - a shared secret only
  OrderService is configured with.

In production this shared-secret approach would typically be replaced by mTLS,
a service-mesh identity, or OAuth2 client-credentials tokens between services -
a static key keeps this sample self-contained and easy to run locally.

### 2. How is "transactional" order creation implemented across two databases?
A true ACID transaction across OrderService's SQL Server database and
InventoryService's SQL Server database isn't possible (they're different services/
databases, contacted over HTTP) - and attempting a two-phase commit across services is
a well-known microservices anti-pattern. Instead, `OrderProcessingService` implements a
**saga with compensation**:

1. **Check stock** - call InventoryService for every line item first; reject
   immediately if any product is missing, inactive, or under-stocked (nothing is
   mutated yet).
2. **Deduct stock** - call `reduce_stock` for each item in turn. If any call fails
   partway through, everything already deducted is rolled back via `restore_stock`
   (compensating transaction), and the whole order is rejected.
3. **Create order + items, mark CONFIRMED** - persisted in a single local SQL Server
   transaction in `order_db`. If this local save somehow fails after stock was already
   reduced, the already-reduced stock is compensated (restored) as well.

`InventoryService.ProductRepository.TryReduceStockAsync` additionally uses a single
atomic `UPDATE ... WHERE stock_qty >= @quantity` (via EF Core's `ExecuteUpdateAsync`)
rather than a read-then-write, so concurrent orders can never oversell the same
product.

### 3. Cancellation
`PATCH /api/orders/{id}/cancel` restores stock for every line item (calls
`restore_stock` on InventoryService) and transitions the order to `CANCELLED`. Already-
cancelled orders return a 400 business-rule error rather than silently succeeding.

### 4. Cross-cutting concerns
- **JWT validation**: identical `Jwt:Secret` / `Issuer` / `Audience` across all three
  services' config, wired via `Common.Security.AddSharedJwtAuthentication`. AuthService
  issues; InventoryService and OrderService validate only (pure resource servers).
- **RBAC**: `[Authorize(Roles = "ADMIN")]` on admin-only endpoints; ownership checks
  (`order.UserId == callerId`) enforced in the service layer for user-scoped resources.
- **Global exception handling**: `Common.Middleware.ExceptionHandlingMiddleware` in
  every service maps `NotFoundException` → 404, `BusinessRuleException` → 400,
  `ForbiddenException` → 403, everything else → 500 with no stack trace leakage, all
  logged with a trace ID.
- **Logging**: structured `ILogger` calls at key points (registration, login attempts,
  stock changes, order state transitions) in every service.

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server 2019+ (local install, Docker container, or Azure SQL) - **or** just use
  the provided `docker-compose.yml`, which runs SQL Server for you.

## Running locally (without Docker)

1. Start a local SQL Server instance and run the three scripts in `Database/` (each
   creates its own database):
   ```bash
   sqlcmd -S localhost -U sa -P '<your-password>' -i Database/auth_db.sql
   sqlcmd -S localhost -U sa -P '<your-password>' -i Database/inventory_db.sql
   sqlcmd -S localhost -U sa -P '<your-password>' -i Database/order_db.sql
   ```
2. Update the connection strings and `Jwt`/`InternalApi` sections in each service's
   `appsettings.json` if you changed the SA password (they must **stay identical to
   each other** across all three services for JWT/internal-key validation to work).
3. Run each service in its own terminal:
   ```bash
   cd src/AuthService      && dotnet run   # http://localhost:5001/swagger
   cd src/InventoryService && dotnet run   # http://localhost:5002/swagger
   cd src/OrderService      && dotnet run   # http://localhost:5003/swagger
   ```
4. Use `EndToEnd.http` (VS Code REST Client / Rider / Visual Studio) to walk through
   register → login → create product → place order → cancel order.

## Running with Docker Compose (SQL Server + all 3 services)

```bash
docker compose up --build
```
This starts SQL Server, waits for it to be healthy, runs all three `Database/*.sql`
scripts via a one-off init container, then starts AuthService (`:5001`),
InventoryService (`:5002`), and OrderService (`:5003`).

## Security Notes Before Deploying This For Real
- Replace `Jwt:Secret` and `InternalApi:ApiKey` with strong, randomly generated values
  stored in a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.) - not in
  `appsettings.json`.
- Consider RS256 (asymmetric signing) instead of HS256 so InventoryService/OrderService
  only hold a public key, not a secret capable of minting tokens.
- Put real TLS in front of all three services and set `RequireHttpsMetadata = true`.
- Replace the shared internal API key with mTLS or OAuth2 client-credentials for
  service-to-service calls.

## Next Steps You May Want
- EF Core migrations (`dotnet ef migrations add InitialCreate`) per service instead of
  relying solely on the raw SQL scripts.
- Integration tests using `WebApplicationFactory` + Testcontainers for SQL Server.
- An API Gateway (YARP / Ocelot) in front of all three services for a single public
  entry point, rate limiting, and centralized JWT validation.
- Replace polling/HTTP saga compensation with an event-driven saga (e.g. via a message
  broker) for better resilience if InventoryService is temporarily unavailable.
