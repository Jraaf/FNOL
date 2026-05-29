using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> b)
    {
        b.ToTable("ClaimDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.BlobReference).HasMaxLength(1000);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.DocumentType).HasConversion<int>();
        b.Property(x => x.UploadedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UploadedBy).HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
    }
}
