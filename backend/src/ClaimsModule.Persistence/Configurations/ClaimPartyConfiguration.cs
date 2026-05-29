using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClaimsModule.Persistence.Configurations;

public class ClaimPartyConfiguration : IEntityTypeConfiguration<ClaimParty>
{
    public void Configure(EntityTypeBuilder<ClaimParty> b)
    {
        b.ToTable("ClaimParties");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        b.Property(x => x.PartyType).HasConversion<int>();
        b.Property(x => x.FirstName).HasMaxLength(255);
        b.Property(x => x.LastName).HasMaxLength(255);
        b.Property(x => x.ContactEmail).HasMaxLength(255);
        b.Property(x => x.ContactPhone).HasMaxLength(50);
        b.Property(x => x.AddressLine).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UpdatedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.DeletedAt).HasColumnType("DATETIMEOFFSET(7)");
        b.Property(x => x.UserCreated).HasMaxLength(100);
        b.Property(x => x.UserModified).HasMaxLength(100);
    }
}
