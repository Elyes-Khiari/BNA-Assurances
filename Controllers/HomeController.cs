using Microsoft.AspNetCore.Mvc;

namespace AssuranceApp.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Route("/particuliers")]
    public IActionResult Particuliers()
    {
        return View();
    }

    [Route("/entreprises")]
    public IActionResult Entreprises()
    {
        return View();
    }

    [Route("/nos-agences")]
    public IActionResult NosAgences()
    {
        return View();
    }

    [Route("/devis")]
    public IActionResult Devis()
    {
        return View();
    }

    [Route("/sinistres")]
    public IActionResult Sinistres()
    {
        return View();
    }

    [Route("/actualites")]
    public IActionResult Actualites()
    {
        return View();
    }

    [Route("/contact")]
    public IActionResult Contact()
    {
        return View();
    }
}
