using AssuranceApp.Data;
using AssuranceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace AssuranceApp.Services;

public class ReclamationService
{
    private readonly AppDbContext context;

    public ReclamationService(AppDbContext context)
    {
        this.context = context;
    }

    public async Task<Reclamation> CreateReclamation(Reclamation reclamation)
    {
        reclamation.CreatedAt = DateTime.UtcNow;
        reclamation.Status = string.IsNullOrWhiteSpace(reclamation.Status) ? "New" : reclamation.Status;

        context.Reclamations.Add(reclamation);
        await context.SaveChangesAsync();

        return reclamation;
    }

    public async Task<List<Reclamation>> GetAllReclamations()
    {
        return await context.Reclamations
            .OrderByDescending(reclamation => reclamation.CreatedAt)
            .ToListAsync();
    }

    public async Task<Reclamation?> GetReclamationById(int id)
    {
        return await context.Reclamations.FirstOrDefaultAsync(reclamation => reclamation.Id == id);
    }

    public async Task<bool> UpdateStatus(int id, string status)
    {
        var reclamation = await context.Reclamations.FirstOrDefaultAsync(item => item.Id == id);
        if (reclamation is null)
        {
            return false;
        }

        reclamation.Status = status;
        await context.SaveChangesAsync();
        return true;
    }
}