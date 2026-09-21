# TechStore

A small ASP.NET Core MVC (.NET 8) student portfolio project for browsing and managing a technology catalog. It uses the existing Bootstrap and jQuery assets, with no added NuGet packages.

## Features

- Responsive home page, navigation, product cards, and shared form styles.
- Product create, read, update, and delete with validation and delete confirmation.
- Search by name, filter by the existing category/type field, product counts, and empty states.
- Consistent price formatting (two decimal places; no currency is assumed).
- Demo signup and login, friendly errors, and antiforgery protection on MVC POST actions.
- Accessible labels, keyboard focus, skip navigation, and a local product illustration when no image exists.

## Run locally

Install a .NET SDK supporting .NET 8 and the ASP.NET Core 8 runtime. From this directory:

```powershell
dotnet restore techstore.sln
dotnet build techstore.sln
dotnet run --project techstore --launch-profile https
```

The HTTPS profile uses https://localhost:7218. For local HTTP, use the `http` profile at http://localhost:5077. If needed, trust your development certificate with `dotnet dev-certs https --trust`.

The main catalog and custom demo account pages do not need a database. The catalog starts empty and is **in memory**: restarting the process clears products. Use **Add product** to begin.

Demo users are stored in `techstore/App_Data/users.txt`, outside the public web root and ignored by Git. To keep an existing user file, set `Store__UsersFile` to its absolute path before running; legacy account records are automatically upgraded to hashed records on authentication access (see below). No old user file is moved or deleted automatically.

## Structure

- `techstore/Controllers/StoreController.cs`: catalog CRUD, search, cookie login/logout, and protected management actions.
- `techstore/Services/UserFileStore.cs`: password hashing/verification, atomic file writes, and legacy conversion.
- `techstore/Models/Product.cs`: validated in-memory product model, renamed from `data`.
- `techstore/Models/Login.cs` and `Users.cs`: demo account input models.
- `techstore/Views/Store/`: store pages and reusable product form/illustration partials.
- `techstore/wwwroot/css/site.css`: shared styling.
- `techstore/Data/`: pre-existing Identity context and migrations, unchanged.

Existing URLs such as `/Store/StartP`, `/Store/Viewdata`, `/Store/one/{id}`, `/Store/additem`, `/Store/edit/{id}`, and `/Store/delete/{id}` remain compatible. Original view names remain aligned with those actions. Product IDs are assigned monotonically within a running process; catalog access is synchronized for concurrent requests.

## Authentication

Store signup uses ASP.NET Core `PasswordHasher<Users>` (salted, versioned PBKDF2 hashes). Login uses `VerifyHashedPassword` and saves an upgraded hash when the framework requests rehashing. No custom cryptography or new packages were added.

A dedicated `StoreCookie` scheme creates a protected authentication ticket with user ID, name, and email only. The cookie is HttpOnly, SameSite=Lax, session-only, with a two-hour sliding ticket lifetime. Production requires HTTPS (Secure cookies); local Development also supports HTTP. Session-only cookies are not a "remember me" feature. Do not delete ASP.NET Data Protection keys if existing sessions must remain valid.

Product creation, editing, and deletion require store authentication on both GET and POST. Browsing and product details remain public. All registered store users can manage products; no admin/ownership model has been introduced. Logout is POST-only and requires antiforgery validation. Login return URLs must be local.

Signup requires 12–128 characters. Legacy passwords remain usable, even if shorter. Password input is cleared before redisplaying invalid forms, and passwords/hashes are never included in claims, TempData, or application logs.

The pre-existing Identity/SQL and optional Google systems remain separate. They do not grant access to protected store actions, which explicitly require `StoreCookie`. No Identity database migration is required, and existing database tables were not changed.

### Existing plaintext files

The next signup/login access to a configured legacy user file converts all valid four-column records (`id;name;email;plaintext`) into five-column records (`id;name;email;hash;passwordhasher-v1`). Existing IDs, names, emails, and working passwords are preserved. A marker avoids guessing whether an old plaintext password happens to resemble a hash.

Conversion parses every record before writing. A malformed/unsupported record stops the operation with a friendly error and leaves the original file untouched. Repair that record locally without pasting credentials into logs or issue reports. Writes use a temporary file in the same directory and atomic replacement; the application creates no plaintext backup. Rehashing and signup also use this write path.

Files not configured for this application are not converted. Older archives/backups can still contain plaintext: restrict access and remove obsolete copies securely yourself. Conversion is irreversible to plaintext; it does not reset passwords. The file must be outside the public web root, writable by the application, and restricted to the app's OS account.

### Remaining scope

This remains a single-process portfolio app with file-backed accounts and an in-memory catalog. Rate limiting, password recovery, account revocation, roles, and multi-instance file coordination are not implemented. Logout removes the browser cookie; there is no server-side ticket revocation list. The unrelated Identity/Google workflows require their own setup and were not tested.

The original settings contained a Google client secret. Revoke/rotate that old secret before use; removing it from source does not revoke it. Do not publish original archives or generated files that may contain it.

## Optional private configuration

No connection strings or OAuth credentials are checked into the application settings. Configure values through environment variables or .NET User Secrets using the existing project UserSecretsId.

Supported keys:

| Configuration key | Environment variable |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Authentication:Google:ClientId` | `Authentication__Google__ClientId` |
| `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |
| `Store:UsersFile` | `Store__UsersFile` |

Configure both Google values to enable the existing provider. Set your own SQL Server connection before using the separate Identity pages. Missing database configuration does not prevent browsing the MVC catalog, but database-backed Identity operations require it.

## Verification

Incremental builds passed, with the final build reporting **0 warnings and 0 errors**. HTTP smoke checks passed for page rendering, missing products, antiforgery rejection, valid/invalid create and edit, details, search/filtering, deletion, ID reuse prevention, first signup, duplicate email, and valid/invalid login.

The workspace also includes `scripts/smoke-test.ps1`. Authentication checks cover signup, valid/invalid login, cookies across requests, local/external return URLs, anonymous GET/POST access, logout, CSRF protection, persisted hash format, legacy conversion, and safe handling of malformed records. The catalog CRUD regression checks also pass.

Run only against a dedicated local Development instance with an empty catalog and a temporary `Store__UsersFile`. The script creates/deletes products and leaves generated accounts in that temporary file. Supplying `-UsersFile` also writes legacy/malformed test records there, so **never point it at a real user file**.

From the workspace root, in one terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Store__UsersFile = Join-Path $PWD "techstore/techstore/techstore/techstore/techstore/App_Data/auth-test-users.txt"
dotnet run --project techstore/techstore/techstore/techstore/techstore --no-launch-profile --urls http://127.0.0.1:5089
```

In another terminal:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke-test.ps1 -UsersFile "techstore/techstore/techstore/techstore/techstore/App_Data/auth-test-users.txt"
```
Responsive styles were reviewed in code; a visual browser/device review is still recommended.

## Repository hygiene

The supplied folders had no Git repository. Ignore rules cover `.vs/`, `bin/`, `obj/`, user-specific files, local account data, and original archives. No commits, pushes, index changes, branch changes, or history changes were made.


