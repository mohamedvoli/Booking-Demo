# Hotel Booking API — Project Roadmap

Oct 14, 2026 · @RadwaAli

## How to use this roadmap

Start with Phase 0 and finish each phase completely before moving to the next. Every phase ends with something you can run and test in the browser, so you always know whether you are on track.

The order is deliberate. You build one small feature end to end first (Amenities), learn the full pattern on it, then repeat that pattern for bigger features. Security comes after you have working endpoints, and Bookings comes last because it depends on Rooms.

Follow the same rhythm for every step:

1. Write a small piece of code.
2. Build the solution (`dotnet build`) and fix errors immediately.
3. Run the API and test the new endpoint in Scalar.
4. Commit to Git with a clear message, such as `Rooms: add create amenity endpoint`.

Each phase ends with a **Done when** checklist. Tick the boxes as you go; if one fails, fix it before moving on. Rough time estimates assume a few focused hours per phase, but go at your own pace.

## Phase 0: Set up your environment

Goal: every tool installed and verified before you write any code. Estimated time: 1 hour.

1. Install the **.NET 10 SDK** and confirm with `dotnet --version` (it should start with `10.`).
2. Install **SQL Server** (Developer or Express edition, or LocalDB that comes with Visual Studio) and **SQL Server Management Studio** or Azure Data Studio to look at your tables.
3. Install the EF Core CLI tool: `dotnet tool install --global dotnet-ef`, then confirm with `dotnet ef --version`.
4. Choose your IDE: Visual Studio 2026, JetBrains Rider, or VS Code with the C# Dev Kit extension.
5. Create an empty folder `HotelBooking`, run `git init`, and add a .NET `.gitignore` with `dotnet new gitignore`.
6. Create a GitHub (or Azure DevOps) repository so your frontend teammate can see your work later.

**Done when**

- [ ] `dotnet --version` shows 10.x
- [ ] You can connect to SQL Server from SSMS or Azure Data Studio
- [ ] `dotnet ef --version` works
- [ ] The repository exists with a `.gitignore` and a first commit

## Phase 1: Create the solution skeleton

Goal: all 17 projects exist with the correct references, and the empty solution builds. Estimated time: 1–2 hours. You write no business code in this phase, only structure.

1. Create the solution and the Host: `dotnet new sln -n HotelBooking`, then `dotnet new webapi -n HotelBooking.Api -o src/Host/HotelBooking.Api`.
2. Create `HotelBooking.Shared` under `src/Shared/` as a class library.
3. Create the 15 module projects (Identity, Rooms, Bookings × Domain, Application, Infrastructure, Presentation, Contracts) under `src/Modules/<Module>/`.
4. Add every project to the solution.
5. Delete the auto-generated `Class1.cs` files and the sample weather endpoint in `Program.cs`.
6. Add project references, one module at a time, following the rules:
   - Domain → Shared
   - Application → its own Domain and Contracts
   - Infrastructure → its own Application
   - Presentation → its own Application and Shared
   - Host → every module's Infrastructure and Presentation
7. Add the cross-module references: Bookings.Application → Rooms.Contracts, and Rooms.Application → Bookings.Contracts.
8. Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the three Presentation projects.
9. Build the solution and commit.

Tip: before adding a reference, ask yourself "does this arrow point toward Shared or Contracts?" If it points from one module's inner layer to another module's inner layer, it's wrong.

**Done when**

- [ ] The solution contains 17 projects in the folder layout from our design
- [ ] `dotnet build` succeeds with no errors
- [ ] No module references another module's Domain, Application or Infrastructure
- [ ] Committed as "Create solution skeleton"

## Phase 2: Build Shared and the Host basics

Goal: the API runs, shows an empty Scalar documentation page, and returns clean JSON errors. Estimated time: 2 hours. Authentication and CORS wait until later phases.

