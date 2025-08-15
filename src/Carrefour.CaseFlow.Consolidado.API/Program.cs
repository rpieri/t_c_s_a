
using Carrefour.CaseFlow.Consolidado.Service.Data;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using Carrefour.CaseFlow.Shared.Kafka.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Consolidado API", 
        Version = "v1",
        Description = "API para consolidação de saldos diários"
    });
});

// Database
builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});

builder.Services.AddScoped<IDatabase>(provider =>
    provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase());


// Services
builder.Services.AddScoped<IConsolidacaoService, ConsolidacaoService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("database", () =>
    {
        try
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
            var canConnect = context.Database.CanConnect();
            return canConnect 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is reachable")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database is not reachable");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    })
    .AddCheck("kafka", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Kafka is configured"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consolidado API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Database Migration
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ConsolidadoDbContext>();
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database migration");
        throw;
    }
}

Log.Information("Consolidado API started successfully on {Environment}", app.Environment.EnvironmentName);

app.Run();