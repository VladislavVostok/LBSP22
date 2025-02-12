using System.Net.Sockets;
using System.Text;

namespace Client;

class Program
{
    static async Task Main()
    {
       using (TcpClient client = new TcpClient()){

            await client.ConnectAsync("127.0.0.1", 5000);
            NetworkStream stream = client.GetStream();
            int i = 1;
            while(true){

                byte[] message = Encoding.UTF8.GetBytes($"Hello, Server {i}");
                await stream.WriteAsync(message, 0, message.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                    Console.WriteLine(Encoding.UTF8.GetString(buffer));
	            i++;
                await Task.Delay(500);
            }                                
       }
    }
}