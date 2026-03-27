using Lab2.IndexesTransactions.Data;
using Lab2.IndexesTransactions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=postgres;Port=5432;Database=lab2db;Username=postgres;Password=postgres";
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? "redis:6379";

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));

// Services
builder.Services.AddScoped<IndexDemoService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddSingleton(sp =>
    new IsolationDemoService(sp.GetRequiredService<IServiceScopeFactory>(), connectionString));
builder.Services.AddScoped<CacheService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lab2 - Indexes, Transactions, Isolation, Cache",
        Version = "v1",
        Description = "API для демонстрации работы с индексами, транзакциями, уровнями изоляции и кэшированием"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab2 API v1");
    options.RoutePrefix = "swagger";
});

// Auto-migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.Run();

namespace Lab2.IndexesTransactions
{
    public partial class Program { }
}