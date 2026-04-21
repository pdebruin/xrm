using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xrm.Server.Data;

namespace Xrm.Tests.Infrastructure;

public class XrmWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public XrmWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContextFactory registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbContextFactory<XrmDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Also remove DbContextOptions if registered
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<XrmDbContext>));
            if (optionsDescriptor != null) services.Remove(optionsDescriptor);

            // Re-register with in-memory SQLite
            services.AddDbContextFactory<XrmDbContext>(options =>
                options.UseSqlite(_connection));

            // Ensure DB is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<XrmDbContext>>();
            using var ctx = factory.CreateDbContext();
            ctx.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
