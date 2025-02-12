using System.Net;
using System.Net.Sockets;


namespace Server;

class Program
{
    static async Task Main(string[] args)
    {
        int port = (args.Length == 0) ? 5001 : int.Parse(args[0]);

        TcpListener listener = new TcpListener(IPAddress.Any, port);

        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        listener.Start();

        Console.WriteLine($"Сервер запущен на порту {port}...");

        while(true){
            TcpClient client = await listener.AcceptTcpClientAsync();

            _ = Task.Run(() => HandleClientAsync(client));
        }

    }

    private static async Task HandleClientAsync(TcpClient client){
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];

        int byteRead;

        try{
            while ((byteRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0){

                Console.WriteLine($"{client.Client.RemoteEndPoint}");
                await stream.WriteAsync(buffer, 0, byteRead);
            }
        }
        catch (Exception e){
            Console.WriteLine($"{e.Message} - {e.InnerException}");
        }
        finally{
            client.Dispose();
        }
    }
}
