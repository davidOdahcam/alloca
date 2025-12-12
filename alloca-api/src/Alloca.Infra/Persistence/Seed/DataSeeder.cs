using System.Text.Json;
using System.Text.Json.Serialization;
using Alloca.Application.Common.Interfaces;
using Alloca.Domain.Entities;
using Alloca.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alloca.Infra.Persistence.Seed;

public class DataSeeder(
    AllocaDbContext db,
    IPasswordHasher hasher,
    IHostEnvironment env,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(env.ContentRootPath, "seed-data.json");
        if (!File.Exists(path))
        {
            logger.LogInformation("seed-data.json não encontrado em {Path}; ignorando seed.", path);
            return;
        }

        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<SeedRoot>(stream, JsonOpts, ct)
                   ?? throw new InvalidOperationException("seed-data.json inválido.");

        await SeedUsersAsync(data.Users, ct);
        await SeedPavilionsAsync(data.Pavilions, ct);
        await AssignManagersAsync(data.Users, ct);
    }

    private async Task SeedUsersAsync(List<SeedUser> users, CancellationToken ct)
    {
        foreach (var s in users)
        {
            var email = s.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, ct)) continue;

            var role = Enum.Parse<UserRole>(s.Role, true);
            db.Users.Add(new User(email, s.FullName, hasher.Hash(s.Password), role));
            logger.LogInformation("Seed user: {Email} ({Role})", email, role);
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedPavilionsAsync(List<SeedPavilion> pavilions, CancellationToken ct)
    {
        foreach (var p in pavilions)
        {
            var code = p.Code.Trim().ToUpperInvariant();
            var pav = await db.Pavilions.FirstOrDefaultAsync(x => x.Code == code, ct);

            if (pav is null)
            {
                pav = new Pavilion(code, p.Name);
                pav.UpdatePolicy(p.MinAdvanceMinutes ?? 5, p.MaxAdvanceDays ?? 30, p.SlotMinutes ?? 30);
                db.Pavilions.Add(pav);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Seed pavilhão: {Code}", code);
            }

            foreach (var oh in p.OperatingHours ?? [])
            {
                var day = Enum.Parse<DayOfWeek>(oh.Day, true);
                var existing = await db.OperatingHours
                    .FirstOrDefaultAsync(o => o.PavilionId == pav.Id && o.DayOfWeek == day, ct);

                if (existing is null)
                {
                    db.OperatingHours.Add(new OperatingHours(pav.Id, day,
                        TimeOnly.Parse(oh.Opens), TimeOnly.Parse(oh.Closes)));
                }
                else
                {
                    existing.Update(TimeOnly.Parse(oh.Opens), TimeOnly.Parse(oh.Closes));
                }
            }
            await db.SaveChangesAsync(ct);

            foreach (var f in p.Floors ?? [])
            {
                var floorCode = f.Code.Trim().ToUpperInvariant();
                var floor = await db.Floors
                    .FirstOrDefaultAsync(x => x.PavilionId == pav.Id && x.Code == floorCode, ct);

                if (floor is null)
                {
                    floor = new Floor(pav.Id, f.Code, f.Name, f.Level, f.SvgKey);
                    db.Floors.Add(floor);
                    await db.SaveChangesAsync(ct);
                }

                foreach (var r in f.Rooms ?? [])
                {
                    var roomExt = r.ExternalId.Trim().ToUpperInvariant();
                    var room = await db.Rooms
                        .FirstOrDefaultAsync(x => x.ExternalId == roomExt, ct);

                    if (room is null)
                    {
                        room = new Room(floor.Id, r.ExternalId, r.Name, r.IsReservable);
                        if (r.Capacity.HasValue) room.SetCapacity(r.Capacity);
                        db.Rooms.Add(room);
                        await db.SaveChangesAsync(ct);
                    }

                    foreach (var d in r.Desks ?? [])
                    {
                        var deskExt = d.ExternalId.Trim().ToUpperInvariant();
                        if (await db.Desks.AnyAsync(x => x.ExternalId == deskExt, ct)) continue;
                        db.Desks.Add(new Desk(room.Id, d.ExternalId, d.Name));
                    }
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }

    private async Task AssignManagersAsync(List<SeedUser> users, CancellationToken ct)
    {
        foreach (var s in users.Where(x => x.ManagesPavilions is { Count: > 0 }))
        {
            var email = s.Email.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user is null) continue;

            foreach (var pavCode in s.ManagesPavilions!)
            {
                var code = pavCode.Trim().ToUpperInvariant();
                var pav = await db.Pavilions.FirstOrDefaultAsync(p => p.Code == code, ct);
                if (pav is null) continue;

                var exists = await db.PavilionManagers
                    .AnyAsync(m => m.PavilionId == pav.Id && m.UserId == user.Id, ct);
                if (exists) continue;

                db.PavilionManagers.Add(new PavilionManager(pav.Id, user.Id));
                logger.LogInformation("Seed manager: {Email} -> {Pavilion}", email, code);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public sealed class SeedRoot
    {
        public List<SeedUser> Users { get; set; } = [];
        public List<SeedPavilion> Pavilions { get; set; } = [];
    }
    public sealed class SeedUser
    {
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Member";
        public List<string>? ManagesPavilions { get; set; }
    }
    public sealed class SeedPavilion
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int? MinAdvanceMinutes { get; set; }
        public int? MaxAdvanceDays { get; set; }
        public int? SlotMinutes { get; set; }
        public List<SeedOperatingHours>? OperatingHours { get; set; }
        public List<SeedFloor>? Floors { get; set; }
    }
    public sealed class SeedOperatingHours
    {
        public string Day { get; set; } = "";
        public string Opens { get; set; } = "";
        public string Closes { get; set; } = "";
    }
    public sealed class SeedFloor
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string? SvgKey { get; set; }
        public List<SeedRoom>? Rooms { get; set; }
    }
    public sealed class SeedRoom
    {
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsReservable { get; set; } = true;
        public int? Capacity { get; set; }
        public List<SeedDesk>? Desks { get; set; }
    }
    public sealed class SeedDesk
    {
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
