using Microsoft.EntityFrameworkCore;
using Xrm.Server.Models;

namespace Xrm.Server.Data;

public class XrmDbContext : DbContext
{
    public XrmDbContext(DbContextOptions<XrmDbContext> options) : base(options) { }

    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<RelationshipDefinition> RelationshipDefinitions => Set<RelationshipDefinition>();
    public DbSet<Record> Records => Set<Record>();
    public DbSet<RecordLink> RecordLinks => Set<RecordLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.PluralName).HasMaxLength(200);
        });

        modelBuilder.Entity<FieldDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityDefinitionId, e.Name }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.DataType).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.EntityDefinition)
                .WithMany(ed => ed.Fields)
                .HasForeignKey(e => e.EntityDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RelationshipDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.RelationshipType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CascadeBehavior).HasConversion<string>().HasMaxLength(50);

            entity.HasOne(e => e.SourceEntity)
                .WithMany(ed => ed.SourceRelationships)
                .HasForeignKey(e => e.SourceEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetEntity)
                .WithMany(ed => ed.TargetRelationships)
                .HasForeignKey(e => e.TargetEntityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Record>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntityDefinitionId);
            entity.Property(e => e.DataJson).IsRequired();

            entity.HasOne(e => e.EntityDefinition)
                .WithMany(ed => ed.Records)
                .HasForeignKey(e => e.EntityDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecordLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RelationshipDefinitionId, e.SourceRecordId, e.TargetRecordId }).IsUnique();

            entity.HasOne(e => e.RelationshipDefinition)
                .WithMany(rd => rd.RecordLinks)
                .HasForeignKey(e => e.RelationshipDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceRecord)
                .WithMany(r => r.SourceLinks)
                .HasForeignKey(e => e.SourceRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetRecord)
                .WithMany(r => r.TargetLinks)
                .HasForeignKey(e => e.TargetRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is EntityDefinition ed) { ed.CreatedAt = now; ed.ModifiedAt = now; }
                else if (entry.Entity is FieldDefinition fd) { fd.CreatedAt = now; fd.ModifiedAt = now; }
                else if (entry.Entity is RelationshipDefinition rd) { rd.CreatedAt = now; rd.ModifiedAt = now; }
                else if (entry.Entity is Record r) { r.CreatedAt = now; r.ModifiedAt = now; }
                else if (entry.Entity is RecordLink rl) { rl.CreatedAt = now; }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is EntityDefinition ed) { ed.ModifiedAt = now; }
                else if (entry.Entity is FieldDefinition fd) { fd.ModifiedAt = now; }
                else if (entry.Entity is RelationshipDefinition rd) { rd.ModifiedAt = now; }
                else if (entry.Entity is Record r) { r.ModifiedAt = now; }
            }
        }
    }
}
