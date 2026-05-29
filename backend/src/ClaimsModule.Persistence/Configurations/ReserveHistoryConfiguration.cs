using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ReserveHistoryConfiguration : IEntityTypeConfiguration<ReserveHistory>
{
    public void Configure(EntityTypeBuilder<ReserveHistory> b)
    {
        b.ToTable("ReserveHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.PreviousAmount).HasColumnType("DECIMAL(19,4)");
        b.Property(x => x.NewAmount).HasColumnType("DECIMAL(19,4)");
        b.Property(x => x.ChangeReason).HasMaxLength(500);
        b.Property(x => x.ChangedBy).HasMaxLength(100);
        b.Property(x => x.ChangedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
        b.HasIndex(x => new { x.ReserveComponentId, x.ChangeSequence }).IsUnique();
    }
}
