using AssuranceApp.Models;
using AssuranceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AssuranceApp.Controllers;

[Authorize]
public class ReclamationController : Controller
{
    private readonly ReclamationService _reclamationService;

    public ReclamationController(ReclamationService reclamationService)
    {
        _reclamationService = reclamationService;
    }

    // GET: /Reclamation/MesReclamations
    public async Task<IActionResult> MesReclamations()
    {
        var numeroPermis = User.FindFirst("NumeroPermis")?.Value;
        if (string.IsNullOrEmpty(numeroPermis))
        {
            return Unauthorized("Numéro de permis introuvable dans la session.");
        }

        var reclamations = await _reclamationService.GetReclamationsByPermis(numeroPermis);
        return View(reclamations);
    }

    // GET: /Reclamation/Gestion
    [Authorize(Roles = "Assureur")]
    public async Task<IActionResult> Gestion(string? search)
    {
        var reclamations = await _reclamationService.GetAllReclamations();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            reclamations = reclamations.Where(r => 
                (r.NumeroReclamation != null && r.NumeroReclamation.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                r.IdReclamation.ToString() == search
            ).ToList();
            
            ViewData["SearchQuery"] = search;
        }

        return View(reclamations);
    }

    // POST: /Reclamation/UpdateStatus
    [HttpPost]
    [Authorize(Roles = "Assureur")]
    public async Task<IActionResult> UpdateStatus(int idReclamation, string status, string commentaireResolution)
    {
        await _reclamationService.UpdateStatus(idReclamation, status, commentaireResolution);
        return RedirectToAction(nameof(Gestion));
    }

    // GET: /Reclamation/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }
}
