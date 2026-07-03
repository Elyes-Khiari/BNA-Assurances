using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace AssuranceApp.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ChatController(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public class ChatRequest
    {
        public string message { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.message))
            return BadRequest(new { reply = "Le message est vide." });

        try
        {
            // 1. Get embedding for the user message
            var embedUrl = _configuration["EmbeddingService:Url"] + "/embed";
            var embedPayload = new { text = request.message };
            var embedResponse = await _httpClient.PostAsJsonAsync(embedUrl, embedPayload);
            
            if (!embedResponse.IsSuccessStatusCode)
            {
                return StatusCode(500, new { reply = "Erreur de connexion au service d'embedding." });
            }
            
            var embedResult = await embedResponse.Content.ReadFromJsonAsync<EmbeddingResponse>();
            var embedding = embedResult?.embedding;

            if (embedding == null)
            {
                return StatusCode(500, new { reply = "Échec de génération de l'embedding." });
            }

            // 2. Query Supabase for similar documents
            var supabaseUrl = _configuration["Supabase:Url"];
            var supabaseKey = _configuration["Supabase:ServiceKey"];

            using var supabaseRequest = new HttpRequestMessage(HttpMethod.Post, $"{supabaseUrl}/rest/v1/rpc/match_documents");
            supabaseRequest.Headers.Add("apikey", supabaseKey);
            supabaseRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
            
            var rpcPayload = new
            {
                query_embedding = embedding,
                match_count = 5
            };
            
            supabaseRequest.Content = new StringContent(JsonSerializer.Serialize(rpcPayload), Encoding.UTF8, "application/json");
            
            Console.WriteLine($"[DEBUG] Calling Supabase RPC: {supabaseUrl}/rest/v1/rpc/match_documents");
            Console.WriteLine($"[DEBUG] Embedding length: {embedding.Count}");
            
            var supabaseResponse = await _httpClient.SendAsync(supabaseRequest);
            
            var supabaseBody = await supabaseResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[DEBUG] Supabase Status: {supabaseResponse.StatusCode}");
            Console.WriteLine($"[DEBUG] Supabase Response: {supabaseBody}");
            
            if (!supabaseResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Supabase Error: {supabaseBody}");
            }

            var documents = supabaseResponse.IsSuccessStatusCode 
                ? JsonSerializer.Deserialize<List<DocumentMatch>>(supabaseBody)
                : new List<DocumentMatch>();
            
            Console.WriteLine($"[DEBUG] Documents found: {documents?.Count ?? 0}");
            
            // Build context from documents
            var contextBuilder = new StringBuilder();
            if (documents != null && documents.Count > 0)
            {
                foreach (var doc in documents)
                {
                    contextBuilder.AppendLine($"Document (Source: {doc.source}, Page: {doc.page_number}):");
                    contextBuilder.AppendLine(doc.content);
                    contextBuilder.AppendLine();
                }
            }
            else
            {
                contextBuilder.AppendLine("Aucune information pertinente n'a été trouvée dans la base de données.");
            }

            // 3. Call Groq API
            var groqApiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(groqApiKey))
            {
                return StatusCode(500, new { reply = "La clé API Groq n'est pas configurée. Veuillez l'ajouter dans appsettings.json." });
            }

            var groqUrl = "https://api.groq.com/openai/v1/chat/completions";

            var systemPrompt = @"Vous êtes l'assistant virtuel de BNA Assurances. 
Répondez aux questions des utilisateurs en vous basant UNIQUEMENT sur le contexte fourni. 
Si la réponse ne se trouve pas dans le contexte, dites poliment que vous ne possédez pas cette information et invitez l'utilisateur à contacter une agence BNA. 
Répondez de manière professionnelle, concise et en français.";

            var groqPayload = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Contexte:\n{contextBuilder.ToString()}\n\nQuestion: {request.message}" }
                },
                temperature = 0.3
            };

            using var groqRequest = new HttpRequestMessage(HttpMethod.Post, groqUrl);
            groqRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", groqApiKey);
            groqRequest.Content = new StringContent(JsonSerializer.Serialize(groqPayload), Encoding.UTF8, "application/json");

            var groqResponse = await _httpClient.SendAsync(groqRequest);
            
            if (!groqResponse.IsSuccessStatusCode)
            {
                var groqErr = await groqResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Groq API Error: {groqErr}");
                return StatusCode(500, new { reply = "Erreur de communication avec le modèle de langage. Veuillez réessayer ultérieurement." });
            }

            var groqResult = await groqResponse.Content.ReadFromJsonAsync<GroqResponse>();
            var aiReply = groqResult?.choices?.FirstOrDefault()?.message?.content ?? "Désolé, je n'ai pas pu générer une réponse.";

            return Ok(new { reply = aiReply });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chat API Error: {ex}");
            return StatusCode(500, new { reply = $"Erreur interne du serveur. Détails : {ex.Message} \n {ex.StackTrace}" });
        }
    }

    public class EmbeddingResponse
    {
        public List<float> embedding { get; set; } = new();
    }

    public class DocumentMatch
    {
        public string content { get; set; } = string.Empty;
        public int page_number { get; set; }
        public string source { get; set; } = string.Empty;
    }

    public class GroqResponse
    {
        public List<GroqChoice> choices { get; set; } = new();
    }

    public class GroqChoice
    {
        public GroqMessage message { get; set; } = new();
    }

    public class GroqMessage
    {
        public string content { get; set; } = string.Empty;
    }
}
