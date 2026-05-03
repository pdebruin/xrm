using Xrm.Core.Models;
using Xrm.Core.Services;
using Xrm.Tests.Infrastructure;

namespace Xrm.Tests.Services;

public class AuthorizationTests : ServiceTestBase
{
    private RecordService CreateRecordServiceWithUser(ICurrentUser user) =>
        new(DbFactory, Array.Empty<IRecordLifecycleHandler>(), user);

    private async Task<Guid> CreateEntityInDomain(string name, string? domain)
    {
        var svc = CreateEntityService();
        var entity = await svc.CreateAsync(new EntityDefinition { Name = name, Domain = domain });
        var fieldSvc = CreateFieldService();
        await fieldSvc.CreateAsync(entity.Id, new FieldDefinition { Name = "Title", DataType = FieldDataType.Text });
        return entity.Id;
    }

    [Fact]
    public async Task AnonymousUser_CanAccess_NullDomainEntity()
    {
        var entityId = await CreateEntityInDomain("Shared", null);
        var svc = CreateRecordServiceWithUser(new AnonymousCurrentUser());

        var result = await svc.CreateAsync(entityId, """{"Title":"Test"}""");
        Assert.True(result.Success);

        var records = await svc.GetAllAsync(entityId);
        Assert.Equal(1, records.Total);
    }

    [Fact]
    public async Task RestrictedUser_CannotRead_DomainEntity()
    {
        var entityId = await CreateEntityInDomain("Finance", "finance");
        var user = new TestCurrentUser(canRead: false, canWrite: false);
        var svc = CreateRecordServiceWithUser(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.GetAllAsync(entityId));
    }

    [Fact]
    public async Task RestrictedUser_CannotWrite_DomainEntity()
    {
        var entityId = await CreateEntityInDomain("Finance", "finance");
        var user = new TestCurrentUser(canRead: true, canWrite: false);
        var svc = CreateRecordServiceWithUser(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CreateAsync(entityId, """{"Title":"Test"}"""));
    }

    [Fact]
    public async Task ReaderUser_CanRead_ButNotWrite()
    {
        var entityId = await CreateEntityInDomain("Sales", "sales");
        var user = new TestCurrentUser(canRead: true, canWrite: false, domain: "sales");
        var svc = CreateRecordServiceWithUser(user);

        // First create with a full-access user
        var adminSvc = CreateRecordServiceWithUser(new AnonymousCurrentUser());
        var result = await adminSvc.CreateAsync(entityId, """{"Title":"Test"}""");
        Assert.True(result.Success);

        // Reader can read
        var records = await svc.GetAllAsync(entityId);
        Assert.Equal(1, records.Total);

        // Reader cannot write
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CreateAsync(entityId, """{"Title":"Another"}"""));
    }

    [Fact]
    public async Task WriterUser_CanReadAndWrite()
    {
        var entityId = await CreateEntityInDomain("HR", "hr");
        var user = new TestCurrentUser(canRead: true, canWrite: true, domain: "hr");
        var svc = CreateRecordServiceWithUser(user);

        var result = await svc.CreateAsync(entityId, """{"Title":"Employee"}""");
        Assert.True(result.Success);

        var records = await svc.GetAllAsync(entityId);
        Assert.Equal(1, records.Total);
    }

    /// <summary>
    /// Test ICurrentUser with configurable access per domain.
    /// </summary>
    private class TestCurrentUser : ICurrentUser
    {
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly string? _domain;

        public TestCurrentUser(bool canRead, bool canWrite, string? domain = null)
        {
            _canRead = canRead;
            _canWrite = canWrite;
            _domain = domain;
        }

        public string? UserKey => "test|user1";
        public string? Email => "test@example.com";
        public string? DisplayName => "Test User";
        public bool IsAuthenticated => true;
        public bool IsSystemAdmin => false;

        public bool CanRead(string? domain)
        {
            if (domain is null) return true; // null-domain entities accessible to all
            if (_domain is not null && string.Equals(_domain, domain, StringComparison.OrdinalIgnoreCase))
                return _canRead;
            return false;
        }

        public bool CanWrite(string? domain)
        {
            if (domain is null) return true;
            if (_domain is not null && string.Equals(_domain, domain, StringComparison.OrdinalIgnoreCase))
                return _canWrite;
            return false;
        }
    }
}
