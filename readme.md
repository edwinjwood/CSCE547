# Dirt Bike Park Booking CLI

## Overview

This repository contains a .NET 9 console application that fulfils the Phase 1 API requirements for a dirt bike park booking portal. Instead of exposing HTTP endpoints, the behaviors are mapped to an interactive command-line interface so you can:

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
│     │  └─ Repositories/    # SQLite repositories (parks + bookings), in-memory cart
│     │     └─ Storage/      # `SqliteConnectionFactory` + schema bootstrap
│     ├─ Presentation/       # CLI pages + menu renderer
│     └─ Program.cs          # Entry point (awaits `Application.RunAsync`)
└─ README.md
```

## How It Works

### Startup & Composition (`App/Application.cs`)

1. Builds the path `data/dirtbikepark.db` under the app base directory.
2. Initializes a `SqliteConnectionFactory`, ensuring the schema exists (`parks`, `park_availability`, `bookings`, `booking_reserved_dates`).
3. Wires repositories and services:
   - `SqliteParkRepository` and `SqliteBookingRepository` provide durable persistence.
   - `InMemoryCartRepository` keeps carts session-local for now.
   - `MockPaymentProcessor` simulates external payment success/failure.
4. Invokes `DataSeeder.SeedParksAsync` to populate initial parks only when the database is empty.
5. Instantiates presentation components (`MenuRenderer`, `CommandRouter`) to drive the CLI loop.

### Domain Services (`Domain/Services`)

| Service | Responsibility |
| --- | --- |
| `ParkService` | Query parks, update guest limits/pricing, manage availability windows |
| `BookingService` | Check availability, reserve/release guests + dates, manage booking lifecycle |
| `CartService` | Track cart items, maintain undo stack, calculate totals/discounts |
| `PaymentService` | Validate card details (Luhn, expiry, CVC) and delegate to payment processor |

Entities (`Park`, `Booking`, `Cart`, `CartItem`) enforce invariants such as capacity and reservation rules, while value objects (`Money`, etc.) encapsulate formatting and arithmetic.

### Persistence (`Infrastructure/Repositories`)

- **SQLite-backed repositories** use `Microsoft.Data.Sqlite` to store parks, bookings, and reserved dates. Operations run within transactions and convert domain types (e.g., `DateOnly`) into persisted ISO strings.
- **Cart repository** remains in-memory; carts are recreated per CLI session. The TODO is to swap this with a SQLite implementation if persistent carts become a requirement.

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

## Running the CLI

```powershell
cd "c:\VS Projects\CSCE547\Dirt Bike Park App\DirtBikePark.Cli"
dotnet build
dotnet run
```

When the app starts you’ll see a welcome banner with a numbered list of parks. Choose a park to step through the booking wizard. After finishing or skipping, you’ll land at the `dbp >` prompt—type `help` at any time to view commands.

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
