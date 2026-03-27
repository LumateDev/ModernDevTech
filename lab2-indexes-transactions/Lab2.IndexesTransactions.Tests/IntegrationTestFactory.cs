using Lab2.IndexesTransactions.Data;
using Lab2.IndexesTransactions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Linq;

namespace Lab2.IndexesTransactions.Tests
{
    public class IntegrationTestFactory : WebApplicationFactory<Lab2.IndexesTransactions.Program>
    {
        private readonly string _postgresConnectionString;
        private readonly string _redisConnectionString;

        public IntegrationTestFactory()
        {
            _postgresConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
                ?? "Host=localhost;Port=5432;Database=lab2db;Username=postgres;Password=postgres";
            _redisConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
                ?? "localhost:6379";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //  отключаем логи EF Core и ASP.NET для тестов
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            builder.ConfigureServices(services =>
            {
                // --- DbContext ---
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor != null) services.Remove(dbDescriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(_postgresConnectionString)
                           .EnableSensitiveDataLogging(false)
                           .EnableDetailedErrors(false));

                // --- Redis ---
                var redisDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null) services.Remove(redisDescriptor);

                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(_redisConnectionString));

                var isolationDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IsolationDemoService));
                if (isolationDescriptor != null) services.Remove(isolationDescriptor);

                services.AddSingleton(sp =>
                    new IsolationDemoService(
                        sp.GetRequiredService<IServiceScopeFactory>(),
                        _postgresConnectionString));
            });
        }
    }
}