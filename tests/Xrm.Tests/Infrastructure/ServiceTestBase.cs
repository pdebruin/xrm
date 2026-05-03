using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xrm.Core.Data;
using Xrm.Core.Services;
using Xrm.Server;

namespace Xrm.Tests.Infrastructure;

/// <summary>
/// Creates a fresh in-memory SQLite database per test method.
/// Dispose closes the connection (and drops the DB).
/// </summary>
public class ServiceTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly IDbContextFactory<XrmDbContext> DbFactory;

    protected ServiceTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<XrmDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create schema
        using (var ctx = new XrmDbContext(options))
        {
            ctx.Database.EnsureCreated();
        }

        DbFactory = new TestDbContextFactory(options);
    }

    protected EntityService CreateEntityService() => new(DbFactory, new DemoDataSeeder());
    protected FieldService CreateFieldService() => new(DbFactory);
    protected RelationshipService CreateRelationshipService() => new(DbFactory);
    protected RecordService CreateRecordService() => new(DbFactory, Array.Empty<IRecordLifecycleHandler>(), new AnonymousCurrentUser());
    protected AuditService CreateAuditService() => new(DbFactory);

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private class TestDbContextFactory : IDbContextFactory<XrmDbContext>
    {
        private readonly DbContextOptions<XrmDbContext> _options;
        public TestDbContextFactory(DbContextOptions<XrmDbContext> options) => _options = options;
        public XrmDbContext CreateDbContext() => new(_options);
    }
}
