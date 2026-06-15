# LiteratureClub

A peer-to-peer textbook marketplace for South African university students. Students can list secondhand textbooks for sale, browse listings, place bids, and pay securely. All scoped to their campus.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database](#database)
- [Seed Data](#seed-data)
- [Project Structure](#project-structure)
- [Roles](#roles)
- [Payment Integration](#payment-integration)
- [Known Limitations](#known-limitations)

---

## Overview

LiteratureClub is an ASP.NET Core 8 MVC web application that connects student sellers and buyers within the same university network. Listings are tied to campuses and course codes, transactions are handled via PayFast, and pickup points are pre-seeded per campus.

---

## Features

- **Listings** — Create, edit, and browse textbook listings with ISBN, title, author, edition, condition, format, and price. Supports photo uploads.
- **Bidding** — Sellers can open listings to bids with an expiry date; buyers can place competing offers.
- **Transactions** — Full buy flow from listing to payment to receipt generation.
- **Wanted Ads** — Students can post wanted ads for textbooks they need, and sellers can respond.
- **Watchlist** — Save listings to a personal watchlist.
- **Dashboard** — Overview of active listings, purchases, sales, bids, and earnings balance.
- **Messaging** — In-app messaging between buyers and sellers scoped to a transaction.
- **Reviews** — Seller reviews tied to completed transactions.
- **Pickup Points** — Campus-specific pickup locations for safe exchanges.
- **Reporting** — Users can report listings, messages, reviews, or other users.
- **Admin** — Seeded admin account with role-based access control.
- **Announcements** — Admin can post platform-wide announcements.
- **Chatbot** - Assist students to navigate through the platform.
- **Translator toggle** - Localisation feature to assit native speakers navigate the app. 
- **Themes** - Alternate between the platform's dark and light mode features.
- **Pagination** - Stopping the overflow of content for users' ease of use.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 MVC |
| Language | C# 12 |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB / SQL Express) |
| Auth | ASP.NET Core Identity |
| Payments | PayFast (sandbox) |
| Email | SendGrid *(optional, not required to run)* |
| Image Processing | SixLabors.ImageSharp |
| Frontend | Bootstrap 5, jQuery, jQuery Validation |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server Express or LocalDB
  - LocalDB ships with Visual Studio; for standalone installs use [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) or VS Code with the C# extension

---

## Getting Started

**1. Clone the repository**

```bash
git clone <your-repo-url>
cd LiteratureClub
```

**2. Configure the connection string**

Open `LiteratureClub/appsettings.json` and update `DefaultConnection` to match your SQL Server instance:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=LiteratureClub;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

For LocalDB use:
```
Server=(localdb)\\mssqllocaldb;Database=LiteratureClub;Trusted_Connection=True;
```

**3. Apply migrations and run**

```bash
cd LiteratureClub
dotnet run
```

Migrations are applied automatically on startup via `context.Database.MigrateAsync()`. Seed data is also inserted automatically on first run.

Alternatively, open `LiteratureClub.slnx` in Visual Studio and press **F5**.

---

## Configuration

All configuration lives in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "SendGrid": {
    "ApiKey": "",
    "SenderEmail": "somasharelitclub@gmail.com",
    "SenderName": "LiteratureClub"
  }
}
```

**SendGrid** is optional. If `ApiKey` is left blank, email sending is skipped silently and user accounts are confirmed automatically on registration.

For sensitive values in development, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
dotnet user-secrets set "SendGrid:ApiKey" "SG.your-key-here"
```

---

## Database

Migrations are managed with EF Core. To add a new migration manually:

```bash
cd LiteratureClub
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

To reset the database entirely:

```bash
dotnet ef database drop
dotnet ef database update
```

The app will re-seed on next startup.

---

## Seed Data

On first run the following is seeded automatically:

**Roles:** `Admin`, `Student`

**Admin account:**
| Field | Value |
|---|---|
| Email | `admin@LiteratureClub.co.za` |
| Password | `Admin@LiteratureClub1!` |

> Change this password immediately after first login in production.

**Universities and campuses** : TUT, Wits, UJ, UP, STADIO, Emeris, and more across South Africa.

**Course codes** : 12 common codes seeded per campus (MATH101, COMP101, ECON101, etc.).

**Pickup points** : Two pickup points seeded per campus (Main Library Entrance, Student Union Building).

---

## Project Structure

```
LiteratureClub/
├── Controllers/
│   ├── AccountController.cs       # Register, Login, Logout
│   ├── ListingsController.cs      # CRUD for textbook listings
│   ├── Bidscontroller.cs          # Bidding logic
│   ├── Transactionscontroller.cs  # Buy flow, receipts
│   ├── Wantedadscontroller.cs     # Wanted ad posts
│   ├── DashboardController.cs     # Student dashboard
│   └── HomeController.cs          # Landing page
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext
│   └── Migrations/                # EF migration history
├── Models/                        # Domain models and view models
├── Services/
│   ├── EmailService.cs            # SendGrid wrapper
│   └── PayFastService.cs          # PayFast payment builder
├── Views/                         # Razor views per controller
├── wwwroot/                       # Static assets (Bootstrap, jQuery, uploads)
├── Program.cs                     # App bootstrap and DI configuration
└── appsettings.json               # Configuration
```

---

## Roles

| Role | Access |
|---|---|
| `Student` | Register, list books, buy, bid, message, review |
| `Admin` | All student access + announcements, reports management, user oversight |

Role assignment happens at registration (`Student`) or via the seeded admin account.

---

## Payment Integration

Payments are processed through **PayFast** in sandbox mode. The `PayFastService` builds the signed payment payload and redirects the buyer to the PayFast sandbox URL.

To switch to live payments:
1. Replace sandbox credentials in `PayFastService.cs` with your live merchant ID and key.
2. Change `SandboxUrl` to `https://www.payfast.co.za/eng/process`.
3. Ensure your server is HTTPS and publicly accessible for ITN (Instant Transaction Notifications).

---

## Known Limitations

- PayFast is configured for sandbox only. Do not use live credentials without completing PayFast's merchant verification.
- Image uploads are stored in `wwwroot/uploads/listings/`. These are not persisted between deployments unless the folder is mapped to persistent storage.
