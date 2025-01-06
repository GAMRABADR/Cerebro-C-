using Discord;
using Discord.Commands;
using IA_CEREBRO.Helpers;
using System.Text;

namespace IA_CEREBRO.Modules;

public class GameNewsCommands : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<string, string> CategoryEmojis = new()
    {
        { "pvp", "⚔️" },
        { "rpg", "🎭" },
        { "fps", "🎯" },
        { "strategy", "🎲" },
        { "mmo", "🌍" },
        { "survivor", "🏕️" }
    };

    [Command("news")]
    [Summary("Mostra le ultime notizie sui videogiochi per una categoria specifica")]
    public async Task GetGameNews([Remainder] string category = "")
    {
        // Invia un messaggio iniziale di caricamento
        var loadingMsg = await ReplyAsync("🔍 Ricerca delle ultime notizie in corso...");

        try
        {
            var news = await GameNewsHelper.GetGameNews(category);

            if (!news.Any())
            {
                await loadingMsg.ModifyAsync(msg => msg.Content = "❌ Nessuna notizia trovata per questa categoria!");
                return;
            }

            var emoji = CategoryEmojis.GetValueOrDefault(category.ToLower(), "🎮");
            
            // Creiamo più embed per gestire tutte le notizie
            var embeds = new List<EmbedBuilder>();
            var currentEmbed = new EmbedBuilder()
                .WithTitle($"{emoji} Ultime Notizie Gaming {(string.IsNullOrEmpty(category) ? "" : $"- {category.ToUpper()}")}")
                .WithColor(Color.Blue)
                .WithFooter(footer => 
                {
                    footer.Text = $"Richiesto da {Context.User.Username} • Pagina 1";
                })
                .WithCurrentTimestamp();

            var description = new StringBuilder();
            var newsCount = 0;
            var pageCount = 1;

            foreach (var item in news)
            {
                newsCount++;
                description.AppendLine($"**{newsCount}.** {item.Title}");
                description.AppendLine($"🔗 {item.Url}");
                description.AppendLine($"📰 **Fonte:** {item.Source} • 📅 {item.Date:dd/MM/yyyy}");
                description.AppendLine();

                // Ogni 10 notizie, creiamo un nuovo embed
                if (newsCount % 10 == 0 && newsCount < news.Count)
                {
                    currentEmbed.WithDescription(description.ToString());
                    embeds.Add(currentEmbed);

                    pageCount++;
                    description.Clear();
                    currentEmbed = new EmbedBuilder()
                        .WithTitle($"{emoji} Ultime Notizie Gaming {(string.IsNullOrEmpty(category) ? "" : $"- {category.ToUpper()}")}")
                        .WithColor(Color.Blue)
                        .WithFooter(footer => 
                        {
                            footer.Text = $"Richiesto da {Context.User.Username} • Pagina {pageCount}";
                        })
                        .WithCurrentTimestamp();
                }
            }

            // Aggiungiamo l'ultimo embed se contiene notizie
            if (description.Length > 0)
            {
                currentEmbed.WithDescription(description.ToString());
                embeds.Add(currentEmbed);
            }

            // Inviamo tutti gli embed
            await loadingMsg.DeleteAsync();
            foreach (var embed in embeds)
            {
                await ReplyAsync(embed: embed.Build());
                await Task.Delay(1000); // Piccolo delay per evitare il rate limit
            }
        }
        catch (Exception ex)
        {
            await loadingMsg.ModifyAsync(msg => 
                msg.Content = "❌ Si è verificato un errore durante il recupero delle notizie. Riprova più tardi.");
            Console.WriteLine($"Errore nel comando news: {ex}");
        }
    }

    [Command("newshelp")]
    [Summary("Mostra le categorie disponibili per il comando news")]
    public async Task ShowNewsHelp()
    {
        var embed = new EmbedBuilder()
            .WithTitle("📰 Categorie Disponibili per le News")
            .WithColor(Color.Blue)
            .WithDescription("Usa il comando `!news [categoria]` per vedere le ultime notizie.\n" +
                           "Se non specifichi una categoria, verranno mostrate tutte le notizie.\n\n" +
                           "**Categorie disponibili:**\n" +
                           "⚔️ `pvp` - Giochi PvP e Multiplayer Competitivi\n" +
                           "🎭 `rpg` - Giochi di Ruolo e JRPG\n" +
                           "🎯 `fps` - Sparatutto in Prima Persona\n" +
                           "🎲 `strategy` - Giochi Strategici e Tattici\n" +
                           "🌍 `mmo` - MMO e Giochi Online\n" +
                           "🏕️ `survivor` - Giochi di Sopravvivenza e Crafting\n")
            .WithFooter(footer => 
            {
                footer.Text = $"Richiesto da {Context.User.Username}";
            })
            .WithCurrentTimestamp();

        await ReplyAsync(embed: embed.Build());
    }
}
