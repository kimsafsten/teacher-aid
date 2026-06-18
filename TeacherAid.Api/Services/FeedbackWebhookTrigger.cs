namespace TeacherAid.Api.Services;

/// <summary>
/// Sends feedback-generation payloads to the n8n webhook without blocking the caller.
/// </summary>
public static class FeedbackWebhookTrigger
{
    /// <summary>
    /// Posts <paramref name="payload"/> to n8n in the background. Errors are not propagated.
    /// </summary>
    public static void Send(IHttpClientFactory http, IConfiguration config, object payload)
    {
        var n8nUrl = config["N8n:WebhookUrl"] ?? "http://127.0.0.1:5678/webhook/feedback";
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = http.CreateClient("ollama");
                await client.PostAsJsonAsync(n8nUrl, payload);
            }
            catch { }
        });
    }
}
