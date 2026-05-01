namespace Xrm.Core.Data;

/// <summary>
/// Interface for pluggable data seeders. Domain implementations register
/// their own seeder to populate entities, fields, and relationships.
/// </summary>
public interface IDataSeeder
{
    Task SeedAsync(XrmDbContext db);
}
