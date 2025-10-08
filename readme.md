# Dirt Bike Park Booking CLI

## Overview

This repository contains a .NET 9 application that fulfils the Phase 1 API requirements for a dirt bike park booking portal. It now ships with two entry points:

- A **REST API** (default) exposing park CRUD endpoints for integration scenarios.
- An **interactive CLI** (opt-in via `--cli`) that mirrors the same business capabilities for quick manual testing.

Both experiences share the same domain, persistence, and payment layers so you can:

- Browse parks and view date/guest availability
- Create, cancel, and remove bookings tied to parks
- Manage a cart (add/remove/undo) with automatic pricing and bundle discounts
- Collect payment details, validate them, and simulate payment processing

The solution is layered to showcase C# fundamentals, OOP design, and collection usage in line with the course rubric.

## Project Structure

```text
CSCE547/
├─ Dirt Bike Park App/
│  └─ DirtBikePark.Cli/
│     ├─ App/                # Composition root + command routing
│     ├─ Data/               # Seed data helpers
│     ├─ Domain/             # Entities, services, interfaces, value objects
│     ├─ Infrastructure/
│     │  ├─ Payments/        # Mock payment processor
│     │  └─ Repositories/    # SQLite repositories (parks, bookings, carts)
│     │     └─ Storage/      # `DirtBikeParkDbContextFactory` + schema bootstrap
│     ├─ Presentation/       # CLI pages + menu renderer
│     └─ Program.cs          # Entry point (awaits `Application.RunAsync`)
└─ README.md
```

## How It Works

### Startup & Composition (`App/Application.cs`)

1. Builds the path `data/dirtbikepark.db` under the app base directory.
2. Initializes a `SqliteConnectionFactory`, ensuring the schema exists (`parks`, `park_availability`, `bookings`, `booking_reserved_dates`).
3. Wires repositories and services:
   - `SqliteParkRepository`, `SqliteBookingRepository`, and `SqliteCartRepository` provide durable persistence across API and CLI modes.
   - `MockPaymentProcessor` simulates external payment success/failure.
4. Invokes `DataSeeder.SeedParksAsync` to populate initial parks only when the database is empty.
5. Instantiates presentation components (`MenuRenderer`, `CommandRouter`) to drive the CLI loop when running in CLI mode.

### REST API Surface (`Program.cs` + `Presentation/Api`)

- Uses ASP.NET Core minimal APIs and the existing EF Core repositories.
- Seed data is applied on startup just like the CLI.
- Endpoint groups:
   - **Parks** (`/api/parks`)
      - `GET /api/parks` – list all parks
      - `GET /api/parks/{id}` – fetch a single park
      - `POST /api/parks` – create a park (body requires name, description, location, guest limit, price, optional availability dates)
      - `PUT /api/parks/{id}` – replace park details and availability
      - `DELETE /api/parks/{id}` – remove a park
   - **Bookings** (`/api/bookings`, `/api/parks/{parkId}/bookings`)
      - `GET /api/bookings` – list all bookings
      - `GET /api/bookings/{id}` – fetch a booking by id
      - `DELETE /api/bookings/{id}` – remove a booking (releases capacity)
      - `GET /api/parks/{parkId}/bookings` – list bookings for a park
      - `POST /api/parks/{parkId}/bookings` – create a booking for a park (supports single-day or multi-day reservations)
   - **Carts** (`/api/carts`)
      - `GET /api/carts?id={cartId}` – get an existing cart or create a new cart when omitted
      - `POST /api/carts/{cartId}/items` – add an existing or newly created booking to the cart
      - `PUT /api/carts/{cartId}/items/{bookingId}/remove` – remove a booking from the cart and return updated totals

> **Tip:** Date values are accepted and returned as ISO `yyyy-MM-dd` strings.

### Domain Services (`Domain/Services`)

| Service | Responsibility |
| --- | --- |
| `ParkService` | Query parks, update guest limits/pricing, manage availability windows |
| `BookingService` | Check availability, reserve/release guests + dates, manage booking lifecycle |
| `CartService` | Track cart items, maintain undo stack, calculate totals/discounts |
| `PaymentService` | Validate card details (Luhn, expiry, CVC) and delegate to payment processor |

Entities (`Park`, `Booking`, `Cart`, `CartItem`) enforce invariants such as capacity and reservation rules, while value objects (`Money`, etc.) encapsulate formatting and arithmetic.

