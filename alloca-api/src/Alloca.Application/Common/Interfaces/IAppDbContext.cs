using Alloca.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alloca.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Pavilion> Pavilions { get; }
    DbSet<Floor> Floors { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Desk> Desks { get; }
    DbSet<OperatingHours> OperatingHours { get; }
    DbSet<PavilionManager> PavilionManagers { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Block> Blocks { get; }
    DbSet<UserStrike> UserStrikes { get; }
    DbSet<UserSuspension> UserSuspensions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
