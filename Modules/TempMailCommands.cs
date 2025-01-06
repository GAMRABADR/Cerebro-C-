using Discord;
using Discord.Commands;
using System.Net.Http;
using System.Text.Json;

namespace IA_CEREBRO.Modules;

public class TempMailCommands : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _httpClient;
    private static readonly string[] domains = { "1secmail.com", "1secmail.org", "1secmail.net" };
    private static readonly Random random = new Random();

    public TempMailCommands()
    {
        _httpClient = new HttpClient();
    }

    [Command("tempmail")]
    [Summary("Genera un indirizzo email temporaneo")]
    public async Task GenerateTempMail()
    {
        var embed = new EmbedBuilder()
            .WithTitle("🔄 Generazione email temporanea in corso...")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: embed);

        try
        {
            // Genera un nome utente casuale
            string username = GenerateRandomUsername();
            string domain = domains[random.Next(domains.Length)];
            string email = $"{username}@{domain}";

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
                    "3. Usa `!checkmail " + email + "` per vedere i messaggi ricevuti")
                .WithCurrentTimestamp()
                .Build();

            await message.ModifyAsync(msg => msg.Embed = successEmbed);
        }
        catch (Exception ex)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Errore")
                .WithDescription($"Non è stato possibile generare l'email temporanea: {ex.Message}")
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(msg => msg.Embed = errorEmbed);
        }
    }

    [Command("checkmail")]
    [Summary("Controlla i messaggi ricevuti su un'email temporanea")]
    public async Task CheckMail([Remainder] string email)
    {
        if (!email.Contains("@") || !domains.Any(d => email.EndsWith(d)))
        {
            await ReplyAsync("❌ Email non valida. Usa un'email generata con il comando `!tempmail`");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🔄 Controllo messaggi in corso...")
            .WithColor(Color.Blue)
            .Build();

        var message = await ReplyAsync(embed: embed);

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
                foreach (var msg in messages.Take(10)) // Mostra solo i primi 10 messaggi
                {
                    successEmbed.AddField(
                        $"Da: {msg.From}",
                        $"**Oggetto:** {msg.Subject}\n**Data:** {msg.Date}"
                    );
                }
            }

            await message.ModifyAsync(msg => { msg.Embed = successEmbed.Build(); });
        }
        catch (Exception ex)
        {
            var errorEmbed = new EmbedBuilder()
                .WithTitle("❌ Errore")
                .WithDescription($"Non è stato possibile controllare l'email: {ex.Message}")
                .WithColor(Color.Red)
                .Build();

            await message.ModifyAsync(msg => msg.Embed = errorEmbed);
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
