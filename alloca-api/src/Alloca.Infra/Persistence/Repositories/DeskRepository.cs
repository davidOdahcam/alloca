using Alloca.Domain.Entities;
using Alloca.Domain.Repositories;

namespace Alloca.Infra.Persistence.Repositories;

public class DeskRepository(AllocaDbContext db) : Repository<Desk>(db), IDeskRepository
{
}
