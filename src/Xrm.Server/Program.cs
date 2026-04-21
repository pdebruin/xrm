using Microsoft.EntityFrameworkCore;
using Xrm.Server.Components;
using Xrm.Server.Data;
using Xrm.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Database — factory pattern for short-lived contexts in Blazor Server
builder.Services.AddDbContextFactory<XrmDbContext>(options =>
    options.UseSqlite("Data Source=xrm.db"));

// Application services (Blazor pages inject these directly)
builder.Services.AddScoped<IEntityService, EntityService>();
builder.Services.AddScoped<IFieldService, FieldService>();
builder.Services.AddScoped<IRelationshipService, RelationshipService>();
builder.Services.AddScoped<IRecordService, RecordService>();

// API Controllers (thin wrappers for external consumers)
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<XrmDbContext>>();
    await using var ctx = await db.CreateDbContextAsync();
    await ctx.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
