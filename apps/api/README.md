# Concept Project for CampusGo Backend

## Commands used

#### Project Initialization:

- Initial Solution:

```
dotnet new sln -n CampusGo
```

- WebApi Project:

```
dotnet new mvc -n CampusCarpool.Web
```

- At project root folder, add WebApi solution to main solution:

```
dotnet sln add CampusCarpool.Web/CampusCarpool.Web.csproj
```

- Then:

```
dotnet build
```

#### SwaggerUI or Scalar UI related:

- To visit the API reference pages (development):

```
localhost:${PORT}/swagger
```

- or

```
localhost:${PORT}/scalar/v1
```

- If either is unavailable, install packages:

```
dotnet add package Swashbuckle.AspNetCore
```

- or

```
dotnet add package Scalar.AspNetCore
```

- Then add inside an if statement within the Web Program.cs:

```
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Use either swagger or scalar
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "CampusGo.v1"));
    app.MapScalarApiReference();
}
```

#### Docker related:

- Create docker-compose.yml file and run it with -d tag to detach it and to keep it running without being bound to the terminal:

```
docker compose up -d
```

#### Core related:

- NPGSQL for EF Core Integration with PostgreSQL:

```
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

- NetTopologySuite for precise locations, switch to latest version if available

```
dotnet add package NetTopologySuite --version 2.6.0
```

- Toolings for migrations to work:

```
dotnet add package Microsoft.EntityFrameworkCore.Design
```

- If EF CLI Tool has not been previously installed:

```
dotnet tool install --global dotnet-ef
```

- then check with:

```
dotnet ef
```

- For first-time migration

```
dotnet ef migrations add InitialCreate
```

- then

```
dotnet ef database update
```

#### JWT Integration

- JWT installation

```
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

- How does JWT Integration within ASP.NET Core work? Every controller inheriting from ControllerBase automatically gets a requested property of type ClaimsPrincipal — this is ASP.NET's built-in representation of whoever is making the current request, populated automatically by the authentication middleware (UseAuthentication()) before your action method ever runs. This is initialized by this code within the Program.cs

- Here's the flow: when a request comes in with a JWT in its Authorization: Bearer <token> header, the JWT middleware configured by Helpers/ClaimsPrincipalExtension.cs does the following:

1. Verifies the token's signature against the Jwt:Key (confirming it wasn't tampered with)
2. Checks it hasn't expired
3. Reads the claims baked into it — remember AuthController.GenerateToken embedded ClaimTypes.NameIdentifier, ClaimTypes.Email, and ClaimTypes.Role
4. Populates ControllerBase.User with a ClaimsPrincipal object containing those claims

#### Miscellaneous

- User-secrets initialization:

```
dotnet user-secrets init
```

- Place credentials into user-secrets:

```
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=campusgo_dev;Username=campusgo;Password=campusgo_dev_pw"
```
