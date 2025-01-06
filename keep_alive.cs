using System.Net;

namespace IA_CEREBRO;

public class KeepAlive
{
    private HttpListener _listener;
    private Thread _serverThread;

    public KeepAlive()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:8080/");
        _serverThread = new Thread(StartServer);
    }

    public void Start()
    {
        _serverThread.Start();
    }

    private void StartServer()
    {
        try
        {
            _listener.Start();
            Console.WriteLine("Server started on port 8080");

            while (true)
            {
                var context = _listener.GetContext();
                var response = context.Response;

                string responseString = "Bot is alive!";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
    }
}
