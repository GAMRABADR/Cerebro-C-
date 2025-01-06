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
            .WithColor(Color.Blue)
            .WithDescription("Ecco i comandi che puoi utilizzare:");

        var modules = _commands.Modules.GroupBy(m => m.Name);
        
        foreach (var module in modules)
        {
            var moduleBuilder = new StringBuilder();
            bool hasCommands = false;

            foreach (var command in module.First().Commands)
            {
                bool canShow = false;
                var preconditions = command.Preconditions
                    .Concat(command.Module.Preconditions)
                    .ToList();

                // Controlla se l'utente può usare il comando
                if (preconditions.Any())
                {
                    var result = await command.CheckPreconditionsAsync(Context, _services);
                    canShow = result.IsSuccess;
                }
                else
                {
                    canShow = true; // Se non ci sono precondizioni, tutti possono vedere il comando
                }

                // Override per staff
                if (isStaff)
                {
                    canShow = true;
                }

                if (canShow)
                {
                    hasCommands = true;
                    var parameters = command.Parameters.Select(p => $"[{p.Name}]");
                    var paramString = parameters.Any() ? " " + string.Join(" ", parameters) : "";
                    
                    moduleBuilder.AppendLine($"▫️ `!{command.Name}{paramString}`");
                    if (!string.IsNullOrEmpty(command.Summary))
                        moduleBuilder.AppendLine($"  *{command.Summary}*");
                }
            }

            if (hasCommands)
            {
                builder.AddField(module.Key ?? "Generale", moduleBuilder.ToString());
            }
        }

        // Aggiungi una nota per gli admin/mod
        if (isStaff)
        {
            builder.WithFooter("👑 Hai accesso a tutti i comandi come membro dello staff");
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