1. In Shared, add `Domain/Entity.cs`, the three exceptions (`DomainException`, `NotFoundException`, `ConflictException`) and `Authorization/Roles.cs`.
2. In the Host, add the packages `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` and `Microsoft.EntityFrameworkCore.Design`.
3. Create `ErrorHandling/GlobalExceptionHandler.cs` and register it with `AddProblemDetails()` and `AddExceptionHandler<GlobalExceptionHandler>()`.
4. Create `Extensions/OpenApiExtensions.cs` with `AddApiDocumentation()`. Skip the Bearer transformer for now; you add it in Phase 5.
5. Write `Program.cs` with only: OpenAPI, the exception handler, `TimeProvider.System`, `MapOpenApi()` and `MapScalarApiReference()`.
6. Put the connection string in `appsettings.json` (`ConnectionStrings:Default`, database `HotelBookingDb`).
7. Set `launchUrl` to `scalar` in `launchSettings.json`.
8. Add a temporary test endpoint that throws a `NotFoundException`, check that you get a 404 JSON response, then delete it.

**Done when**

- [ ] Pressing Run opens the Scalar page in the browser
- [ ] `/openapi/v1.json` returns a JSON document
- [ ] A thrown `NotFoundException` becomes a 404 ProblemDetails response
- [ ] Committed as "Add Shared kernel and Host setup"

## Phase 3: Rooms module, part 1 — Amenities end to end

Goal: your first complete vertical slice, from HTTP request to a database row and back. Estimated time: 3–4 hours. This is the most important phase, because every later feature repeats this exact pattern. Endpoints stay unprotected for now.

Work through the layers from the inside out:

1. **Domain:** create `Amenity` (Name, Description) inheriting `Entity`, with a static `Create(...)` method and an `Update(...)` method that validate input and throw `DomainException`.
2. **Application:** create `AmenityRequest`, `AmenityResponse`, the `IAmenityRepository` interface and `AmenityService` with `GetAllAsync`, `CreateAsync` and `UpdateAsync`. Add a `DependencyInjection.cs` with `AddRoomsApplication()`.
3. **Infrastructure:** add `Microsoft.EntityFrameworkCore.SqlServer`. Create `RoomsDbContext` (schema `dbo`), `AmenityConfiguration` (max lengths, unique name index) and `AmenityRepository`. Create `RoomsModule.cs` with `AddRoomsModule()` using the history table `__EFMigrationsHistory_Rooms`.
4. **Presentation:** create `AmenityEndpoints.cs` with `MapAmenityEndpoints()` for GET, POST and PUT on `/api/amenities`.
5. **Host:** call `AddRoomsModule()` and `MapAmenityEndpoints()` in `Program.cs`.
6. **Database:** run `dotnet ef migrations add InitialRooms --context RoomsDbContext` (with `--project` and `--startup-project`), then `dotnet ef database update`.
7. Test in Scalar: create an amenity, list amenities, update one, and try an invalid one to see a 400 error.

If you get stuck, it is usually one of three things: a missing project reference, a service not registered in `AddRoomsModule()`, or a migration command run against the wrong project.

**Done when**

- [ ] `HotelBookingDb` exists in SQL Server with an `Amenities` table and `__EFMigrationsHistory_Rooms`
- [ ] You can create, list and update amenities from Scalar
- [ ] A duplicate or empty name returns a clear error, not a 500
- [ ] You can explain in your own words what each of the four layers did for this feature
- [ ] Committed as "Rooms: amenities CRUD"

## Phase 4: Rooms module, part 2 — Rooms and their amenities

Goal: the manager's full feature set works, still without login. Estimated time: 3–4 hours. You repeat the Phase 3 pattern on a richer entity.

1. **Domain:** add the `RoomType` enum (Single, Double, Suite) and the `Room` entity: Number, Type, Capacity, PricePerNight, IsActive, and a collection of amenities. Add methods `Create`, `Update`, `Activate`, `Deactivate` and `SetAmenities`.
2. **Infrastructure:** configure the many-to-many relationship Room ↔ Amenity (EF creates a `RoomAmenities` join table), a unique index on room number, and `decimal(10,2)` for the price. Add `RoomRepository`.
3. **Application:** create `RoomService` and the request/response DTOs. The response includes the room's amenity names, which is what the frontend will show.
4. **Presentation:** create `RoomEndpoints.cs` with GET list, GET by id, POST, PUT, POST `/activate`, POST `/deactivate` and PUT `/amenities`.
5. **Database:** add a migration `AddRooms` and update the database.
6. Test in Scalar: create three rooms of different types, attach amenities, deactivate one, and try a duplicate room number (expect 409).

For now, `Deactivate` just changes the flag. In Phase 7 you add the check for upcoming bookings, because the Bookings module does not exist yet.

**Done when**

