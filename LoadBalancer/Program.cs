using System.Net;
using System.Net.Sockets;

namespace LoadBalancer;

class Program
{
    private static readonly List<IPEndPoint> _server = new(){
        new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001),
        new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002),
        new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003)
    };

    private static readonly object _lock = new object();
    private static int _currentServerIndex = 0;

    static async Task Main(string[] args)
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();

        Console.WriteLine("Балансировочный сервер запущен на порту 5000");


        while(true){
            TcpClient client = await listener.AcceptTcpClientAsync();

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }


    private static async Task HandleClientAsync(TcpClient client){
        
        IPEndPoint selectedServer = GetNextServer();
       
       if(!await IsServerAvailableAsync(selectedServer)){
            Console.WriteLine($"Сервер {selectedServer} недоступен. Закрываем соединение с клиентом...");
            client.Dispose();
       }

        Console.WriteLine($"{client.Client.RemoteEndPoint}");

        using (TcpClient server = new TcpClient()){

            try{

                await server.ConnectAsync(selectedServer);

                await Task.WhenAll(
                    RedirectDataAsync(client.GetStream(), server.GetStream()),
                    RedirectDataAsync(server.GetStream(), client.GetStream())

                );

            }
            catch (Exception e){
                Console.WriteLine($"{e.Message} - {e.InnerException}");
            }
            finally{
                client.Dispose();
            }
        }
    }

    private static async Task RedirectDataAsync(NetworkStream from, NetworkStream to){
        byte[] buffer = new byte[1024];
        int bytesRead;

        try
        {
            while ((bytesRead = await from.ReadAsync(buffer, 0, buffer.Length)) > 0){
                await to.WriteAsync(buffer, 0, bytesRead);
            }
        }
        catch (Exception ex)
        {
            
            Console.WriteLine($"Ошибка перенаправления данных: {ex.Message}");
        }
        finally{
            await from.DisposeAsync();
            await to.DisposeAsync();
        }
    }

    private static async Task<bool> IsServerAvailableAsync( IPEndPoint selectedServer){
        try
        {
            using (var pingClient = new TcpClient()){
                await pingClient.ConnectAsync(selectedServer.Address, selectedServer.Port);
            }
            
            return true;
        }
        catch
        {
            return false;            
        }
    }

    private static IPEndPoint GetNextServer(){
        lock(_lock){
            IPEndPoint server = _server[_currentServerIndex];
            _currentServerIndex = (_currentServerIndex + 1) % _server.Count;
            return server;
        }
    }

}
