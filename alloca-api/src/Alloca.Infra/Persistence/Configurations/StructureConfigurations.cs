using Alloca.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alloca.Infra.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.Role).HasConversion<int>();
    }
}

public class PavilionConfiguration : IEntityTypeConfiguration<Pavilion>
{
    public void Configure(EntityTypeBuilder<Pavilion> b)
    {
        b.ToTable("Pavilions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();

        b.HasMany(x => x.Floors).WithOne().HasForeignKey(f => f.PavilionId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.OperatingHours).WithOne().HasForeignKey(o => o.PavilionId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Managers).WithOne().HasForeignKey(m => m.PavilionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> b)
    {
        b.ToTable("Floors");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.SvgKey).HasMaxLength(100);
        b.HasIndex(x => new { x.PavilionId, x.Code }).IsUnique();

        b.HasMany(x => x.Rooms).WithOne().HasForeignKey(r => r.FloorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalId).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.ExternalId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasMany(x => x.Desks).WithOne().HasForeignKey(d => d.RoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeskConfiguration : IEntityTypeConfiguration<Desk>
{
    public void Configure(EntityTypeBuilder<Desk> b)
    {
        b.ToTable("Desks");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalId).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.ExternalId).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
    }
}

public class OperatingHoursConfiguration : IEntityTypeConfiguration<OperatingHours>
{
    public void Configure(EntityTypeBuilder<OperatingHours> b)
    {
        b.ToTable("OperatingHours");
        b.HasKey(x => x.Id);
        b.Property(x => x.DayOfWeek).HasConversion<int>();
        b.HasIndex(x => new { x.PavilionId, x.DayOfWeek }).IsUnique();
    }
}

public class PavilionManagerConfiguration : IEntityTypeConfiguration<PavilionManager>
{
    public void Configure(EntityTypeBuilder<PavilionManager> b)
    {
        b.ToTable("PavilionManagers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.PavilionId, x.UserId }).IsUnique();
        b.HasOne<User>().WithMany(u => u.ManagedPavilions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
