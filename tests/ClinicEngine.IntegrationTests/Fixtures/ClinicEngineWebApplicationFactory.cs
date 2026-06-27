using ClinicEngine.Application.Common.Interfaces;
using ClinicEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicEngine.IntegrationTests.Fixtures;

public sealed class ClinicEngineWebApplicationFactory : WebApplicationFactory<Program>
{

    private const string TestDatabaseName = "ClinicEngineIntegrationTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

           
            var userDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICurrentUserService));
            if (userDescriptor is not null)
                services.Remove(userDescriptor);

            
            services.AddScoped<ICurrentUserService, TestCurrentUserService>();

            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(TestDatabaseName);
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics
                        .InMemoryEventId.TransactionIgnoredWarning));
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public string UserName => "test-user";
}