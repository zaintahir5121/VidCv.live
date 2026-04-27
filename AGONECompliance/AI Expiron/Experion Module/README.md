# Experion Module (Standalone)

This folder contains a standalone, reusable Experion module split into:

- `Frontend/` - framework-agnostic `experion-sdk.js` with circle trigger, contextual popup, suggestions, old/new chat flow.
- `Backend/` - ASP.NET Core API with DOM cleaning, context routing (`LLM` vs `Action`), action knowledgebase matching, learning cache, and Facebook action executor.
- `TestPage/` - one-page ASP.NET Core app to validate the module independently.

## Backend endpoints

Base URL: `/api/experion-module`

- `POST /session/bootstrap`
- `POST /context/resolve`
- `POST /conversation/message`
- `GET /conversation/history`
- `GET /history` (SDK compatibility alias)
- `POST /actions/facebook-post`

All endpoints require logged-in user context (`X-User-Id` or request `userId`).

## Run locally

Backend:

- `cd "AI Expiron/Experion Module/Backend"`
- `dotnet build "Experion.Module.sln"`
- `cd src/Experion.Backend`
- `dotnet run --urls http://127.0.0.1:5188`

Test page:

- `cd "AI Expiron/Experion Module/TestPage/Experion.Module.TestPage"`
- `dotnet build`
- `dotnet run --urls http://127.0.0.1:5190`

Then open `http://127.0.0.1:5190`.

## Configuration

Set `ExperionModule` settings in:

- `Backend/src/Experion.Backend/appsettings.json`
- `Backend/src/Experion.Backend/appsettings.Development.json`

Important:

- `OpenAi.Enabled`, `OpenAi.Endpoint`, `OpenAi.ApiKey`, `OpenAi.DeploymentName`
- `Facebook.PageId`, `Facebook.PageAccessToken`, `Facebook.GraphApiBaseUrl`
- `Cache.AzureSearchEndpoint`, `Cache.AzureSearchApiKey`, `Cache.AzureSearchIndexName`
