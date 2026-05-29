using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class CauseOfLossCodeConfiguration : IEntityTypeConfiguration<CauseOfLossCode>
{
    public void Configure(EntityTypeBuilder<CauseOfLossCode> b)
    {
        b.ToTable("CauseOfLossCodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(255);
        b.Property(x => x.PerilCategory).HasMaxLength(50);
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
        b.HasIndex(x => new { x.OrganizationEntityId, x.Code }).IsUnique();
    }
}