- [ ] `Rooms` and `RoomAmenities` tables exist
- [ ] All room endpoints work from Scalar
- [ ] A room's response lists its amenities
- [ ] Duplicate room number returns 409, negative price returns 400
- [ ] Committed as "Rooms: rooms CRUD and amenity assignment"

## Phase 5: Identity module and securing the API

Goal: staff log in, receive a JWT, and the Rooms endpoints are protected by role. Estimated time: 3–4 hours.

1. **Domain:** create `User` with Username, PasswordHash and Role.
2. **Application:** create `LoginRequest`, `LoginResponse` (token, role, expiry), `IUserRepository`, `ITokenGenerator` and `AuthService`. The service finds the user, verifies the password and asks the generator for a token.
3. **Infrastructure:** add `Microsoft.IdentityModel.JsonWebTokens` and `Microsoft.Extensions.Identity.Core`. Create `AuthDbContext` (history table `__EFMigrationsHistory_Identity`), `UserRepository`, `JwtTokenGenerator` (writes `role` and `name` claims) and `UserSeeder`, which creates one Manager and one Clerk using `PasswordHasher<User>`.
4. **Presentation:** create `AuthEndpoints.cs` with `POST /api/auth/login`, open to anonymous users.
5. **Host:** add the `Jwt` section to `appsettings.json`, store `Jwt:Key` with `dotnet user-secrets`, add `AuthenticationExtensions.cs`, call `UseAuthentication()` and `UseAuthorization()`, and run `SeedUsersAsync()` in Development.
6. Add the Bearer transformer to `OpenApiExtensions.cs` so Scalar shows a token field.
7. Migrate the Identity module's database.
8. Protect the Rooms endpoints: write operations require `Roles.Manager`; GET rooms and GET amenities allow both roles.
9. Test: call a Rooms endpoint without a token (401), log in as clerk and try to create a room (403), log in as manager and create one (201).

**Done when**

- [ ] A `Users` table exists with two seeded accounts and hashed passwords
- [ ] Login returns a token; a wrong password returns 401 with no hint which part was wrong
- [ ] 401, 403 and 201 behave exactly as in step 9
- [ ] `Jwt:Key` is not in any committed file
- [ ] Committed as "Identity: login, JWT and role protection"

## Phase 6: Bookings module, part 1 — Guests, availability and creating bookings

Goal: a clerk can search free rooms and book one for a guest. This is your first cross-module call. Estimated time: 4–5 hours.

1. **Rooms.Contracts:** create `RoomInfoDto` and `IRoomsModuleApi` with `GetRoomAsync` and `GetActiveRoomsAsync`.
2. **Rooms.Application:** implement it as `internal class RoomsModuleApi` and register it in `AddRoomsApplication()`. Test nothing yet; it is just a class.
3. **Bookings.Domain:** create `Guest` (FullName, Phone, Email), the `BookingStatus` enum and the `Booking` entity with `Create(...)` only. Leave check-in, check-out and cancel for Phase 7.
4. **Bookings.Infrastructure:** create `BookingsDbContext` (history table `__EFMigrationsHistory_Bookings`), configurations (RoomId as a plain column with no foreign key, a real foreign key to Guest, a unique index on guest phone), and the repositories. The `HasOverlapAsync` query only considers Confirmed and CheckedIn bookings.
5. **Bookings.Application:** create `GuestService` (search by name or phone), `AvailabilityService` (gets active rooms from `IRoomsModuleApi`, removes overlapping ones, filters by capacity) and `BookingService.CreateAsync` as designed.
6. **Presentation:** create `GET /api/bookings/available-rooms`, `POST /api/bookings`, `GET /api/bookings/{id}` and `GET /api/guests`, all requiring `Roles.Clerk`.
7. Register `AddBookingsModule()` and the endpoints in the Host, then migrate.
8. Test as clerk: search availability, book a room, search again (the room disappears for those dates), try the same dates again (409), book for a returning guest (no duplicate guest row).

**Done when**

- [ ] `Guests` and `Bookings` tables exist, and `Bookings` has no foreign key to `Rooms`
- [ ] Bookings.Application references Rooms.Contracts only, never Rooms.Application
- [ ] Overlapping bookings are rejected with 409; back-to-back bookings (checkout day = next check-in day) are allowed
- [ ] TotalPrice is nights × price at booking time
- [ ] Committed as "Bookings: availability and create booking"

