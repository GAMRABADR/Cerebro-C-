using Discord;
using Discord.Commands;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Cerebro.Modules

public class TempMailCommands : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _httpClient;
    private static readonly string[] domains = { "1secmail.com", "1secmail.org", "1secmail.net" };
    private static readonly Random random = new Random();
    private static readonly ConcurrentDictionary<ulong, string> userEmails = new ConcurrentDictionary<ulong, string>();

    public TempMailCommands()
    {
        _httpClient = new HttpClient();
    }

    [Command("tempmail")]
    [Summary("Genera un indirizzo email temporaneo")]
    public async Task GenerateTempMail()
    {
        var userId = Context.User.Id;
        
        if (userEmails.ContainsKey(userId))
        {
            await ReplyAsync("❌ Hai già un'email temporanea attiva. Usa `!deletetemp` per eliminarla prima di generarne una nuova.");
            return;
        }

        var loadingEmbed = new EmbedBuilder()
            .WithTitle("🔄 Generazione email temporanea in corso...")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: loadingEmbed);

        try
        {
            string username = GenerateRandomUsername();
            string domain = domains[random.Next(domains.Length)];
            string email = $"{username}@{domain}";

            if (userEmails.TryAdd(userId, email))
            {
                var successEmbed = new EmbedBuilder()
                    .WithTitle("📧 Email Temporanea Generata")
                    .WithDescription($"**Email:** `{email}`")
                    .WithColor(Color.Green)
                    .WithFooter(footer => {
                        footer.Text = "Questa email sarà valida per 24 ore";
                    })
                    .AddField("Come usare", 
                        "1. Copia l'indirizzo email\n" +
                        "2. Usalo dove hai bisogno\n" +
                        "3. Usa `!checkmail` per vedere i messaggi ricevuti\n" +
                        "4. Usa `!deletetemp` quando hai finito")
                    .WithCurrentTimestamp()
                    .Build();

                await message.ModifyAsync(m => { m.Embed = successEmbed; });
            }
        }
        catch (Exception ex)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Errore")
                .WithDescription($"Non è stato possibile generare l'email temporanea: {ex.Message}")
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(m => { m.Embed = errorEmbed; });
        }
    }

    [Command("checkmail")]
    [Summary("Controlla i messaggi ricevuti sulla tua email temporanea")]
    public async Task CheckMail()
    {
        var userId = Context.User.Id;
        
        if (!userEmails.TryGetValue(userId, out string? email))
        {
            await ReplyAsync("❌ Non hai un'email temporanea attiva. Usa `!tempmail` per generarne una.");
            return;
        }

        if (email == null)
        {
            await ReplyAsync("❌ Si è verificato un errore con l'email temporanea. Prova a generarne una nuova.");
            return;
        }

        var loadingEmbed = new EmbedBuilder()
            .WithTitle("🔄 Controllo messaggi in corso...")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: loadingEmbed);

        try
        {
            string[] emailParts = email.Split('@');
            string login = emailParts[0];
            string domain = emailParts[1];

            var response = await _httpClient.GetAsync($"https://www.1secmail.com/api/v1/?action=getMessages&login={login}&domain={domain}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var messages = JsonSerializer.Deserialize<List<EmailMessage>>(content);

            var successEmbed = new EmbedBuilder()
                .WithTitle("📬 Messaggi Ricevuti")
                .WithColor(Color.Green)
                .WithFooter(footer => {
                    footer.Text = $"Email: {email}";
                })
                .WithCurrentTimestamp();

            if (messages == null || !messages.Any())
            {
                successEmbed.WithDescription("📭 Nessun messaggio ricevuto");
            }
            else
            {
                successEmbed.WithDescription($"📨 {messages.Count} messaggi ricevuti:");
                foreach (var msg in messages.Take(10))
                {
                    successEmbed.AddField(
                        $"Da: {msg.From}",
                        $"**Oggetto:** {msg.Subject}\n**Data:** {msg.Date}"
                    );
                }
            }

            await message.ModifyAsync(m => { m.Embed = successEmbed.Build(); });
        }
        catch (Exception ex)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Errore")
                .WithDescription($"Non è stato possibile controllare l'email: {ex.Message}")
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(m => { m.Embed = errorEmbed; });
        }
    }

    [Command("deletetemp")]
    [Summary("Elimina la tua email temporanea attiva")]
    public async Task DeleteTempMail()
    {
        var userId = Context.User.Id;
        
        if (userEmails.TryRemove(userId, out string? email))
        {
            if (email == null)
            {
                await ReplyAsync("❌ Si è verificato un errore durante l'eliminazione dell'email temporanea.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🗑️ Email Temporanea Eliminata")
                .WithDescription($"L'email `{email}` è stata eliminata con successo.")
                .WithColor(Color.Green)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: embed);
        }
        else
        {
            await ReplyAsync("❌ Non hai un'email temporanea attiva da eliminare.");
        }
    }

    private string GenerateRandomUsername()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}

public class EmailMessage
{
    public int Id { get; set; }
    public string From { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Date { get; set; } = "";
}
