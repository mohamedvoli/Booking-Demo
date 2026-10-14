# Hotel Bookings — Technical Assessment

**Role:**  .NET Developer
**Track:** Modular Monolith Architecture with ASP.NET Core
**Stack:** .NET 10 · ASP.NET Core Web APIs · Entity Framework Core 10 · SQL Server 
**Document version:** 1.0

---

## 1. Overview

### 1.1 Purpose

In this assessment you will build the back end of a **hotel bookings application** as a **modular monolith** in ASP.NET Core. The application manages every room in the hotel and every booking made there. Hotel staff use it to:

- maintain the room inventory and each room's amenities,
- create bookings for guests and automatically assign the cheapest suitable room,
- cancel bookings, check guests in and check guests out,
- see a daily front-desk view of today's arrivals and departures,
- see hotel occupancy for the next 7 days

We are evaluating whether you can:

1. Turn business rules into a clean, testable domain model.
2. Split a single deployable application into **well-isolated modules** with explicit contracts between them.
3. Use EF Core properly: code-first models, migrations and one `DbContext` per module.
4. Build a predictable HTTP API with validation, authorization and consistent error responses.
5. Write focused unit tests and integration tests.

The scope is deliberately small. **We value correctness, clarity and clean boundaries over feature count or clever infrastructure.** You do not need message brokers, microservices, containers in production, CQRS read stores or event sourcing.

### 1.2 Estimated Effort

| Part | Estimated time |
|---|---|
| Core scope (Sections 4.1 – 4.8) | 16 – 22 hours |
| Extra challenge: Room Service (Section 4.9) | 2 – 4 hours |

You will normally have **5 calendar days** from receiving this document to submit. Partial submissions are accepted. A smaller, well-built solution scores higher than a complete but tangled one.

### 1.3 Roles (Actors)

| Role | Description |
|---|---|
| **Hotel Manager** | Full access. The only role that can create or edit rooms. |
| **Hotel Clerk** | Day-to-day front-desk operations: bookings, check-in/out, amenities, room service. Can view rooms but cannot create or edit them. |

Every endpoint in the application requires an authenticated user with at least one of these two roles. Anonymous access is not allowed, except for the login endpoint and (in Development only) the OpenAPI document.

---

## 2. Architecture & Technical Guidelines

### 2.1 Architectural Style: Modular Monolith

The solution is **one deployable ASP.NET Core application** (the *Host*) made up of independent **modules**. Each module:

- owns a single business capability and its data,
- has its own `DbContext` mapped to its own **database schema**,
- exposes behaviour to other modules **only** through a public **Contracts** project,
- keeps its domain, application logic and persistence `internal` wherever practical.

You could extract a module into a separate service later without rewriting its internals. You are **not** asked to do this.

### 2.2 Module Breakdown

