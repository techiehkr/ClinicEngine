# ClinicEngine

Clinic appointment booking system — patients can book slots with doctors, admins get a live dashboard. Built this to handle the race condition problem properly (two people booking the same slot at the same time).

## Stack

ASP.NET Core 8, SQL Server, EF Core 8, MediatR, FluentValidation, xUnit

## Setup



```bash
git clone https://github.com/YOUR-USERNAME/ClinicEngine.git
cd ClinicEngine
dotnet restore
```

Update the connection string in `src/ClinicEngine.Web/appsettings.json` if needed — default points to LocalDB which should just work if you have VS installed.

Run migrations:

```bash
dotnet ef migrations add InitialCreate --project src/ClinicEngine.Infrastructure --startup-project src/ClinicEngine.Web
dotnet ef database update --project src/ClinicEngine.Infrastructure --startup-project src/ClinicEngine.Web
```

Start the app:

```bash
cd src/ClinicEngine.Web
dotnet run
```

First startup seeds the DB automatically — 5 departments, 10 doctors, 20 patients, sample bookings. Dashboard is at `/admin/dashboard`, API docs at `/swagger`.

If you hit an SSL warning in the browser: `dotnet dev-certs https --trust`

## Project Layout

```
src/
  ClinicEngine.Domain/           # entities, business rules, exceptions — no dependencies
  ClinicEngine.Application/      # use cases, CQRS handlers, repository interfaces
  ClinicEngine.Infrastructure/   # EF Core, SQL Server, repository implementations
  ClinicEngine.Web/              # API controllers, Razor Pages, middleware

tests/
  ClinicEngine.UnitTests/
  ClinicEngine.IntegrationTests/
```

Dependencies only go inward. Domain knows nothing. Application knows Domain. Infrastructure implements Application's interfaces. Web wires it all up.

## How the Concurrency Problem is Solved

This was the main challenge. Multiple patients booking the same slot simultaneously shouldn't both succeed.

Three layers handle this:

**Pessimistic lock** — when a booking starts, we immediately lock the availability row in SQL Server using `UPDLOCK, ROWLOCK`. Other transactions targeting that same slot block until the first one finishes. This is done with a raw SQL query because EF Core doesn't support lock hints natively.

**Optimistic lock** — Appointments table has a `ROWVERSION` column. EF Core adds `WHERE RowVersion = @original` to every update. If two transactions read the same row and both try to update it, the second one fails with a concurrency exception, which we catch and return as 409.

**Unique index** — last safety net. Filtered unique index on `(DoctorId, AppointmentDateTime)` where status isn't cancelled. Even if the application-level checks somehow fail, the database rejects the duplicate.

## Error Handling

Everything goes through a single middleware. Domain exceptions, validation failures, DB conflicts — all caught in one place and returned as RFC 7807 problem details. No stack traces ever leave the server. The response includes a `traceId` which maps to the full error in server logs.

## Audit Trail

Every table has `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `IsDeleted`. These are filled automatically by an EF Core interceptor before every save — nothing in the business logic sets them manually. Appointment status changes are also logged to `AppointmentHistories` — immutable records, never updated or deleted.

## API

Full docs at `/swagger`. Quick reference:

```
GET  /api/appointments                    paginated list, filter by patient/doctor/date
GET  /api/appointments/available-slots    free slots for a doctor on a given date
POST /api/appointments                    book a slot
PUT  /api/appointments/{id}/reschedule    move to different time
PUT  /api/appointments/{id}/cancel
PUT  /api/appointments/{id}/complete
GET  /api/doctors                         paginated, filter by dept/specialization/availability
GET  /api/departments
```

All list endpoints are server-side paginated. No client-side filtering.

## Dashboard

`/admin/dashboard` — Razor Page showing summary cards, today's schedule, and a Chart.js breakdown by department. Schedule table auto-refreshes every 30s by fetching a partial view from the server — no SPA, no separate API call for the refresh, just server-rendered HTML swapping in.

## Tests

```bash
dotnet test                    # run everything
dotnet test --verbosity normal # with output
```

Unit tests cover domain entity behaviour and command handler logic with mocked dependencies. Integration tests run the full stack with an in-memory database.



## Scaling Notes

No server-side state so horizontal scaling works without sticky sessions. Locking is handled at the database level so it works correctly across multiple instances. For higher load: add Redis for reference data caching, point dashboard reads at a read replica, expose `/health` for load balancer probing.

