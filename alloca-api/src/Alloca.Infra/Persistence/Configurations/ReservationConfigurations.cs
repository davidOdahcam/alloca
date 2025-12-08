using Alloca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alloca.Infra.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("Reservations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.ResourceType).HasConversion<int>();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.DecisionReason).HasMaxLength(500);

        b.OwnsOne(x => x.Period, p =>
        {
            p.Property(t => t.StartUtc).HasColumnName("StartUtc").IsRequired();
            p.Property(t => t.EndUtc).HasColumnName("EndUtc").IsRequired();
        });

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.PavilionId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.RoomId);
        b.HasIndex(x => x.DeskId);
    }
}

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> b)
    {
        b.ToTable("Blocks");
        b.HasKey(x => x.Id);
        b.Property(x => x.TargetType).HasConversion<int>();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.OwnsOne(x => x.Period, p =>
        {
            p.Property(t => t.StartUtc).HasColumnName("StartUtc").IsRequired();
            p.Property(t => t.EndUtc).HasColumnName("EndUtc").IsRequired();
        });
        b.HasIndex(x => new { x.TargetType, x.TargetId });
    }
}

public class UserStrikeConfiguration : IEntityTypeConfiguration<UserStrike>
{
    public void Configure(EntityTypeBuilder<UserStrike> b)
    {
        b.ToTable("UserStrikes");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.ExpiresAt);
    }
}

public class UserSuspensionConfiguration : IEntityTypeConfiguration<UserSuspension>
{
    public void Configure(EntityTypeBuilder<UserSuspension> b)
    {
        b.ToTable("UserSuspensions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.StartsAt, x.EndsAt });
    }
}
