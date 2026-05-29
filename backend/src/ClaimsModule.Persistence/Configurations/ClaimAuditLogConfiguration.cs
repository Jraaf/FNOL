using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimAuditLogConfiguration : IEntityTypeConfiguration<ClaimAuditLog>
{
    public void Configure(EntityTypeBuilder<ClaimAuditLog> b)
    {
        b.ToTable("ClaimAuditLog");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.EventType).HasMaxLength(100);
        b.Property(x => x.OldValues).HasMaxLength(-1);
        b.Property(x => x.NewValues).HasMaxLength(-1);
        b.Property(x => x.TriggeredBy).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.OccurredAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
        b.HasIndex(x => new { x.ClaimId, x.OccurredAt });
    }
}
