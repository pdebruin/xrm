using Microsoft.EntityFrameworkCore;
using Xrm.Core.Models;

namespace Xrm.Core.Data;

public class XrmDbContext : DbContext
{
    public XrmDbContext(DbContextOptions<XrmDbContext> options) : base(options) { }

    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<RelationshipDefinition> RelationshipDefinitions => Set<RelationshipDefinition>();
    public DbSet<Record> Records => Set<Record>();
    public DbSet<RecordLink> RecordLinks => Set<RecordLink>();
    public DbSet<AutoNumberSequence> AutoNumberSequences => Set<AutoNumberSequence>();

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

            entity.HasOne(e => e.ParentEntity)
                .WithMany(ed => ed.ParentRelationships)
                .HasForeignKey(e => e.ParentEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ChildEntity)
                .WithMany(ed => ed.ChildRelationships)
                .HasForeignKey(e => e.ChildEntityId)
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
            entity.HasIndex(e => new { e.RelationshipDefinitionId, e.ParentRecordId, e.ChildRecordId }).IsUnique();

            entity.HasOne(e => e.RelationshipDefinition)
                .WithMany(rd => rd.RecordLinks)
                .HasForeignKey(e => e.RelationshipDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentRecord)
                .WithMany(r => r.ParentLinks)
                .HasForeignKey(e => e.ParentRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChildRecord)
                .WithMany(r => r.ChildLinks)
                .HasForeignKey(e => e.ChildRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AutoNumberSequence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FieldDefinitionId).IsUnique();

            entity.HasOne(e => e.FieldDefinition)
                .WithMany()
                .HasForeignKey(e => e.FieldDefinitionId)
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
