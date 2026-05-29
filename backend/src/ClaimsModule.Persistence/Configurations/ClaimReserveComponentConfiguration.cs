using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimReserveComponentConfiguration : IEntityTypeConfiguration<ClaimReserveComponent>
{
    public void Configure(EntityTypeBuilder<ClaimReserveComponent> b)
    {
        b.ToTable("ClaimReserveComponents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.ComponentType).HasConversion<int>();
        b.Property(x => x.CurrentAmount).HasColumnType("DECIMAL(19,4)");
        b.Property(x => x.ApprovalStatus).HasConversion<int>();
        b.Property(x => x.RejectionReason).HasMaxLength(1000);
        b.Property(x => x.LastApprovedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.LastApprovedBy).HasMaxLength(100);
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
        b.Property(x => x.RowVer).IsRowVersion();
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.History)
            .WithOne()
            .HasForeignKey(h => h.ReserveComponentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ClaimId, x.ComponentType, x.ApprovalStatus });
    }
}
