using Microsoft.EntityFrameworkCore;

namespace FinBot.Dal.DbContexts;

public class ReplicaDbContext(DbContextOptions<ReplicaDbContext> options) : PDbContext(options)
{
    
}