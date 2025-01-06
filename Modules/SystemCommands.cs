using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.IO;

namespace IA_CEREBRO.Modules;

public class SystemCommands : ModuleBase<SocketCommandContext>
{
    private readonly DiscordSocketClient _client;
    private static IMessageChannel? _lastChannel;

    public SystemCommands(DiscordSocketClient client)
    {
        _client = client;
    }

    [Command("restart")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Summary("Riavvia il bot")]
    public async Task RestartBot()
    {
        _lastChannel = Context.Channel;
        var message = await ReplyAsync("🔄 Avvio sequenza di riavvio...\nIl bot sarà di nuovo online tra 30 secondi.");
        
        // Aspetta un momento per assicurarsi che il messaggio venga inviato
        await Task.Delay(1000);

        try
        {
            // Disconnetti il client Discord
            await _client.StopAsync();
            await _client.LogoutAsync();

            // Ottieni il percorso dell'eseguibile corrente
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(currentDir, "../../.."));

            // Crea un processo batch che:
            // 1. Killa tutti i processi dotnet
            // 2. Mostra un countdown
            // 3. Riavvia il bot
            string batchContent = @$"
@echo off
taskkill /F /IM dotnet.exe
echo Riavvio in corso...
for /l %%i in (30,-1,1) do (
    cls
    echo Riavvio in corso... %%i secondi rimanenti
    timeout /t 1 /nobreak >nul
)
cd ""{projectDir}""
start /B dotnet run
exit
";
            string batchPath = Path.Combine(Path.GetTempPath(), "restart_bot.bat");
            File.WriteAllText(batchPath, batchContent);

            // Esegui il batch file
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {batchPath}",
                UseShellExecute = true,
                CreateNoWindow = false
            });
        }
        catch (Exception ex)
        {
            await ReplyAsync($"❌ Errore durante il riavvio: {ex.Message}");
        }
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