## Phase 7: Bookings module, part 2 — Lifecycle and the reverse call

Goal: the full booking lifecycle works, and Rooms asks Bookings before deactivating a room. Estimated time: 3–4 hours.

1. **Domain:** add `CheckIn(today)`, `CheckOut()` and `Cancel(reason)` to `Booking`, each throwing `DomainException` for an invalid status change.
2. **Application:** add `CheckInAsync`, `CheckOutAsync` and `CancelAsync` to `BookingService`. Get today's date from the injected `TimeProvider`, never `DateTime.Now`.
3. Add a list query: `GET /api/bookings?status=&date=` returning bookings with guest name and room number. The room number comes from `IRoomsModuleApi`, because Bookings only stores the RoomId.
4. **Presentation:** add the three action endpoints (`/check-in`, `/check-out`, `/cancel`) and the list endpoint.
5. **Bookings.Contracts:** create `IBookingsModuleApi.HasUpcomingBookingsAsync`, implement it in Bookings.Application and register it.
6. **Rooms.Application:** inject `IBookingsModuleApi` into `RoomService.DeactivateAsync` and throw `ConflictException` when the room has upcoming bookings.
7. Add a migration only if you changed the Booking table (for example `CancellationReason`).
8. Walk through the whole story as the clerk: book → check in → check out. Then book → cancel. Then try every wrong order (check out before check in, cancel after check in) and expect 400.
9. As manager, try to deactivate a room with a future booking (409), cancel the booking as clerk, and deactivate again (success).

**Done when**

- [ ] Every valid status change works and every invalid one returns 400 with a clear message
- [ ] Cancelled bookings free the room in availability search
- [ ] Deactivating a booked room returns 409
- [ ] Rooms references Bookings.Contracts only, and the solution still builds with no circular reference
- [ ] Committed as "Bookings: lifecycle; Rooms: deactivate guard"

## Phase 8: Polish and hand off to the frontend

Goal: your teammate can start integrating without asking you questions. Estimated time: 2–3 hours.

1. Add `CorsExtensions.cs` with the frontend's URL in `Cors:AllowedOrigins`, and call `UseCors` before `UseAuthentication`. Ask your teammate which port they use.
2. Give every endpoint a summary with `.WithSummary("...")` and declare its responses with `.Produces<T>()` and `.ProducesProblem(...)`, so the OpenAPI document is complete.
3. Write `HotelBooking.Api.http` with the full happy path: login as manager, create a room, login as clerk, search, book, check in, check out.
4. Write a `README.md` with: how to run the API, the two seeded accounts, the Scalar URL, the `/openapi/v1.json` URL, the role rules, and the error format (ProblemDetails).
5. Run everything from a clean state: drop the database, run the three `database update` commands, start the API, and repeat the `.http` file.
6. Push to the shared repository and send your teammate the README link.

**Done when**

- [ ] A call from the frontend's origin is not blocked by CORS
- [ ] Every endpoint in Scalar has a summary and its possible status codes
- [ ] The project works from a fresh clone by following only the README
- [ ] Your teammate has the repository, the README and the two test accounts

## Common pitfalls and what comes next

Most beginner problems in this project come from a short list of mistakes. Check this list first when something breaks:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| "Unable to resolve service for type ..." at startup | A service or repository isn't registered | Add it in the module's `AddXxxModule()` or `AddXxxApplication()` |
| `dotnet ef` says no DbContext found or picks the wrong one | Missing `--context`, `--project` or `--startup-project` | Always pass all three |
| Migration creates tables from another module | DbContext includes another module's entity | Each DbContext lists only its own entities |
| Every request returns 401 even with a token | Issuer, audience or key differs between generator and validator | Both read the same `Jwt` section |
| 403 for the right user | Role claim name mismatch | Generator writes `role`; validation uses `RoleClaimType = "role"` |
| Frontend sees a CORS error | `UseCors` is after authentication, or wrong origin | Move `UseCors` up; check `Cors:AllowedOrigins` |
| A 500 instead of 400/404/409 | Plain `Exception` thrown | Throw one of the three Shared exceptions |

Once the demo works, these are good next topics, roughly in this order: input validation with FluentValidation or .NET 10's built-in minimal API validation, pagination for list endpoints, optimistic concurrency to prevent two clerks double-booking at the same moment, and finally unit and integration testing when you feel ready.
