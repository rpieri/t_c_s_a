using Carrefour.CaseFlow.Consolidado.Service.Data;
using Carrefour.CaseFlow.Consolidado.Service.Services;
using Carrefour.CaseFlow.Consolidado.Worker;
using Carrefour.CaseFlow.Shared.Kafka.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddSerilog();

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

builder.Services.AddKafkaConsumer(builder.Configuration, "consolidado-service");

// Services
builder.Services.AddScoped<IConsolidacaoService, ConsolidacaoService>();


builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
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

Log.Information("Consolidado Worker started successfully");
host.Run();