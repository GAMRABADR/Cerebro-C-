using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;

namespace IA_CEREBRO.Modules;

public class SystemCommands : ModuleBase<SocketCommandContext>
{
    private readonly DiscordSocketClient _client;

    public SystemCommands(DiscordSocketClient client)
    {
        _client = client;
    }

    [Command("shutdown")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Spegne il bot")]
    public async Task ShutdownBot()
    {
        await ReplyAsync("👋 Spegnimento in corso...");
        
        try
        {
            // Disconnetti il client Discord
            await _client.StopAsync();
            await _client.LogoutAsync();
            
            // Aspetta un momento per assicurarsi che la disconnessione sia completata
            await Task.Delay(2000);

            // Crea un batch file per killare tutti i processi dotnet e poi eliminare se stesso
            string batchContent = @"
@echo off
taskkill /F /IM dotnet.exe
timeout /t 1 /nobreak
(goto) 2>nul & del ""%~f0""
";
            string batchPath = Path.Combine(Path.GetTempPath(), "shutdown_bot.bat");
            File.WriteAllText(batchPath, batchContent);

            // Esegui il batch file
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start /B {batchPath}",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            await ReplyAsync($"❌ Errore durante lo spegnimento: {ex.Message}");
            
            // In caso di errore, prova a terminare direttamente il processo
            Environment.Exit(0);
        }
    }
}
