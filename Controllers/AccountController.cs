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
    private readonly AssuranceApp.Services.EmailService _emailService;

    public AccountController(AppDbContext context, IConfiguration config, AssuranceApp.Services.EmailService emailService)
    {
        _context = context;
        _config = config;
        _emailService = emailService;
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

        var response = await client.GetAsync($"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{Uri.EscapeDataString(email)}&select=*");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || responseContent == "[]")
        {
            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

        using var cDoc = System.Text.Json.JsonDocument.Parse(responseContent);
        var authUser = cDoc.RootElement[0];
        
        var dbHash = authUser.GetProperty("PasswordHash").GetString();
        bool isPasswordValid = false;
        try
        {
            isPasswordValid = BCrypt.Net.BCrypt.Verify(password, dbHash);
        }
        catch
        {
            // If dbHash is not a valid bcrypt hash (e.g. old test accounts)
            isPasswordValid = false;
        }

        if (!isPasswordValid)
        {
            ViewBag.Error = "Email ou mot de passe incorrect.";
            return View();
        }

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
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var newUser = new System.Collections.Generic.Dictionary<string, string>
        {
            { "FullName", fullName },
            { "Email", email },
            { "PasswordHash", hashedPassword },
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

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await client.GetAsync($"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{email}&select=Id");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || responseContent == "[]")
        {
            ViewBag.Message = "Si un compte avec cet e-mail existe, un code a été envoyé.";
            return View();
        }

        var random = new Random();
        var code = random.Next(100000, 999999).ToString();
        var expires = DateTime.UtcNow.AddMinutes(15).ToString("O");

        var updateReq = new HttpRequestMessage(HttpMethod.Patch, $"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{Uri.EscapeDataString(email)}");
        
        var payload = new System.Collections.Generic.Dictionary<string, string>
        {
            { "ResetCode", code },
            { "ResetCodeExpires", expires }
        };
        updateReq.Content = System.Net.Http.Json.JsonContent.Create(payload);
        
        var patchRes = await client.SendAsync(updateReq);
        
        if (!patchRes.IsSuccessStatusCode)
        {
            var err = await patchRes.Content.ReadAsStringAsync();
            ViewBag.Message = $"Erreur DB: {patchRes.StatusCode} - {err}";
            return View();
        }

        var body = $"Votre code de vérification est: <b>{code}</b>. Il expire dans 15 minutes.";
        await _emailService.SendEmailAsync(email, "Code de réinitialisation de mot de passe", body);

        TempData["ResetEmail"] = email;
        return RedirectToAction("VerifyCode");
    }

    [HttpGet]
    public IActionResult VerifyCode()
    {
        if (TempData["ResetEmail"] == null)
        {
            return RedirectToAction("ForgotPassword");
        }
        
        TempData.Keep("ResetEmail");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyCode(string code)
    {
        code = code?.Trim();
        var email = TempData["ResetEmail"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("ForgotPassword");
        }

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

        var response = await client.GetAsync($"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{Uri.EscapeDataString(email)}&select=ResetCode,ResetCodeExpires");
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || responseContent == "[]")
        {
            ViewBag.Error = "Erreur lors de la vérification.";
            TempData.Keep("ResetEmail");
            return View();
        }

        using var cDoc = System.Text.Json.JsonDocument.Parse(responseContent);
        var dbUser = cDoc.RootElement[0];
        var dbCode = dbUser.GetProperty("ResetCode").GetString();
        
        DateTime? expires = null;
        if (dbUser.TryGetProperty("ResetCodeExpires", out var expiresProp) && expiresProp.ValueKind != System.Text.Json.JsonValueKind.Null)
        {
            if (DateTime.TryParse(expiresProp.GetString(), out var d))
                expires = d.ToUniversalTime();
        }

        if (dbCode != code || expires == null || expires < DateTime.UtcNow)
        {
            ViewBag.Error = "Le code est invalide ou a expiré.";
            TempData.Keep("ResetEmail");
            return View();
        }

        TempData["ResetVerifiedEmail"] = email;
        return RedirectToAction("ResetPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        if (TempData["ResetVerifiedEmail"] == null)
        {
            return RedirectToAction("ForgotPassword");
        }
        TempData.Keep("ResetVerifiedEmail");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string newPassword)
    {
        var email = TempData["ResetVerifiedEmail"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("ForgotPassword");
        }

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("apikey", supabaseKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

        var updateReq = new HttpRequestMessage(HttpMethod.Patch, $"{supabaseUrl}/rest/v1/ApplicationUsers?Email=eq.{Uri.EscapeDataString(email)}");
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        
        var payload = new System.Collections.Generic.Dictionary<string, string?>
        {
            { "PasswordHash", hashedPassword },
            { "ResetCode", null },
            { "ResetCodeExpires", null }
        };
        updateReq.Content = System.Net.Http.Json.JsonContent.Create(payload);
        
        await client.SendAsync(updateReq);

        return RedirectToAction("Login");
    }
}
