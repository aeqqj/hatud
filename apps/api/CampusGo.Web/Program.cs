using CampusGo.Web.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using CampusGo.Web.Services;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// CORS policy name
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

// Add services to the container.

builder.Services.AddControllers();

// Add the CORS services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("https://scalar.com", "https://jsdelivr.net")
                  .AllowAnyHeader()
                  .AllowAnyMethod();

            // TIP: If you still run into issues during sandbox testing, 
            // you can temporarily swap the above lines for:
            // policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization"
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null)] = []
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Supabase"),
        o => o.UseNetTopologySuite()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Make every endpoint within every controller require authentication by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

var connString = app.Configuration.GetConnectionString("Supabase");
Console.WriteLine($"[DEBUG] Supabase connection string length: {connString?.Length ?? -1}");
Console.WriteLine($"[DEBUG] Starts with: {connString?.Substring(0, Math.Min(10, connString?.Length ?? 0))}");

app.UseForwardedHeaders();

// Enable CORS
app.UseCors(myAllowSpecificOrigins);

app.Use(async (context, next) =>
{
    Console.WriteLine($"[DEBUG] Scheme: {context.Request.Scheme}");
    Console.WriteLine($"[DEBUG] Host: {context.Request.Host}");
    Console.WriteLine($"[DEBUG] X-Forwarded-Proto: {context.Request.Headers["X-Forwarded-Proto"]}");

    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Use either swagger or scalar
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "CampusGo.v1"));
    app.MapScalarApiReference().AllowAnonymous();

}
else
{
    // Uncaught errors are handled using this:
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
        });
    });
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();