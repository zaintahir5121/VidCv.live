# IIS Deployment - aibabag.com

1. Install .NET 8 Hosting Bundle on IIS server.
2. Publish app:
   - `dotnet publish -c Release -o C:\inetpub\wwwroot\aibabag`
3. Create IIS app pool:
   - Name: `aibabag`
   - .NET CLR: `No Managed Code`
4. Create IIS website:
   - Path: `C:\inetpub\wwwroot\aibabag`
   - Host: `aibabag.com`
5. Add HTTPS binding and SSL certificate.
6. Set Google OAuth redirect URI:
   - `https://aibabag.com/api/auth/google-response`
7. Ensure write permission on app folder for app pool identity.