| Module | Responsibility | Owns (tables) | Schema |
|---|---|---|---|
| **Identity** | Users, roles, authentication | ASP.NET Core Identity tables | `identity` |
| **Rooms** | Room inventory, amenities, room–amenity assignment | `Rooms`, `Amenities`, `RoomAmenities` | `rooms` |
| **Bookings** | Bookings, booking status lifecycle, room availability, front-desk views (today's arrivals/departures, occupancy) | `Bookings`, `BookingStatuses` | `bookings` |

> **Why do front-desk views live in Bookings?** Arrivals, departures and occupancy are all questions about bookings. Bookings owns the data, so Bookings answers them. It uses the Rooms contract for room numbers and the total room count.

### 2.3 Solution Layout

The solution **must** follow this structure. Names may use your own root namespace instead of `Hotel`.

```text
Hotel.sln
│
├── src/
│   ├── Host/
│   │   └── Hotel.Api/                         # Composition root: Program.cs, auth, OpenAPI, module registration
│   │
│   ├── Shared/
│   │   ├── Hotel.Shared.Kernel/               # Result<T>, Error, base Entity, domain event abstractions, IDomainEvent
│   │   └── Hotel.Shared.Infrastructure/       # ProblemDetails handler, EF conventions, in-process event bus, auth policies
│   │
│   └── Modules/
│       ├── Identity/
│       │   └── Hotel.Modules.Identity/        # Identity is mostly framework-provided; a single project is fine
│       │
│       ├── Rooms/
│       │   ├── Hotel.Modules.Rooms.Contracts/       # PUBLIC: IRoomsModuleApi, DTOs, integration events
│       │   ├── Hotel.Modules.Rooms.Domain/          # Room, RoomAmenity, Amenity, domain rules
│       │   ├── Hotel.Modules.Rooms.Application/     # Use cases, validators, IRoomsModuleApi implementation
│       │   └── Hotel.Modules.Rooms.Infrastructure/  # RoomsDbContext, configurations, migrations, endpoints, RoomsModule.cs
│       │
│       ├── Bookings/
│       │   ├── Hotel.Modules.Bookings.Contracts/
│       │   ├── Hotel.Modules.Bookings.Domain/
│       │   ├── Hotel.Modules.Bookings.Application/
│       │   └── Hotel.Modules.Bookings.Infrastructure/
│
├── seed/
│   └── rooms.csv                              # Converted from the provided Room.xls
│
├── Directory.Build.props                      # Nullable, ImplicitUsings, TreatWarningsAsErrors, LangVersion
├── Directory.Packages.props                   # Central package management
├── .editorconfig
└── README.md
```

Each module exposes **one** registration entry point, which the Host calls:

```csharp
// Hotel.Api/Program.cs (illustrative)
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSharedInfrastructure(builder.Configuration)
    .AddIdentityModule(builder.Configuration)
    .AddRoomsModule(builder.Configuration)
    .AddBookingsModule(builder.Configuration)
    .AddRoomServiceModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityModule();
app.MapRoomsModule();
app.MapBookingsModule();
app.MapRoomServiceModule();

app.Run();
```

### 2.4 Layer Responsibilities (inside a module)

| Layer | Contains | May reference |
|---|---|---|
| **Domain** | Entities, value objects, enums, domain rules and invariants, domain errors. No EF Core, no ASP.NET Core. | `Shared.Kernel` only |
| **Application** | Use-case handlers/services, request/response models, validators, interfaces it needs (e.g. `IBookingsRepository`), implementation of the module's public API. | Own Domain, `Shared.Kernel`, **other modules' Contracts** |
| **Infrastructure** | `DbContext`, entity configurations, migrations, repository implementations, Minimal API endpoint definitions, DI registration (`XxxModule.cs`). | Own Application & Domain, `Shared.Infrastructure` |
| **Contracts** | Public interface (`IXxxModuleApi`), DTOs (`record` types), integration event records. No logic and no EF types. | `Shared.Kernel` only (or nothing) |

### 2.5 Dependency Rules (Non-Negotiable)

1. A module **must not** reference another module's `Domain`, `Application` or `Infrastructure` project. Only `*.Contracts` may cross module boundaries.
2. A module **must not** query or write another module's tables or use another module's `DbContext`. This applies to raw SQL too.
3. **No foreign key constraints across schemas.** `Booking.RoomId` is a plain identifier. Bookings checks that the room exists through `IRoomsModuleApi`.
4. `Contracts` projects contain only interfaces, `record` DTOs and event records. They must not expose domain entities.
5. The Host references each module's `Infrastructure` project (for registration) and nothing else from inside the modules.
6. Domain projects have **no** dependency on EF Core, ASP.NET Core or any other module.
7. These rules **must** be enforced by at least one automated architecture test (see Section 5.3).

### 2.6 Inter-Module Communication

Use the simplest mechanism that fits each case:

| Need | Mechanism | Example in this project |
|---|---|---|
| **Synchronous query** that needs another module's data now | Call the other module's public API interface from its Contracts project. It is registered in DI and resolved in-process. | Bookings calls `IRoomsModuleApi.GetCandidateRoomsAsync(...)` to find rooms with enough capacity, ordered by price. |
| **Synchronous command/check** across modules | Same as above. | RoomService calls `IBookingsModuleApi.GetActiveBookingForRoomAsync(roomId)` before accepting a request. |

Guidance:

- An in-process mediator library is **optional**. A plain interface and DI is perfectly acceptable. If you use a third-party mediator, check its current licence terms and mention your choice in the README.
- Module API methods return **DTOs**, never entities or `IQueryable`.
- Avoid "chatty" calls inside loops. Prefer batch methods such as `GetRoomNumbersAsync(IReadOnlyCollection<Guid> roomIds)`.

Illustrative Rooms contract:

```csharp
namespace Hotel.Modules.Rooms.Contracts;

public interface IRoomsModuleApi
{
    Task<bool> RoomExistsAsync(Guid roomId, CancellationToken ct = default);

    /// Rooms that can host the requested party, cheapest first.
    Task<IReadOnlyList<RoomSummaryDto>> GetCandidateRoomsAsync(
        int numberOfAdults, int numberOfChildren, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> GetRoomNumbersAsync(
        IReadOnlyCollection<Guid> roomIds, CancellationToken ct = default);

    Task<int> GetTotalRoomCountAsync(CancellationToken ct = default);
}

public sealed record RoomSummaryDto(
    Guid Id, string RoomNumber, int AdultsCapacity, int ChildrenCapacity, decimal Price);
```

### 2.7 Data Access (EF Core)

- **Code-first** with **EF Core migrations**. Each module has its own `DbContext` and its own migrations folder.
- All module contexts share **one physical database**. Each uses its own **schema** (`modelBuilder.HasDefaultSchema("bookings")`) and its own migrations history table:

  ```csharp
  options.UseSqlServer(connectionString, sql =>
      sql.MigrationsHistoryTable("__EFMigrationsHistory", "bookings"));
  ```
- Configure entities with `IEntityTypeConfiguration<T>` classes, not data annotations on domain entities.
- Use `Guid` primary keys generated in the application (`Guid.CreateVersion7()` is recommended).
- Use `DateOnly` for check-in/check-out dates and `decimal(10,2)` for money.
- Model fixed reference data (statuses, amenities) as **C# enums** persisted to a **lookup table** seeded with `HasData`. See Section 3.
- Use `AsNoTracking()` for read-only queries. Project to DTOs in the query (`Select`) rather than loading full graphs.
- Do **not** use the EF Core In-Memory provider for tests (see Section 5.3).

### 2.8 API / Exposure Layer

- **ASP.NET Core Web APIs**, grouped per module with `MapGroup("/api/rooms")` and similar.
- Each module defines its endpoints inside its own Infrastructure project, typically in an `Endpoints/` folder.
- Expose an **OpenAPI** document using the built-in `Microsoft.AspNetCore.OpenApi`, plus a UI such as Scalar or Swagger UI in Development.
- Return `TypedResults` so that response types appear in the OpenAPI document.
- Errors **must** be returned as **RFC 9457 Problem Details** (`application/problem+json`). See Section 2.10.
- A front end is **not required**. The original requirements describe screens. In this assessment each screen becomes one or more endpoints, and the API must return everything a UI would need, including which actions are currently allowed (see `availableActions` in Section 4.4). Building a UI is a bonus item.

### 2.9 Authentication & Authorization

- Use **ASP.NET Core Identity** with bearer tokens (`AddIdentityApiEndpoints` / `MapIdentityApi`) or JWT. Keep it simple.
- Seed two roles (`HotelManager`, `HotelClerk`) and at least one user per role at startup in Development. Document the test credentials in the README. Never commit real credentials.
- Define named authorization policies, not scattered role strings:

  ```csharp
  public static class Policies
  {
      public const string Staff   = "Staff";    // HotelManager OR HotelClerk
      public const string Manager = "Manager";  // HotelManager only
  }
  ```

### 2.10 Error Handling & Validation

- **Expected business failures** (e.g. "no room available", "booking cannot be checked in today") are returned as values using a `Result` / `Result<T>` type with a typed `Error`. They must **not** be thrown as exceptions.
- **Unexpected failures** are caught by a global `IExceptionHandler` and returned as a 500 Problem Details response that does not leak stack traces.
- **Input validation** runs before the use case executes. Use .NET 10 built-in API validation (`builder.Services.AddValidation()` with DataAnnotations on request records), or FluentValidation 
- Status code mapping:

| Situation | HTTP status |
|---|---|
| Request is malformed / fails validation | `400 Bad Request` with `errors` dictionary (`ValidationProblemDetails`) |
| Not authenticated | `401 Unauthorized` |
| Authenticated but lacks role | `403 Forbidden` |
| Resource does not exist | `404 Not Found` |
| Valid request that violates a business rule or current state (invalid status transition, duplicate amenity, room no longer available) | `409 Conflict` |
| Unexpected error | `500 Internal Server Error` |

### 2.11 Coding Standards

- `Nullable` enabled and `TreatWarningsAsErrors` on, via `Directory.Build.props`.
- Central package management via `Directory.Packages.props`.
- `async`/`await` end-to-end with a `CancellationToken` passed through. No `.Result` or `.Wait()`.
- **Never read the system clock directly** (`DateTime.Now`, `DateTime.Today`, `DateOnly.FromDateTime(DateTime.Now)`). Inject `TimeProvider` so that "today" can be controlled in tests. The hotel's local time zone comes from configuration (`Hotel:TimeZoneId`).
- Entities protect their invariants: private setters, factory methods (`Booking.Create(...)`) and behaviour methods (`booking.CheckIn(today)`). Avoid anemic entities with public setters and logic spread across services.
- Prefer `sealed` classes and `record` DTOs. Mark module internals `internal`.
- Meaningful names, small methods and no commented-out code. Follow the `.editorconfig`.
- Structured logging with `ILogger<T>`, using message templates rather than string interpolation.

---

## 3. Domain Model

### 3.1 Entities

> All attributes are **mandatory** unless marked *optional*.

#### Rooms module

**Room**

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `RoomNumber` | `string` (max 10) | Required. Unique across the hotel.¹ |
| `AdultsCapacity` | `int` | Required. ≥ 1¹ |
| `ChildrenCapacity` | `int` | Required. ≥ 0¹ |
| `Price` | `decimal(10,2)` | Required. Price **per night**. > 0¹ |
| `Amenities` | collection of `RoomAmenity` | 0..n. No duplicates. |

**Amenity** (fixed reference data, `enum` + lookup table)

| Value | Label |
|---|---|
| 1 | Television |
| 2 | Internet Access |
| 3 | Hair Dryer |
| 4 | Premium Towels |
| 5 | Crib |
| 6 | Safe |

**RoomAmenity** (join entity, owned by `Room`)

| Field | Type | Rules |
|---|---|---|
| `RoomId` | `Guid` | FK → `Room` (same schema, so an FK constraint is allowed) |
| `Amenity` | `Amenity` (enum) | FK → `Amenities` lookup |
| — | — | **Unique** `(RoomId, Amenity)`. A room cannot have the same amenity twice. |

#### Bookings module

**Booking**

| Field | Type | Rules |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `GuestLastName` | `string` (max 100) | Required |
| `GuestFirstName` | `string` (max 100) | Required |
| `CheckInDate` | `DateOnly` | Required |
| `CheckOutDate` | `DateOnly` | Required. Must be **after** `CheckInDate`. |
| `NumberOfAdults` | `int` | Required. ≥ 1 |
| `NumberOfChildren` | `int` | **Optional.** If provided, ≥ 0. Treat missing as 0. |
| `RoomId` | `Guid` | Required. Reference to a Rooms-module room. **No database FK** (cross-module). |
| `Status` | `BookingStatus` (enum) | Required. FK → `BookingStatuses` lookup (same schema). |

**BookingStatus** (fixed reference data, `enum` + lookup table with `Id`, `Label`, `Order`, `IsActive`)

| Id | Label | Order | IsActive |
|---|---|---|---|
| 1 | Booked | 1 | true |
| 2 | CheckedIn | 2 | true |
| 3 | CheckedOut | 3 | true |
| 4 | Canceled | 4 | true |



### 3.2 Relationships

```text
[rooms]                          [bookings]                         
Room 1 ──── 0..n RoomAmenity     Booking n ──── 1 BookingStatus     
RoomAmenity n ──── 1 Amenity     Booking.RoomId ┄┄┄> Room.Id        

──── = FK constraint inside one schema          ┄┄┄> = logical reference across modules (no FK; validated via Contracts)
```

### 3.3 Seed Data

- **Rooms.** Seed the `Room` table from the provided **`Room.xls`**. Convert it to `seed/rooms.csv` (or JSON) and load it with an idempotent seeder that runs at startup in Development, or with `HasData`. Sample of the expected rows:

  | RoomNumber | AdultsCapacity | ChildrenCapacity | Price |
  |---|---|---|---|
  | 101 | 1 | 0 | 34 |
  | 102 | 2 | 0 | 48 |
  | 103 | 2 | 1 | 52 |
  | 104 | 2 | 2 | 64 |
  | 201 | 1 | 0 | 34 |
  | … | … | … | … |

- **Booking statuses** and **amenities**. Seed them with `HasData` as listed in 3.1.
- **Users.** One `HotelManager` and one `HotelClerk` (Development only).
- **Room service types** *(extra challenge)*. Seed a few sample types, e.g. *Breakfast in Room*, *Laundry*, *Spa Treatment*, with prices of your choice.
- **Sample bookings** *(recommended)*. Seed bookings relative to "today" that cover every status, at least one arrival today and at least one departure today, so reviewers can try the front-desk endpoints straight away.

---

## 4. Functional Specifications & Business Logic

Each feature below has user stories, the endpoints to expose, business rules and acceptance criteria written in Given/When/Then form. **Acceptance criteria are the grading baseline.** Reviewers will run them against your API.

Unless stated otherwise, every endpoint requires the **Staff** policy (Hotel Manager **or** Hotel Clerk).

---

### 4.1 Rooms — List & View

**User stories**

- *As a hotel clerk or manager, I want to list all rooms in the hotel so that I can see the room inventory.*
- *As a hotel clerk or manager, I want to view a single room with its amenities.*

**Endpoints**

| Method | Route | Policy | Response |
|---|---|---|---|
| `GET` | `/api/rooms` | Staff | `200` list of `{ id, roomNumber, adultsCapacity, childrenCapacity, price }`, ordered by `roomNumber` |
| `GET` | `/api/rooms/{roomId}` | Staff | `200` `{ id, roomNumber, adultsCapacity, childrenCapacity, price, amenities: [{ amenity, label }] }` or `404` |

**Acceptance criteria**

- **AC-R1.** Given an authenticated clerk, when they call `GET /api/rooms`, then every room in the database is returned with room number, adults capacity, children capacity and price.
- **AC-R2.** Given an anonymous caller, when they call any `/api/rooms` endpoint, then the response is `401`.
- **AC-R3.** Given a room id that does not exist, when `GET /api/rooms/{roomId}` is called, then the response is `404` Problem Details.

---

### 4.2 Rooms — Create & Edit

**User stories**

- *As a hotel manager, I want to create a new room so that it can be booked.*
- *As a hotel manager, I want to edit an existing room's information.*
- *As a hotel clerk, I can see room details but I must not be able to create or change rooms.*

**Endpoints**

| Method | Route | Policy | Body | Response |
|---|---|---|---|---|
| `POST` | `/api/rooms` | **Manager** | `{ roomNumber, adultsCapacity, childrenCapacity, price }` | `201 Created` with `Location` header and the room |
| `PUT` | `/api/rooms/{roomId}` | **Manager** | same as above | `200` with updated room, `404` if not found |

**Business rules**

- **BR-R1.** All four fields are required.
- **BR-R2.** `RoomNumber` is unique. A duplicate returns `409`.
- **BR-R3.** `AdultsCapacity ≥ 1`, `ChildrenCapacity ≥ 0`, `Price > 0`.

**Acceptance criteria**

- **AC-R4.** Given a manager, when they POST a valid room, then it is persisted and returned with `201`, and it appears in `GET /api/rooms`.
- **AC-R5.** Given a **clerk**, when they POST or PUT a room, then the response is `403` and nothing changes.
- **AC-R6.** Given a manager, when they POST a room with a missing field or a negative price, then the response is `400` with a validation error for each offending field.
- **AC-R7.** Given a manager, when they PUT valid changes to an existing room, then the changes are persisted.

> **Note.** "Cancel" on the original edit screen is a UI-only navigation action and has no API equivalent.

---

### 4.3 Bookings — List & Search

**User stories**

- *As a hotel clerk or manager, I want to list existing bookings and search them by guest name and/or status.*

**Endpoint**

| Method | Route | Policy |
|---|---|---|
| `GET` | `/api/bookings?guestName={text}&status={status}&sortBy={field}&sortDirection={asc\|desc}&page={n}&pageSize={n}` | Staff |

**Response item:** `{ id, guestLastName, guestFirstName, guestDisplayName, roomNumber, checkInDate, checkOutDate, status }`

**Business rules**

- **BR-B1.** `guestName` is optional. When provided, it matches bookings whose **first name or last name contains** the text, case-insensitively.
- **BR-B2.** `status` is optional. When omitted, bookings in **any status** are returned.
- **BR-B3.** Both filters can be combined (AND).
- **BR-B4.** Results can be sorted by `guestLastName`, `guestFirstName`, `roomNumber`, `checkInDate`, `checkOutDate` or `status`. The default sort is `guestLastName` ascending.
- **BR-B5.** Results are paginated. Default `pageSize` 20, maximum 100. Include `totalCount` in the response.
- **BR-B6.** `roomNumber` is resolved through `IRoomsModuleApi` in a single batch call per page, not one call per row.

> **Sorting by `roomNumber`.** The room number belongs to the Rooms module. Either resolve and sort in memory for the current page set, or keep a `RoomNumber` snapshot on the booking at booking time. If you snapshot, explain the trade-off in your README.

**Acceptance criteria**

- **AC-B1.** Given bookings for "Silva, Gabriel" and "Pereira, Joana", when searching `guestName=silv`, then only the Silva booking is returned.
- **AC-B2.** Given bookings in several statuses, when searching `status=Booked`, then only `Booked` bookings are returned.
- **AC-B3.** Given no filters, then all bookings are returned (paginated).

---

### 4.4 Bookings — Create (with Automatic Room Assignment) & View

Creating a booking is the most important feature of the application. It is a **two-step flow**:

1. **Find an available room.** The clerk/manager enters the guest details, dates and party size and asks for an available room. The system automatically picks the **cheapest** room that fits.
2. **Book the room.** Once a room has been proposed, the clerk/manager either **books** it or goes back without saving anything.

**User stories**

- *As a hotel clerk or manager, I want the system to propose the cheapest available room that fits the guests for the requested dates.*
- *As a hotel clerk or manager, I want to confirm the booking for the proposed room.*
- *As a hotel clerk or manager, I want to view a booking's details and see which actions are currently possible on it.*
- *As a hotel clerk or manager, I want to correct a guest's name on an existing booking.*

**Endpoints**

| Method | Route | Policy | Description |
|---|---|---|---|
| `GET` | `/api/bookings/available-room?checkInDate=&checkOutDate=&numberOfAdults=&numberOfChildren=` | Staff | Returns the cheapest available room `{ roomId, roomNumber, pricePerNight }`, or `404` with a "No room available" problem if none fits. **No data is written.** |
| `POST` | `/api/bookings` | Staff | Body `{ guestLastName, guestFirstName, checkInDate, checkOutDate, numberOfAdults, numberOfChildren?, roomId }`. Creates the booking with status `Booked`. Returns `201`. |
| `GET` | `/api/bookings/{bookingId}` | Staff | Booking details, including `room { id, roomNumber, pricePerNight }` and `availableActions` (see 4.5). |
| `PUT` | `/api/bookings/{bookingId}/guest` | Staff | Body `{ guestLastName, guestFirstName }`. Allowed only while status is `Booked`.³ |

> ³ *Implementation clarification.* Changing dates or party size changes room availability. To change those, cancel the booking and create a new one.

**Validation rules (create & availability search)**

- **BR-B7.** Check-in date and check-out date are **mandatory**.
- **BR-B8.** Check-out must be **after** check-in (`CheckOutDate > CheckInDate`).
- **BR-B9.** There must be **at least one adult** (`NumberOfAdults ≥ 1`).
- **BR-B10.** The number of children **cannot be negative**. It is optional and defaults to 0.
- **BR-B11.** Guest last name and first name are mandatory.

**Room availability rule (core business logic)**

A room is a **candidate** for a request (`adults`, `children`, `checkIn`, `checkOut`) when **all** of the following hold:

- **BR-B12.** `adults > 0`
- **BR-B13.** `Room.AdultsCapacity ≥ adults`
- **BR-B14.** `Room.AdultsCapacity + Room.ChildrenCapacity ≥ adults + children`. Children may use adult places, but adults may not use child places.
- **BR-B15.** No **non-canceled** booking for that room overlaps the requested stay. An existing booking `B` overlaps when:

  ```text
  B.CheckInDate < requested.CheckOutDate   AND   B.CheckOutDate > requested.CheckInDate
  ```

  Stays are treated as **nights**. A guest checking out on the 10th does **not** conflict with a guest checking in on the 10th. Bookings in **any** status except `Canceled` block the room, including `Booked`, `CheckedIn` and `CheckedOut`.

- **BR-B16.** Among all candidates, return the one with the **lowest `Price`**. Break ties by `RoomNumber` ascending so that the result is deterministic.

The original requirement expressed this as a single SQL query joining `Room` and `Booking`. **In a modular monolith that join is not allowed**, because the two tables belong to different modules. The expected approach is:

```csharp
// Bookings.Application — illustrative, not prescriptive
var candidates = await _roomsApi.GetCandidateRoomsAsync(adults, children, ct);   // capacity filter + ORDER BY Price (Rooms module)

var candidateIds = candidates.Select(r => r.Id).ToList();

var blockedRoomIds = await _db.Bookings                                          // overlap filter (Bookings module)
    .Where(b => candidateIds.Contains(b.RoomId)
             && b.Status != BookingStatus.Canceled
             && b.CheckInDate < checkOut
             && b.CheckOutDate > checkIn)
    .Select(b => b.RoomId)
    .Distinct()
    .ToListAsync(ct);

var room = candidates.FirstOrDefault(r => !blockedRoomIds.Contains(r.Id));       // cheapest free room
```

**Booking creation rules**

- **BR-B17.** On `POST /api/bookings`, the server **re-checks** that `roomId` exists (via `IRoomsModuleApi`), that it fits the party (BR-B13/14) and that it is still free for the dates (BR-B15). Another booking may have been made since the room was proposed. If any check fails, return `409 Conflict` and create nothing.
- **BR-B18.** A newly created booking always has status **`Booked`**. Clients cannot set the status on create.

**Acceptance criteria**

- **AC-B4.** Given rooms 101 (1 adult, 0 children, 34), 102 (2/0, 48), 103 (2/1, 52) and 104 (2/2, 64), all free, when searching for 2 adults and 1 child, then room **103** is proposed.
- **AC-B5.** Given the same rooms with 103 already booked (non-canceled) for overlapping dates, when searching for 2 adults and 1 child, then room **104** is proposed.
- **AC-B6.** Given room 103's only overlapping booking is **Canceled**, then room 103 is proposed.
- **AC-B7.** Given an existing booking for room 103 from the 5th to the 10th, when searching from the 10th to the 12th, then room 103 is available (back-to-back stays are allowed).
- **AC-B8.** Given `numberOfAdults = 0`, then the response is `400`.
- **AC-B9.** Given `numberOfChildren = -1`, then the response is `400`.
- **AC-B10.** Given `checkOutDate ≤ checkInDate`, then the response is `400`.
- **AC-B11.** Given no room fits the request, then `GET available-room` returns `404` with a clear Problem Details `title`/`detail`.
- **AC-B12.** Given a proposed room that another user books first, when the first user POSTs the booking, then the response is `409` and no second booking exists.
- **AC-B13.** Given a valid POST, then the booking is stored with status `Booked` and returned with `201` and a `Location` header.

---

### 4.5 Booking Lifecycle — Cancel, Check-in, Check-out

**User stories**

- *As a hotel clerk or manager, I want to cancel a booking that has not started.*
- *As a hotel clerk or manager, I want to check guests in on their check-in date.*
- *As a hotel clerk or manager, I want to check guests out at any time during their stay, because guests may leave early.*

**Endpoints**

| Method | Route | Policy | Success |
|---|---|---|---|
| `POST` | `/api/bookings/{bookingId}/cancel` | Staff | `200` with updated booking |
| `POST` | `/api/bookings/{bookingId}/check-in` | Staff | `200` with updated booking |
| `POST` | `/api/bookings/{bookingId}/check-out` | Staff | `200` with updated booking |

**State machine**

```text
              cancel
   ┌──────────────────────────► Canceled
   │
Booked ──── check-in ────► CheckedIn ──── check-out ────► CheckedOut
         (today == CheckInDate)      (CheckInDate ≤ today ≤ CheckOutDate)
```

**Business rules**

- **BR-L1. Cancel** is allowed only when status is `Booked`.
- **BR-L2. Check-in** is allowed only when status is `Booked` **and** `today == CheckInDate`. Check-in before the check-in date is never possible.
- **BR-L3. Check-out** is allowed only when status is `CheckedIn` **and** `CheckInDate ≤ today ≤ CheckOutDate`. Guests may leave early.
- **BR-L4.** Any other transition is rejected with `409 Conflict` and a Problem Details body that explains why (e.g. *"Check-in is only possible on the check-in date (2026-10-20)."*).
- **BR-L5.** These rules live in the **domain entity** (`booking.Cancel()`, `booking.CheckIn(today)`, `booking.CheckOut(today)`), not in endpoint or handler code.
- **BR-L6.** `today` is the current date in the hotel's configured time zone, obtained from `TimeProvider`.
- **BR-L7. Available actions.** `GET /api/bookings/{id}` returns an `availableActions` array containing exactly the actions that are valid right now. This replaces "only show the relevant buttons" from the original requirements:

  | Status | Condition | `availableActions` |
  |---|---|---|
  | `Booked` | today ≠ CheckInDate | `["Cancel"]` |
  | `Booked` | today = CheckInDate | `["Cancel", "CheckIn"]` |
  | `CheckedIn` | CheckInDate ≤ today ≤ CheckOutDate | `["CheckOut"]` |
  | `CheckedIn` | otherwise | `[]` |
  | `CheckedOut` / `Canceled` | — | `[]` |

  The same domain methods used to perform transitions **must** compute this list (e.g. `booking.CanCheckIn(today)`), so that the two never disagree.

- **BR-L8.** When check-out succeeds, the Bookings module publishes `GuestCheckedOutIntegrationEvent(BookingId, RoomId, CheckedOutAt)` on the in-process event bus. No consumer is required, but the event must be published.

**Acceptance criteria**

- **AC-L1.** Given a `Booked` booking whose check-in date is tomorrow, when check-in is attempted, then `409` is returned and the status is unchanged.
- **AC-L2.** Given a `Booked` booking whose check-in date is today, when check-in is called, then the status becomes `CheckedIn`.
- **AC-L3.** Given a `CheckedIn` booking during its stay, when check-out is called, then the status becomes `CheckedOut`, including when today is earlier than the check-out date.
- **AC-L4.** Given a `Booked` booking, when check-out is attempted, then `409` is returned.
- **AC-L5.** Given a `CheckedIn` booking, when cancel is attempted, then `409` is returned.
- **AC-L6.** Given a `Booked` booking, when cancel is called, then the status becomes `Canceled`, and the room becomes available again for those dates (see AC-B6).
- **AC-L7.** `availableActions` matches the table in BR-L7 for each scenario. Unit-test this with a fake `TimeProvider`.

---

### 4.6 Front Desk — Today's Arrivals & Departures (Homepage)

**User stories**

- *As a hotel clerk or manager, I want to see the guests checking in today and the guests checking out today, and to check them in or out directly from that list.*

**Endpoint**

| Method | Route | Policy |
|---|---|---|
| `GET` | `/api/front-desk/today` | Staff |

**Response**

```json
{
  "date": "2026-10-14",
  "checkingInToday": [
    { "bookingId": "…", "guestDisplayName": "Silva, Francisco", "guestLastName": "Silva", "guestFirstName": "Francisco",
      "roomNumber": "202", "checkOutDate": "2026-10-16" }
  ],
  "checkingOutToday": [
    { "bookingId": "…", "guestDisplayName": "Silva, Miguel", "guestLastName": "Silva", "guestFirstName": "Miguel",
      "roomNumber": "104", "checkInDate": "2026-10-11" }
  ]
}
```

Staff check guests in and out from this view using the **same** endpoints as in 4.5 (`POST /api/bookings/{id}/check-in` and `/check-out`). Do not create duplicate endpoints.

**Business rules**

- **BR-F1.** *Checking in today* contains bookings with `CheckInDate == today` and status `Booked`. Each item shows guest name, room number and **check-out date**.
- **BR-F2.** *Checking out today* contains bookings with `CheckOutDate == today` and status `CheckedIn`. Each item shows guest name, room number and **check-in date**.
- **BR-F3. Standard guest name format.** Everywhere the application shows a guest's full name, it uses one format: **`{LastName}, {FirstName}`** (e.g. `Montgomery, Jane`). The last name is the emphasised part, so the separate `guestLastName` and `guestFirstName` fields are also returned for UIs that want to style it. Implement this **once**, e.g. as a `GuestName` value object with a `DisplayName` property, and reuse it in the bookings list, booking detail and front-desk lists.

> *Implementation clarification on BR-F1/F2.* The original requirement says "bookings where the check-in/check-out date is today". The status filter is added so that each list contains only guests on whom the action (check-in / check-out) can still be performed.

**Acceptance criteria**

- **AC-F1.** Given a `Booked` booking with check-in today, then it appears in `checkingInToday` with its check-out date.
- **AC-F2.** Given a `CheckedIn` booking with check-out today, then it appears in `checkingOutToday` with its check-in date.
- **AC-F3.** Given a booking with check-in today that is `Canceled`, then it does not appear in either list.
- **AC-F4.** Every guest name in every response uses the `LastName, FirstName` format.

---

### 4.7 Room Amenities

**User stories**

- *As a hotel clerk or manager, I want to see the amenities of a room.*
- *As a hotel clerk or manager, I want to add an amenity to a room.*
- *As a hotel clerk or manager, I want to remove an amenity from a room.*

**Endpoints**

| Method | Route | Policy | Description |
|---|---|---|---|
| `GET` | `/api/amenities` | Staff | Lists all amenity types (for dropdowns). |
| `GET` | `/api/rooms/{roomId}/amenities` | Staff | Amenities of the room. These are also included in `GET /api/rooms/{roomId}`. |
| `POST` | `/api/rooms/{roomId}/amenities` | Staff | Body `{ amenity }`. Adds the amenity. `201`. |
| `DELETE` | `/api/rooms/{roomId}/amenities/{amenity}` | Staff | Removes the amenity. `204`, or `404` if the room doesn't have it. |

**Business rules**

- **BR-A1.** The allowed amenities are exactly: Television, Internet Access, Hair Dryer, Premium Towels, Crib and Safe.
- **BR-A2.** A room **must not** have the same amenity twice. This is enforced in the domain (`room.AddAmenity(...)` returns an error) **and** by a unique database index.
- **BR-A3.** Adding amenities is allowed for both the hotel clerk and the hotel manager, unlike editing the room's core data.

**Acceptance criteria**

- **AC-A1.** Given room 101 without Television, when Television is added, then it appears in the room's amenities.
- **AC-A2.** Given room 101 already has Television, when Television is added again, then the response is `409` and only one Television entry exists.
- **AC-A3.** Given an unknown amenity value, then the response is `400`.
- **AC-A4.** Given a clerk, when they add an amenity, then it succeeds (`201`).

---

### 4.8 Occupancy for the Next 7 Days (Homepage)

**User story**

- *As a hotel clerk or manager, I want to see the hotel's occupancy percentage for each of the next 7 days, starting today, so that I can plan staffing and resources.*

**Endpoint**

| Method | Route | Policy |
|---|---|---|
| `GET` | `/api/front-desk/occupancy` | Staff |

**Response:** a list of exactly **7 data points**, one per day, ordered by date. A chart can plot these directly, with days on the X-axis and percentage on the Y-axis.

```json
[
  { "date": "2026-10-14", "occupancyPercentage": 40.0 },
  { "date": "2026-10-15", "occupancyPercentage": 50.0 },
  …
  { "date": "2026-10-20", "occupancyPercentage": 60.0 }
]
```

**Business rules**

- **BR-O1.** The days are `today`, `today + 1`, …, `today + 6`.
- **BR-O2.** A room is **occupied on day `d`** when it has a **non-canceled** booking with `CheckInDate ≤ d < CheckOutDate`, i.e. the night of `d` is booked. This is the same night-based rule as availability (BR-B15).
- **BR-O3.** `occupancyPercentage(d) = occupiedRooms(d) / totalRooms × 100`, rounded to **1 decimal place**. Each room counts at most once per day.
- **BR-O4.** `totalRooms` comes from `IRoomsModuleApi.GetTotalRoomCountAsync()`. If there are no rooms, every percentage is `0`. Never divide by zero.
- **BR-O5.** Days with no bookings are still returned, with `0`. The list always has 7 items.
- **BR-O6.** Compute this with **at most one database query** against the bookings table for the whole 7-day window, plus one Rooms API call. Do not run one query per day.

**Acceptance criteria**

- **AC-O1.** Given 10 rooms and 4 rooms booked for tonight only, then day 1 shows `40.0` and day 2 shows `0.0`.
- **AC-O2.** Given a canceled booking covering tonight, then it does not count.
- **AC-O3.** Given a booking that checks out today, then it does **not** count for today.
- **AC-O4.** The response always contains 7 consecutive dates starting at today.

---

### 4.9 Endpoint Summary

| Module | Method | Route | Policy |
|---|---|---|---|
| Identity | `POST` | `/api/identity/login` | Anonymous |
| Rooms | `GET` | `/api/rooms` | Staff |
| Rooms | `GET` | `/api/rooms/{roomId}` | Staff |
| Rooms | `POST` | `/api/rooms` | Manager |
| Rooms | `PUT` | `/api/rooms/{roomId}` | Manager |
| Rooms | `GET` | `/api/amenities` | Staff |
| Rooms | `GET` | `/api/rooms/{roomId}/amenities` | Staff |
| Rooms | `POST` | `/api/rooms/{roomId}/amenities` | Staff |
| Rooms | `DELETE` | `/api/rooms/{roomId}/amenities/{amenity}` | Staff |
| Bookings | `GET` | `/api/bookings` | Staff |
| Bookings | `GET` | `/api/bookings/{bookingId}` | Staff |
| Bookings | `GET` | `/api/bookings/available-room` | Staff |
| Bookings | `POST` | `/api/bookings` | Staff |
| Bookings | `PUT` | `/api/bookings/{bookingId}/guest` | Staff |
| Bookings | `POST` | `/api/bookings/{bookingId}/cancel` | Staff |
| Bookings | `POST` | `/api/bookings/{bookingId}/check-in` | Staff |
| Bookings | `POST` | `/api/bookings/{bookingId}/check-out` | Staff |
| Bookings | `GET` | `/api/booking-statuses` | Staff |
| Bookings | `GET` | `/api/front-desk/today` | Staff |
| Bookings | `GET` | `/api/front-desk/occupancy` | Staff |

---

## 5. Technical Deliverables

### 5.1 Source Code

- A **Git repository** (link to a private GitHub/Azure DevOps/GitLab repo shared with the reviewers, or a zip that includes the `.git` folder). **Commit history matters.** Small, meaningful commits are expected, not one "initial commit" with everything.
- The solution builds with `dotnet build` with **zero warnings**, using the .NET 10 SDK.
- The solution layout follows Section 2.3.

### 5.2 Database

- EF Core **migrations** for every module `DbContext`, committed to source control.
- The database can be created from scratch by either:
  - running the app in Development, which applies migrations and seeds data automatically, **or**
  - a documented command per module, e.g.

    ```bash
    dotnet ef database update --project src/Modules/Bookings/Hotel.Modules.Bookings.Infrastructure --startup-project src/Host/Hotel.Api --context BookingsDbContext
    ```
- Seed data as described in Section 3.3.


### 5.4 README.md

The README must include:

1. **Prerequisites and how to run.** SDK version, database setup, connection string, and `dotnet run` steps.
2. **Test credentials** for the seeded Manager and Clerk users (Development only).
3. **Architecture overview.** A short description or diagram of the modules and how they communicate.
4. **Key decisions and trade-offs.** E.g. how you resolved room numbers for the booking list, your choice of `Result` type, and mediator vs. plain interfaces.


### 5.5 API Collection

- An `.http` file (Visual Studio / Rider / VS Code REST Client) **or** a Postman collection that walks through the main flows: log in as manager → create room → add amenity → log in as clerk → find available room → book → check in → front desk today → occupancy → check out.

---

