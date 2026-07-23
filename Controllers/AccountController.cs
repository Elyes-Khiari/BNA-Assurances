using System.Security.Claims;
using AssuranceApp.Data;
using AssuranceApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssuranceApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AccountController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await client.GetAsync($"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{email}&PasswordHash=eq.{password}&select=*");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || responseContent == "[]")
        {
            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        using var cDoc = System.Text.Json.JsonDocument.Parse(responseContent);
        var authUser = cDoc.RootElement[0];

        var userModel = new ApplicationUser
        {
            Id = authUser.GetProperty("Id").GetInt32(),
            FullName = authUser.GetProperty("FullName").GetString() ?? "",
            Email = authUser.GetProperty("Email").GetString() ?? "",
            Role = authUser.GetProperty("Role").GetString() ?? "",
            NumeroPermis = authUser.GetProperty("NumeroPermis").GetString() ?? ""
        };

        await SignInUser(userModel);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Signup()
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Signup(string fullName, string email, string password, string numeroPermis)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

        // 1. Check if user with email already exists in Supabase
        var emailCheckRes = await client.GetAsync($"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{email}&select=Id");
        var emailCheckJson = await emailCheckRes.Content.ReadAsStringAsync();
        if (emailCheckRes.IsSuccessStatusCode && emailCheckJson != "[]")
        {
            ViewBag.Error = "Cet email est déjà utilisé.";
            return View();
        }

        // 2. Check if NumeroPermis exists in ClientRecords
        var response = await client.GetAsync($"{supabaseUrl}/rest/v1/ClientRecords?NumeroPermis=eq.{numeroPermis}&select=*");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || responseContent == "[]")
        {
            ViewBag.Error = "Aucun contrat trouvé avec ce numéro de permis. Veuillez contacter un agent pour souscrire à un contrat.";
            return View();
        }

        // 3. Create User in Supabase
        var newUser = new System.Collections.Generic.Dictionary<string, string>
        {
            { "FullName", fullName },
            { "Email", email },
            { "PasswordHash", password },
            { "Role", "Client" },
            { "NumeroPermis", numeroPermis }
        };

        var createReq = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/rest/v1/ApplicationUsers");
        createReq.Headers.Add("Prefer", "return=representation");
        createReq.Content = System.Net.Http.Json.JsonContent.Create(newUser);

        var createRes = await client.SendAsync(createReq);
        if (!createRes.IsSuccessStatusCode)
        {
            var errorContent = await createRes.Content.ReadAsStringAsync();
            ViewBag.Error = $"Erreur lors de la création du compte: {errorContent}";
            return View();
        }

        var createdJson = await createRes.Content.ReadAsStringAsync();
        using var cDoc = System.Text.Json.JsonDocument.Parse(createdJson);
        var createdUser = cDoc.RootElement[0];

        var userModel = new ApplicationUser
        {
            Id = createdUser.GetProperty("Id").GetInt32(),
            FullName = createdUser.GetProperty("FullName").GetString() ?? "",
            Email = createdUser.GetProperty("Email").GetString() ?? "",
            Role = createdUser.GetProperty("Role").GetString() ?? "",
            NumeroPermis = createdUser.GetProperty("NumeroPermis").GetString() ?? ""
        };

        // 4. Sign in
        await SignInUser(userModel);
        
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInUser(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("NumeroPermis", user.NumeroPermis)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            });
    }
}
