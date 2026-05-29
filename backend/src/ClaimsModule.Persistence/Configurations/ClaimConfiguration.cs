using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> b)
    {
        b.ToTable("Claims");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.ClaimNumber).IsRequired().HasMaxLength(50);
        b.HasIndex(x => new { x.OrganizationEntityId, x.ClaimNumber }).IsUnique();
        b.Property(x => x.PolicyNumber).HasMaxLength(50);
        b.Property(x => x.ClientName).HasMaxLength(255);
        b.Property(x => x.ClaimType).HasMaxLength(50);
        b.Property(x => x.CauseOfLossCode).HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.AssignedHandlerUserId).HasMaxLength(100);
        b.Property(x => x.AssignedHandlerName).HasMaxLength(255);
        b.Property(x => x.LossDate).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.ReportedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.LastTouchedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
        b.Property(x => x.RowVer).IsRowVersion();

        b.Ignore(x => x.DomainEvents);

        b.HasOne(x => x.LossEvent)
            .WithOne()
            .HasForeignKey<LossEvent>(le => le.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Parties).WithOne().HasForeignKey(p => p.ClaimId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.RiskObjects).WithOne().HasForeignKey(r => r.ClaimId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Reserves).WithOne().HasForeignKey(r => r.ClaimId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(d => d.ClaimId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.AuditLog).WithOne().HasForeignKey(a => a.ClaimId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.Status);
        b.HasIndex(x => new { x.OrganizationEntityId, x.Status, x.LastTouchedAt });
    }
}