### Persistence (`Infrastructure/Repositories`)

- **SQLite-backed repositories** use Entity Framework Core with `Microsoft.EntityFrameworkCore.Sqlite` to store parks, bookings, carts, and related child records. LINQ queries drive the CRUD operations while value converters serialize `DateOnly` fields.

### Presentation (`Presentation/*`)

- `MenuRenderer` centralizes console formatting and help text.
- `CommandRouter` runs the REPL-like command loop.
- CLI pages (`ParksPage`, `BookingsPage`, `CartPage`, `CheckoutPage`) orchestrate prompts and delegate to domain services.
- A dedicated welcome flow helps users pick a park, configure dates/guests, and jump into the cart without typing GUIDs.

### Collections & Patterns Demonstrated

- `List<T>` / `SortedSet<T>` for park availability
- `Dictionary<TKey, TValue>` for cart items
- `Stack<T>` for undo history
- LINQ for filtering and projections
- Transactions and prepared statements in SQLite to enforce data integrity

## Prerequisites

- .NET SDK 9.0 or higher
- Windows PowerShell 5.1 (default shell for provided commands)

## Running the REST API

```powershell
cd "c:\VS Projects\CSCE547\Dirt Bike Park App\DirtBikePark.Cli"
dotnet build
dotnet run
```

The API listens on `http://localhost:5000` by default (override with `--urls`).

### Quick Endpoint Demo (PowerShell)

```powershell
# List seeded parks and capture one id
$parks = Invoke-RestMethod -Uri "http://localhost:5000/api/parks"
$parkId = $parks[0].id

# Create a park
$body = @{
   name = "Foothills Trail Center"
   description = "Flowy single track with beginner clinics"
   location = "Asheville, NC"
   guestLimit = 20
   pricePerGuestPerDay = 95
   currency = "USD"
   availableDates = @( (Get-Date).AddDays(1).ToString('yyyy-MM-dd') )
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/api/parks" -Method Post -Body $body -ContentType "application/json"

# Create a single-day booking for an existing park
$booking = Invoke-RestMethod -Uri "http://localhost:5000/api/parks/$parkId/bookings" -Method Post -Body (@{
   guestName = "Guest"
   guests = 1
   dayCount = 1
   guestCategoryName = "Adult"
   date = (Get-Date).AddDays(2).ToString('yyyy-MM-dd')
} | ConvertTo-Json) -ContentType "application/json"

# Create or retrieve a cart (omit id to create)
$cart = Invoke-RestMethod -Uri "http://localhost:5000/api/carts"

# Add the booking to the cart
$addBody = @{
   bookingId = $booking.id
   quantity = 1
} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/api/carts/$($cart.cartId)/items" -Method Post -Body $addBody -ContentType "application/json"
```

## Running the CLI

```powershell
cd "c:\VS Projects\CSCE547\Dirt Bike Park App\DirtBikePark.Cli"
dotnet build
dotnet run -- --cli
```

When the CLI starts you’ll see a welcome banner with a numbered list of parks. Choose a park to step through the booking wizard. After finishing or skipping, you’ll land at the `dbp >` prompt—type `help` at any time to view commands.

### Suggested Walkthrough

1. `parks list` – inspect seeded parks and capacity
2. `bookings create` – follow prompts to reserve dates/guests
3. `cart show` – review cart totals, then `cart undo` to test history
4. `checkout` – enter payment details (even final digit ⇒ success; odd ⇒ decline)

> **Resetting data:** Delete `DirtBikePark.Cli/data/dirtbikepark.db` to reseed the database on the next run.

## Troubleshooting

| Issue | Fix |
| --- | --- |
| “database is locked” | Ensure only one CLI instance is running; SQLite restricts concurrent writers. |
| Seed data missing | Remove the `data/dirtbikepark.db` file and rerun; seeding only happens against an empty DB. |
| Payment always failing | Use a card number ending with an even digit; odd digits intentionally trigger declines. |

## Extensibility Notes

- Replace the in-memory cart repository with a SQLite-backed version for persistent carts across sessions.
- Add admin-focused commands for park CRUD by extending `ParkService` and `CommandRouter`.
- Introduce a configuration file (JSON) for tuning discounts, tax rates, or future pricing tiers.

Feel free to experiment with new commands, tweak pricing rules, or extend the persistence layer to explore advanced scenarios.
