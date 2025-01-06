using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace IA_CEREBRO.Modules;

public class SystemCommands : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;

    public SystemCommands(CommandService commands)
    {
        _commands = commands;
    }

    [Command("help")]
    [Summary("Mostra la lista dei comandi disponibili")]
    public async Task Help()
    {
        var embed = new EmbedBuilder()
            .WithTitle("📋 Comandi Disponibili")
            .WithColor(Color.Blue);

        // Comandi Generali
        var generalCommands = new StringBuilder();
        generalCommands.AppendLine("**!help** - Mostra questo messaggio");
        generalCommands.AppendLine("**!tempmail** - Genera una email temporanea");
        generalCommands.AppendLine("**!checkmail** - Controlla i messaggi della email temporanea");
        generalCommands.AppendLine("**!deletetemp** - Elimina la email temporanea attiva");
        embed.AddField("📌 Comandi Generali", generalCommands.ToString());

        // Comandi Moderazione
        var modCommands = new StringBuilder();
        modCommands.AppendLine("**!join** - Fa entrare il bot nel tuo canale vocale");
        modCommands.AppendLine("**!leave** - Fa uscire il bot dal canale vocale");
        modCommands.AppendLine("**!kick @utente** - Espelle un utente dal server");
        modCommands.AppendLine("**!ban @utente** - Bandisce un utente dal server");
        modCommands.AppendLine("**!clear [numero]** - Elimina un numero specifico di messaggi");
        modCommands.AppendLine("**!mute @utente** - Silenzia un utente");
        modCommands.AppendLine("**!unmute @utente** - Rimuove il silenziamento da un utente");
        embed.AddField("🛡️ Comandi Moderazione", modCommands.ToString());

        // Comandi Admin
        var adminCommands = new StringBuilder();
        adminCommands.AppendLine("**!join [nomecanale]** - Fa entrare il bot in un canale vocale specifico");
        adminCommands.AppendLine("**!autojoin** - Attiva/disattiva l'auto-join nei canali vocali");
        adminCommands.AppendLine("**!shutdown** - Spegne il bot");
        adminCommands.AppendLine("**!restart** - Riavvia il bot");
        adminCommands.AppendLine("**!setlogchannel #canale** - Imposta il canale per i log");
        adminCommands.AppendLine("**!setwelcome #canale** - Imposta il canale di benvenuto");
        embed.AddField("⚡ Comandi Amministrazione", adminCommands.ToString());

        await ReplyAsync(embed: embed.Build());
    }

    [Command("shutdown")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Spegne il bot (Solo Admin)")]
    public async Task Shutdown()
    {
        await ReplyAsync("⚡ Spegnimento in corso...");
        Environment.Exit(0);
    }

    [Command("restart")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Riavvia il bot (Solo Admin)")]
    public async Task Restart()
    {
        await ReplyAsync("⚡ Riavvio in corso...");
        var fileName = Path.Combine(Path.GetTempPath(), "restart_bot.bat");
        File.WriteAllText(fileName, "@echo off\r\ntimeout /t 1\r\ndotnet run --project \"" + 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\"");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,
            CreateNoWindow = true
        });
        
        Environment.Exit(0);
    }
}
