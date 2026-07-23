using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Text;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public ChatController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http = httpClientFactory.CreateClient();
        _config = config;
    }

    // =========================
    // MODELS
    // =========================
    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string SessionId { get; set; } = "";
    }

    // =========================
    // CONVERSATION
    // =========================
    private async Task<Guid> GetOrCreateConversation(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = Guid.NewGuid().ToString();

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{supabaseUrl}/rest/v1/conversations?session_id=eq.{sessionId}&select=id"
        );

        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", supabaseKey);

        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.GetArrayLength() > 0)
        {
            return Guid.Parse(doc.RootElement[0].GetProperty("id").GetString()!);
        }

        var create = new HttpRequestMessage(
            HttpMethod.Post,
            $"{supabaseUrl}/rest/v1/conversations"
        );

        create.Headers.Add("apikey", supabaseKey);
        create.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", supabaseKey);
        create.Headers.Add("Prefer", "return=representation");

        create.Content = JsonContent.Create(new
        {
            session_id = sessionId
        });

        var createRes = await _http.SendAsync(create);
        var createdJson = await createRes.Content.ReadAsStringAsync();

        using var createdDoc = JsonDocument.Parse(createdJson);

        return Guid.Parse(createdDoc.RootElement[0].GetProperty("id").GetString()!);
    }

    // =========================
    // SAVE MESSAGE
    // =========================
    private async Task SaveMessage(Guid conversationId, string role, string content)
    {
        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{supabaseUrl}/rest/v1/messages"
        );

        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", supabaseKey);
        request.Headers.Add("Prefer", "return=representation");

        request.Content = JsonContent.Create(new
        {
            conversation_id = conversationId,
            role = role,
            content = content
        });

        await _http.SendAsync(request);
    }

    // =========================
    // STREAM ENDPOINT
    // =========================
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/plain; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Message is required.");
            return;
        }

        var sessionId = request.SessionId;
        string? numeroPermis = null;

        if (User.Identity is { IsAuthenticated: true })
        {
            numeroPermis = User.FindFirst("NumeroPermis")?.Value;
            if (!string.IsNullOrEmpty(numeroPermis))
            {
                sessionId = $"client_{numeroPermis}";
            }
        }

        var conversationId = await GetOrCreateConversation(sessionId);

        // 1. Save user message
        await SaveMessage(conversationId, "user", request.Message);

        // =========================
        // EMBEDDING
        // =========================
        var embedRes = await _http.PostAsJsonAsync(
            "http://127.0.0.1:8000/embed",
            new { text = request.Message }
        );

        if (!embedRes.IsSuccessStatusCode)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsync("Embedding service failed.");
            return;
        }

        var embedData = await embedRes.Content.ReadFromJsonAsync<EmbeddingResponse>();

        if (embedData?.Embedding == null || embedData.Embedding.Length == 0)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsync("Embedding service returned an empty embedding.");
            return;
        }

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];

        // =========================
        // RAG
        // =========================
        var ragReq = new HttpRequestMessage(
            HttpMethod.Post,
            $"{supabaseUrl}/rest/v1/rpc/match_documents"
        );

        ragReq.Headers.Add("apikey", supabaseKey);
        ragReq.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", supabaseKey);

        ragReq.Content = JsonContent.Create(new
        {
            query_embedding = embedData.Embedding,
            match_count = 5
        });

        var ragRes = await _http.SendAsync(ragReq);
        var ragJson = await ragRes.Content.ReadAsStringAsync();

        if (!ragRes.IsSuccessStatusCode)
        {
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsync("Document search failed.");
            return;
        }

        var matches = JsonSerializer.Deserialize<List<SupabaseMatch>>(ragJson) ?? new();
        var context = string.Join("\n", matches.Select(m => m.content));

        // =========================
        // CLIENT CONTEXT (If Logged In)
        // =========================
        string clientContext = "";
        if (!string.IsNullOrEmpty(numeroPermis))
        {
            var clientReq = new HttpRequestMessage(HttpMethod.Get, $"{supabaseUrl}/rest/v1/ClientRecords?NumeroPermis=eq.{numeroPermis}&select=*");
            clientReq.Headers.Add("apikey", supabaseKey);
            clientReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);
            
            var clientRes = await _http.SendAsync(clientReq);
            if (clientRes.IsSuccessStatusCode)
            {
                var clientJson = await clientRes.Content.ReadAsStringAsync();
                using var cDoc = JsonDocument.Parse(clientJson);
                if (cDoc.RootElement.GetArrayLength() > 0)
                {
                    var record = cDoc.RootElement[0];
                    clientContext = "INFORMATIONS DU CLIENT CONNECTÉ:\n";
                    foreach (var prop in record.EnumerateObject())
                    {
                        var val = prop.Value.ValueKind == System.Text.Json.JsonValueKind.Null ? "" : prop.Value.ToString();
                        clientContext += $"- {prop.Name}: {val}\n";
                    }
                    clientContext += "Utilise ces informations pour répondre de manière personnalisée si la question concerne ses contrats ou garanties.\n\n";
                }
            }
        }

        var prompt = BuildPrompt(request.Message, context, clientContext);

        // =========================
        // GROQ STREAM
        // =========================
        var groqKey = _config["Groq:ApiKey"];

        var groqRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions"
        );

        groqRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", groqKey);

        groqRequest.Content = JsonContent.Create(new
        {
            model = "llama-3.3-70b-versatile",
            stream = true,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful insurance assistant." },
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        });

        var response = await _http.SendAsync(
            groqRequest,
            HttpCompletionOption.ResponseHeadersRead
        );

        if (!response.IsSuccessStatusCode)
        {
            var openRouterKey = _config["OpenRouter:ApiKey"];
            if (!string.IsNullOrEmpty(openRouterKey))
            {
                var openRouterRequest = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
                openRouterRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openRouterKey);
                openRouterRequest.Content = JsonContent.Create(new
                {
                    model = "meta-llama/llama-3.3-70b-instruct",
                    stream = true,
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful insurance assistant." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.2
                });

                response = await _http.SendAsync(openRouterRequest, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Response.StatusCode = StatusCodes.Status502BadGateway;
                    await Response.WriteAsync($"OpenRouter fallback failed: {error}");
                    return;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Response.StatusCode = StatusCodes.Status502BadGateway;
                await Response.WriteAsync($"Groq request failed: {error}");
                return;
            }
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var fullResponse = new StringBuilder();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var data = line.Substring(6);

            if (data == "[DONE]")
                break;

            try
            {
                using var json = JsonDocument.Parse(data);

                var delta = json.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (!delta.TryGetProperty("content", out var contentElement))
                    continue;

                var token = contentElement.GetString();

                if (!string.IsNullOrEmpty(token))
                {
                    fullResponse.Append(token);

                    await Response.WriteAsync(token);
                    await Response.Body.FlushAsync();
                }
            }
            catch { }
        }

        // 2. Save assistant message
        await SaveMessage(conversationId, "assistant", fullResponse.ToString());
    }

    // =========================
    // HISTORY ENDPOINT
    // =========================
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? sessionId)
    {
        if (User.Identity is { IsAuthenticated: true })
        {
            var np = User.FindFirst("NumeroPermis")?.Value;
            if (!string.IsNullOrEmpty(np))
            {
                sessionId = $"client_{np}";
            }
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Ok(new List<object>()); // Empty history
        }

        var conversationId = await GetOrCreateConversation(sessionId);

        var supabaseUrl = _config["Supabase:Url"];
        var supabaseKey = _config["Supabase:ServiceKey"];

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{supabaseUrl}/rest/v1/messages?conversation_id=eq.{conversationId}&order=created_at.asc&select=role,content"
        );
        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", supabaseKey);

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return StatusCode(502, "Failed to load history");

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    // =========================
    // PROMPT
    // =========================
    private string BuildPrompt(string message, string context, string clientContext)
    {
        return $@"
CONTEXT:
{context}

{clientContext}

QUESTION:
{message}

Answer clearly and concisely using only the context provided. 
If the user asks about their personal info or contract, use the 'INFORMATIONS DU CLIENT CONNECTÉ' block.
If missing info, say you don't know. Reply in French.
";
    }
}

// =========================
// DTOs
// =========================
public class EmbeddingResponse
{
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class SupabaseMatch
{
    public string content { get; set; } = "";
}
