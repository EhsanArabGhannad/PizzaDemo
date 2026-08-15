# Pizza Knight

A responsive Pizza Knight ordering demo being migrated to ASP.NET Core MVC.

## Run locally

The active application targets .NET 8. From the project directory, run:

```powershell
dotnet run --project PizzaNight/PizzaNight/PizzaNight.csproj
```

Then open the HTTP address printed by ASP.NET Core (the default project profile
uses <http://localhost:5067>).

Stop the server with `Ctrl+C`.

The original static files remain at the repository root during the migration.
The ASP.NET Core version is served from `PizzaNight/PizzaNight`.

## Current scope

The responsive ordering journey currently includes:

- Homepage and sample category-filtered menu
- Pizza size, crust and extras customisation
- Persistent basket with quantity controls and item removal
- Delivery and collection order types
- A £2.50 delivery fee and one fixed 50p service fee per non-empty order
- Test checkout with customer contact details, delivery address and order notes
- Pizza Knight's published address, telephone number, opening hours and directions
- An EF Core 8 SQLite development database with menu and order entities
- A database-backed menu endpoint at `/api/menu`
- A server-validated order endpoint at `POST /api/orders`
- Server-side product, option, price and fee calculation with per-client rate limiting
- A protected order-management dashboard at `/admin`
- Controlled kitchen status transitions from pending through completion
- Automatic initial migration and demo-menu seeding on first run

## Database

The development connection string uses `App_Data/pizza-knight.db`. Database
files and development data-protection keys are ignored by Git and are created
automatically when the application starts.

The initial migration is stored under `PizzaNight/PizzaNight/Data/Migrations`.
To apply migrations manually, run:

```powershell
dotnet ef database update --project PizzaNight/PizzaNight/PizzaNight.csproj
```

Prices are stored as integer pence in the database and converted to pounds in
the API response.

## Development admin

Open `/admin` and use the development-only credentials from
`PizzaNight/PizzaNight/appsettings.Development.json`. Before any non-development
deployment, provide `Admin__Username` and `Admin__Password` through secure
environment configuration. The application will refuse to start without an
admin username and a password of at least 12 characters.

Test checkout submissions are retained in the local development database. This
is still a demonstration only: no payment is processed and no order is sent to
the restaurant. Published opening hours must be confirmed with the shop before
the production launch.
