using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;

namespace IA_CEREBRO.Modules;

public class HelpCommand : ModuleBase<SocketCommandContext>
{
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;

    public HelpCommand(CommandService commands, IServiceProvider services)
    {
        _commands = commands;
        _services = services;
    }

    [Command("help")]
    public async Task Help()
    {
        var user = Context.User as SocketGuildUser;
        var isAdmin = user.GuildPermissions.Administrator;
        var isModerator = user.Roles.Any(r => r.Name.ToLower() == "moderator");
        var isStaff = isAdmin || isModerator;

        var builder = new EmbedBuilder()
            .WithTitle("📚 Comandi Disponibili")
            .WithColor(Color.Blue);

        var embedFields = new List<EmbedFieldBuilder>();

        // Comandi generali (visibili a tutti)
        var generalCommands = new StringBuilder();
        generalCommands.AppendLine("`!help` - Mostra questo messaggio");
        generalCommands.AppendLine("`!tempmail` - Genera una email temporanea");
        generalCommands.AppendLine("`!checkmail` - Controlla i messaggi dell'email temporanea");
        generalCommands.AppendLine("`!deletetemp` - Elimina l'email temporanea");
        embedFields.Add(new EmbedFieldBuilder()
            .WithName("📌 Comandi Generali")
            .WithValue(generalCommands.ToString())
            .WithIsInline(false));

        // Comandi di moderazione (visibili solo a moderatori e admin)
        if (isStaff)
        {
            var moderationCommands = new StringBuilder();
            moderationCommands.AppendLine("`!join` - Fa entrare il bot nel tuo canale vocale");
            moderationCommands.AppendLine("`!leave` - Fa uscire il bot dal canale vocale");
            moderationCommands.AppendLine("`!kick @utente [motivo]` - Espelle un utente dal server");
            moderationCommands.AppendLine("`!ban @utente [motivo]` - Bandisce un utente dal server");
            moderationCommands.AppendLine("`!clear [numero]` - Elimina un numero specifico di messaggi");
            moderationCommands.AppendLine("`!mute @utente [durata]` - Silenzia un utente");
            moderationCommands.AppendLine("`!unmute @utente` - Rimuove il silenziamento da un utente");
            embedFields.Add(new EmbedFieldBuilder()
                .WithName("🛡️ Comandi di Moderazione")
                .WithValue(moderationCommands.ToString())
                .WithIsInline(false));
        }

        // Comandi di amministrazione (visibili solo agli admin)
        if (isAdmin)
        {
            var adminCommands = new StringBuilder();
            adminCommands.AppendLine("`!autojoin` - Attiva/disattiva l'auto-join nei canali vocali");
            adminCommands.AppendLine("`!join [nomecanale]` - Fa entrare il bot in un canale specifico");
            adminCommands.AppendLine("`!restart` - Riavvia il bot");
            adminCommands.AppendLine("`!shutdown` - Spegne il bot");
            adminCommands.AppendLine("`!setwelcome #canale` - Imposta il canale di benvenuto");
            adminCommands.AppendLine("`!setlogchannel #canale` - Imposta il canale per i log");
            embedFields.Add(new EmbedFieldBuilder()
                .WithName("⚡ Comandi di Amministrazione")
                .WithValue(adminCommands.ToString())
                .WithIsInline(false));
        }

        foreach (var field in embedFields)
        {
            builder.AddField(field);
        }

        await ReplyAsync(embed: builder.Build());
    }

    [Command("help")]
    public async Task Help(string commandName)
    {
        var user = Context.User as SocketGuildUser;
        var isAdmin = user.GuildPermissions.Administrator;
        var isModerator = user.Roles.Any(r => r.Name.ToLower() == "moderator");
        var isStaff = isAdmin || isModerator;

        var result = _commands.Search(Context, commandName);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Non ho trovato nessun comando chiamato **{commandName}**");
            return;
        }

        var builder = new EmbedBuilder()
            .WithTitle($"ℹ️ Comando: {commandName}")
            .WithColor(Color.Blue);

        foreach (var command in result.Commands)
        {
            var cmd = command.Command;

            // Controlla se l'utente può vedere questo comando
            bool canShow = false;
            if (isStaff)
            {
                canShow = true;
            }
            else
            {
                var preconditions = cmd.Preconditions.Concat(cmd.Module.Preconditions).ToList();
                if (preconditions.Any())
                {
                    var preconditionResult = await cmd.CheckPreconditionsAsync(Context, _services);
                    canShow = preconditionResult.IsSuccess;
                }
                else
                {
                    canShow = true;
                }
            }

            if (canShow)
            {
                var parameters = cmd.Parameters.Select(p => $"[{p.Name}]");
                var paramString = parameters.Any() ? " " + string.Join(" ", parameters) : "";

                builder.WithDescription($"**Uso:** `!{cmd.Name}{paramString}`\n" +
                                     $"**Modulo:** {cmd.Module.Name}\n" +
                                     $"{(cmd.Summary ?? "Nessuna descrizione disponibile")}");

                if (cmd.Aliases.Any())
                    builder.AddField("Alias", string.Join(", ", cmd.Aliases.Select(a => $"`{a}`")));

                if (cmd.Parameters.Any())
                {
                    var parameterDescriptions = cmd.Parameters
                        .Select(p => $"`{p.Name}` - {(p.Summary ?? "Nessuna descrizione")}");
                    builder.AddField("Parametri", string.Join("\n", parameterDescriptions));
                }
            }
        }

        if (string.IsNullOrEmpty(builder.Description))
        {
            await ReplyAsync("Non hai i permessi per vedere questo comando.");
            return;
        }

        await ReplyAsync(embed: builder.Build());
    }
}
