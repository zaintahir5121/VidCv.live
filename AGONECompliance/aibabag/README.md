# AiBabag

AiBabag is a one-page ASP.NET Core MVC (.NET 8) app with:

- Google login (OAuth)
- Camera capture in browser
- Birth-date based astrology profile
- Detailed dashboard (movies, food, animals, friends, family, office, marriage, future, past, finance, health, travel, spirituality and more)
- SQLite storage via Entity Framework Core

## Run

1. Install .NET 8 SDK
2. Configure Google credentials in `appsettings.json`
3. Run:

   - `dotnet restore`
   - `dotnet ef migrations add InitialCreate`
   - `dotnet ef database update`
   - `dotnet run`

## Important

- Update Google OAuth redirect URL to:
  `https://localhost:5001/api/auth/google-response`
  or your deployment URL:
  `https://aibabag.com/api/auth/google-response`

