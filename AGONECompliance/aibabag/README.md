# aibabag

`aibabag` is a production-style one-page ASP.NET Core MVC (`.NET 8`) application:

- professional interactive landing + dashboard UI
- Google OAuth login first
- automatic camera open after login
- birthday autofill from Gmail profile (Google People API) when available
- free AI model integration (`text.pollinations.ai`)
- photo-context + DOB + email/name based insight generation
- social sharing/export + optional direct Facebook/LinkedIn posting
- SQLite persistence via EF Core

## Core user flow

1. Open `/`
2. Click **Continue with Google**
3. App logs in and attempts to pull birthday
4. Camera opens automatically
5. Capture photo, set DOB, click **Generate Dashboard**
6. Export/share/post insights

## Free AI API used (forever-free path)

The app uses `https://text.pollinations.ai` (free endpoint) for generated insight text.

Configure in `appsettings.json`:

- `Ai.Provider`: `Pollinations`
- `Ai.Enabled`: `true`
- `Ai.BaseUrl`: `https://text.pollinations.ai`
- `Ai.TimeoutSeconds`: `20`

If AI API is unavailable, deterministic fallback text is returned automatically.

## Google OAuth + Gmail birthday integration guide

1. Open Google Cloud Console and select/create project.
2. Configure OAuth consent screen:
   - App name, support email
   - Add scopes:
     - `openid`
     - `email`
     - `profile`
     - `https://www.googleapis.com/auth/user.birthday.read`
3. Create OAuth 2.0 Client ID (Web application).
4. Authorized redirect URIs:
   - `https://localhost:5001/signin-google`
   - `http://localhost:5000/signin-google`
   - `https://your-domain/signin-google` (IIS production)
5. Put values in `appsettings.json`:
   - `Google:ClientId`
   - `Google:ClientSecret`
6. Restart app and login.

Birthday is read via Google People API in backend (`/api/auth/google-birthday`), with safe fallback to manual DOB input if unavailable.

## Social posting configuration

Direct posting uses optional backend credentials:

- Facebook:
  - `Social:FacebookAccessToken`
  - `Social:FacebookPageId` (leave empty to post to `me/feed`)
- LinkedIn:
  - `Social:LinkedInAccessToken`
  - `Social:LinkedInPersonUrn` (format `urn:li:person:xxxx`)

Without these values, social buttons still support browser share/copy and download export.

## Local run

1. Install .NET 8 SDK.
2. Configure `appsettings.json` for Google/OAuth and optional Social credentials.
3. Run:
   - `dotnet restore`
   - `dotnet ef migrations add InitialCreate` (first time)
   - `dotnet ef database update`
   - `dotnet run`

## IIS deployment instructions (Windows VM)

1. Install prerequisites on VM:
   - .NET 8 Hosting Bundle
   - IIS + ASP.NET Core Module
2. Publish:
   - `dotnet publish -c Release -o ./publish`
3. Create IIS site:
   - Physical path: published folder
   - App pool: `No Managed Code`
4. Set folder permissions:
   - App pool identity needs read/write for SQLite DB folder.
5. Configure HTTPS binding and certificate.
6. Update Google OAuth redirect URI to production HTTPS URL.
7. Restart IIS site and validate:
   - `/`
   - `/api/auth/status`
   - `/api/auth/google-birthday`

## Notes for production readiness

- Keep OAuth and Social secrets out of source control; use environment variables or secure config provider.
- Use HTTPS only.
- Add DB migrations before final deployment (do not rely only on `EnsureCreated` for long-term production schema evolution).

