# aibabag

`aibabag` is a one-page ASP.NET Core MVC (.NET 8) app with:

- Google OAuth login
- Browser camera capture for profile photo
- Birth-date based zodiac/chinese-zodiac profile
- AI-generated astrology narratives for personality, career, love, family, money, travel, health, and more
- SQLite storage via EF Core

## Forever-free AI API used

This project uses `https://text.pollinations.ai` for AI text generation (free endpoint).

- No paid SDK required
- Server-side HTTP call from .NET service
- Fallback logic returns deterministic local text if API is unavailable

Configure in `appsettings.json`:

- `Ai.Provider`: `Pollinations`
- `Ai.Enabled`: `true`
- `Ai.BaseUrl`: `https://text.pollinations.ai`
- `Ai.TimeoutSeconds`: `20`

## Local run

1. Install .NET 8 SDK.
2. Set Google OAuth credentials in `appsettings.json`:
   - `Google:ClientId`
   - `Google:ClientSecret`
3. Run commands:
   - `dotnet restore`
   - `dotnet ef migrations add InitialCreate` (first time only)
   - `dotnet ef database update`
   - `dotnet run`

## Google OAuth setup guide

1. Open Google Cloud Console and create/select a project.
2. Enable OAuth consent screen:
   - Add app name, support email, and authorized domain.
3. Create OAuth 2.0 Client ID of type **Web application**.
4. Add Authorized redirect URIs:
   - `https://localhost:5001/signin-google`
   - `http://localhost:5000/signin-google`
   - `https://your-domain/signin-google` (production)
5. Copy generated client ID/secret into `appsettings.json`.
6. Restart app and test login via `/api/auth/login`.

## Deployment note (IIS)

When publishing to IIS behind HTTPS, ensure:

- External URL is HTTPS.
- Google OAuth redirect URI exactly matches production URL `https://your-domain/signin-google`.
- Writable permissions for app folder (SQLite DB file creation).

