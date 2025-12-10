using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Infra.Persistence;

public class AllocaDbContext(DbContextOptions<AllocaDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Pavilion> Pavilions => Set<Pavilion>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Desk> Desks => Set<Desk>();
    public DbSet<OperatingHours> OperatingHours => Set<OperatingHours>();
    public DbSet<PavilionManager> PavilionManagers => Set<PavilionManager>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<UserStrike> UserStrikes => Set<UserStrike>();
    public DbSet<UserSuspension> UserSuspensions => Set<UserSuspension>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AllocaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
