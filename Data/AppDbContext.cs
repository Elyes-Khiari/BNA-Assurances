using AssuranceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AssuranceApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Reclamation> Reclamations => Set<Reclamation>();

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();

    public DbSet<ClientRecord> ClientRecords => Set<ClientRecord>();
}