using AssuranceApp.Models;
using AssuranceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApp.Controllers;

public class ReclamationController : Controller
{
    private readonly ReclamationService reclamationService;

    public ReclamationController(ReclamationService reclamationService)
    {
        this.reclamationService = reclamationService;
    }

    public async Task<IActionResult> Index()
    {
        var reclamations = await reclamationService.GetAllReclamations();
        return View(reclamations);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reclamation reclamation)
    {
        if (!ModelState.IsValid)
        {
            return View(reclamation);
        }

        await reclamationService.CreateReclamation(reclamation);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var reclamation = await reclamationService.GetReclamationById(id);
        if (reclamation is null)
        {
            return NotFound();
        }

        return View(reclamation);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var updated = await reclamationService.UpdateStatus(id, status);
        if (!updated)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}