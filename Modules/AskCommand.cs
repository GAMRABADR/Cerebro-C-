using Discord;
using Discord.Commands;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;

namespace Cerebro.Modules


public class AskCommand : ModuleBase<SocketCommandContext>
{
    private static readonly HttpClient client = new HttpClient();
    private const string API_URL = "https://api-inference.huggingface.co/models/Helsinki-NLP/opus-mt-en-it";
    private static string? _token;

    // Dizionario per risposte comuni in italiano
    private static readonly Dictionary<string, string> RisposteComuni = new()
    {
        { "ciao", "Ciao! Come posso aiutarti? 😊" },
        { "come stai", "Sto molto bene, grazie! Sono qui per aiutarti! 🌟" },
        { "chi sei", "Sono Cerebro, un bot assistente creato per aiutarti! 🤖" },
        { "help", "Puoi farmi qualsiasi domanda usando il comando !ask seguito dalla tua domanda! 💬" },
        { "grazie", "Di niente! Sono sempre qui per aiutare! 😊" },
        { "buongiorno", "Buongiorno! Come posso esserti utile oggi? 🌞" },
        { "buonasera", "Buonasera! Spero di poterti aiutare! 🌙" }
    };

    static AskCommand()
    {
        _token = Environment.GetEnvironmentVariable("HUGGINGFACE_TOKEN");
        if (client.DefaultRequestHeaders.Authorization == null && _token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }
        client.Timeout = TimeSpan.FromSeconds(10);
    }

    [Command("ask")]
    [Summary("Fai una domanda al bot")]
    public async Task Ask([Remainder] string question)
    {
        try
        {
            // Prima controlliamo le risposte predefinite
            var questionLower = question.ToLower().Trim();
            foreach (var risposta in RisposteComuni)
            {
                if (questionLower.Contains(risposta.Key))
                {
                    var embed = new EmbedBuilder()
                        .WithTitle("🤖 Risposta")
                        .WithDescription(risposta.Value)
                        .WithColor(Color.Blue)
                        .WithFooter($"Richiesto da {Context.User.Username}")
                        .WithCurrentTimestamp()
                        .Build();

                    await ReplyAsync(embed: embed);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(_token))
            {
                await ReplyAsync("❌ Token HuggingFace non configurato. Controlla le variabili d'ambiente.");
                return;
            }

            var loadingMessage = await ReplyAsync("🤔 Ci sto pensando...");

            try
            {
                // Usiamo un modello di traduzione italiano
                var requestBody = new { 
                    inputs = question,
                    parameters = new { 
                        max_length = 100,
                        temperature = 0.3,  // Temperatura più bassa per risposte più precise
                        return_full_text = false
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(API_URL, content);
                var responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}");
                Console.WriteLine($"Response: {responseText}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await loadingMessage.ModifyAsync(m => m.Content = "⏳ Troppi messaggi! Aspetta 10 secondi e riprova.");
                    }
                    else
                    {
                        await loadingMessage.ModifyAsync(m => m.Content = "❌ Mi dispiace, riprova tra poco.");
                    }
                    return;
                }

                using var jsonDoc = JsonDocument.Parse(responseText);
                var generatedText = jsonDoc.RootElement[0]
                    .GetProperty("generated_text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    await loadingMessage.ModifyAsync(m => m.Content = "❌ Non ho ricevuto una risposta valida.");
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle("🤖 Risposta")
                    .WithDescription(generatedText)
                    .WithColor(Color.Blue)
                    .WithFooter($"Richiesto da {Context.User.Username}")
                    .WithCurrentTimestamp()
                    .Build();

                await loadingMessage.DeleteAsync();
                await ReplyAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella generazione: {ex.Message}");
                await loadingMessage.ModifyAsync(m => m.Content = "❌ Si è verificato un errore. Riprova tra poco.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Errore generale: {ex.Message}");
            await ReplyAsync("❌ Si è verificato un errore imprevisto.");
        }
    }
}
