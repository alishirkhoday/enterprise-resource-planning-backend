using ERP.Application;
using ERP.Infrastructure;
using ERP.WebApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

var rootPath = Path.Combine(builder.Environment.ContentRootPath, "..", "settings.json");
if (File.Exists(rootPath))
    builder.Configuration.AddJsonFile(rootPath, true, true);

builder.Host.UseSerilog();

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorizationServices(builder.Configuration);
builder.Services.AddLogServices(builder.Configuration, builder.Environment);
builder.Services.AddSendMessageServices(builder.Configuration);

//Swagger
builder.Services.AddApiVersionAndSwaggerServices(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddControllers();

var app = builder.Build();

// Exceptions are handled by ExceptionHandlingMiddleware in every environment, so that a caller
// always gets the same envelope back and the incident is always logged. What differs by
// environment is only how much of the exception the caller is told, which the middleware decides.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.AddWebApplications();

try
{
    Log.Information("ERP Application Starting Up...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ERP Application Failed to Start.");
}
finally
{
    Log.CloseAndFlush();
}