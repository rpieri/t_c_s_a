using Carrefour.CaseFlow.Lancamentos.Application.Commands;
using Carrefour.CaseFlow.Lancamentos.Application.Handlers;
using Carrefour.CaseFlow.Lancamentos.Application.Validators;
using Carrefour.CaseFlow.Lancamentos.Domain.Interfaces;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Data;
using Carrefour.CaseFlow.Lancamentos.Infrastructure.Repositories;
using Carrefour.CaseFlow.Shared.Kafka.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
    
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Lancamentos API", Version = "v1" });
    c.EnableAnnotations();
});

builder.Services.AddDbContext<LancamentoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(CreateLancamentoHandler).Assembly);
});

builder.Services.AddValidatorsFromAssemblyContaining<CreateLancamentoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateLancamentoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<DeleteLancamentoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<GetLancamentosByPeriodoValidator>();


builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();

builder.Services.AddKafkaProducer(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck("database", () => 
    {
        try
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LancamentoDbContext>();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lancamentos API v1");
        c.RoutePrefix = "swagger";
    });
}


app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

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
        var context = scope.ServiceProvider.GetRequiredService<LancamentoDbContext>();
        await context.Database.EnsureCreatedAsync();
        Log.Information("Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error during database migration");
        throw;
    }
}

Log.Information("Lancamentos API started successfully");

app.Run();