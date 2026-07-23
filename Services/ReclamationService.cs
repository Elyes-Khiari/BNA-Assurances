using System.Text.Json;
using System.Text.Json.Serialization;
using AssuranceApp.Models;
using Microsoft.Extensions.Configuration;

namespace AssuranceApp.Services;

public class ReclamationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public ReclamationService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http = httpClientFactory.CreateClient();
        _config = config;
        _supabaseUrl = _config["Supabase:Url"];
        _supabaseKey = _config["Supabase:ServiceKey"];
        
        _http.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _supabaseKey);
    }

    public async Task<Reclamation> CreateReclamation(Reclamation reclamation)
    {
        reclamation.CreatedAt = DateTime.UtcNow;
        reclamation.UpdatedAt = DateTime.UtcNow;
        reclamation.Statut = StatutReclamation.Ouverte;

        // Temporarily set a dummy NumeroReclamation since it's required by the model
        reclamation.NumeroReclamation = "TEMP"; 

        var createReq = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/Reclamations");
        createReq.Headers.Add("Prefer", "return=representation");
        createReq.Content = System.Net.Http.Json.JsonContent.Create(reclamation, options: new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });

        var createRes = await _http.SendAsync(createReq);
        if (!createRes.IsSuccessStatusCode)
        {
            var err = await createRes.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create reclamation in Supabase: {err}");
        }

        var json = await createRes.Content.ReadAsStringAsync();
        var createdList = JsonSerializer.Deserialize<List<Reclamation>>(json);
        if (createdList == null || createdList.Count == 0) throw new Exception("No data returned from Supabase.");
        
        var created = createdList[0];

        // Update with the final NumeroReclamation
        created.NumeroReclamation = $"REC-{DateTime.UtcNow:yyyy}-{created.IdReclamation:D6}";
        
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/Reclamations?IdReclamation=eq.{created.IdReclamation}");
        patchReq.Content = System.Net.Http.Json.JsonContent.Create(new { NumeroReclamation = created.NumeroReclamation });
        await _http.SendAsync(patchReq);

        return created;
    }

    public async Task<List<Reclamation>> GetAllReclamations()
    {
        var response = await _http.GetAsync($"{_supabaseUrl}/rest/v1/Reclamations?select=*");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("GetAllReclamations Error: " + await response.Content.ReadAsStringAsync());
            return new List<Reclamation>();
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<Reclamation>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Reclamation>();
        return result.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<List<Reclamation>> GetReclamationsByPermis(string numeroPermis)
    {
        var response = await _http.GetAsync($"{_supabaseUrl}/rest/v1/Reclamations?NumeroPermis=eq.{numeroPermis}&select=*");
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("GetReclamationsByPermis Error: " + await response.Content.ReadAsStringAsync());
            return new List<Reclamation>();
        }
        
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<Reclamation>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Reclamation>();
        return result.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<Reclamation?> GetReclamationById(int id)
    {
        var response = await _http.GetAsync($"{_supabaseUrl}/rest/v1/Reclamations?IdReclamation=eq.{id}&select=*");
        if (!response.IsSuccessStatusCode) return null;
        
        var json = await response.Content.ReadAsStringAsync();
        var list = JsonSerializer.Deserialize<List<Reclamation>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return list?.FirstOrDefault();
    }

    public async Task<bool> UpdateStatus(int id, string status, string? commentaire = null)
    {
        if (!Enum.TryParse<StatutReclamation>(status, true, out var statut))
            return false;

        var updates = new Dictionary<string, object>
        {
            { "Statut", (int)statut },
            { "UpdatedAt", DateTime.UtcNow }
        };

        if (!string.IsNullOrEmpty(commentaire))
            updates["CommentaireResolution"] = commentaire;

        if (statut == StatutReclamation.Resolue || statut == StatutReclamation.Cloturee)
            updates["DateResolution"] = DateTime.UtcNow;

        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/Reclamations?IdReclamation=eq.{id}");
        patchReq.Content = System.Net.Http.Json.JsonContent.Create(updates);
        
        var response = await _http.SendAsync(patchReq);
        return response.IsSuccessStatusCode;
    }
}
