using Microsoft.EntityFrameworkCore;
using Xrm.Core;
using Xrm.Core.Data;
using Xrm.Server;
using Xrm.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// XRM core services (DbContext, entity/field/relationship/record services)
builder.Services.AddXrmCore("Data Source=xrm.db");
builder.Services.AddXrmSeeder<DemoDataSeeder>();

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
    await ctx.ApplySchemaUpgradesAsync();
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
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Xrm.Blazor.Components.App).Assembly);

app.Run();

public partial class Program { }
