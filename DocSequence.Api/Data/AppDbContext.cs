using DocSequence.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocSequence.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentType>(e =>
        {
            e.ToTable("DocumentTypes", t =>
            {
                // BIN2 collation: under the default case-insensitive collation, [A-Z] also matches lowercase
                t.HasCheckConstraint("CK_DocumentTypes_Prefix",
                    "Prefix COLLATE Latin1_General_BIN2 NOT LIKE '%[^A-Z]%' AND LEN(Prefix) BETWEEN 2 AND 5");
                t.HasCheckConstraint("CK_DocumentTypes_CurrentNumber", "CurrentNumber >= 0");
            });

            e.HasKey(x => x.DocumentTypeId);
            e.Property(x => x.Prefix).HasMaxLength(5).IsUnicode(false);
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Prefix).IsUnique().HasDatabaseName("UX_DocumentTypes_Prefix");

            // Demo seed (SRS §19.4)
            var seeded = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            e.HasData(
                new DocumentType { DocumentTypeId = 1, Prefix = "CXY", Name = "CXY Drawing", CurrentNumber = 10428, IsActive = true, UpdatedAt = seeded },
                new DocumentType { DocumentTypeId = 2, Prefix = "PXY", Name = "PXY Drawing", CurrentNumber = 10428, IsActive = true, UpdatedAt = seeded });
        });

        modelBuilder.Entity<GeneratedDocument>(e =>
        {
            e.ToTable("GeneratedDocuments");
            e.HasKey(x => x.GeneratedDocumentId);
            e.Property(x => x.Identifier).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.DocumentName).HasMaxLength(200);
            e.Property(x => x.EngineerName).HasMaxLength(100);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => new { x.DocumentTypeId, x.Number }).IsUnique()
             .HasDatabaseName("UX_GeneratedDocuments_DocumentTypeId_Number");
            e.HasIndex(x => x.RequestKey).IsUnique()
             .HasDatabaseName("UX_GeneratedDocuments_RequestKey");
            e.HasIndex(x => x.Identifier).IsUnique()
             .HasDatabaseName("UX_GeneratedDocuments_Identifier");
            e.HasIndex(x => new { x.CreatedAt, x.GeneratedDocumentId }).IsDescending()
             .HasDatabaseName("IX_GeneratedDocuments_CreatedAt");

            e.HasOne(x => x.DocumentType).WithMany()
             .HasForeignKey(x => x.DocumentTypeId)
             .HasConstraintName("FK_GeneratedDocuments_DocumentTypes")
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}