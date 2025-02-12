using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Server;

class Server
{

	private static readonly string[] Cards = {
		"2H", "3H", "4H", "5H", "6H", "7H", "8H", "9H", "10H", "JH", "QH", "KH", "AH",
		"2D", "3D", "4D", "5D", "6D", "7D", "8D", "9D", "10D", "JD", "QD", "KD", "AD",
		"2C", "3C", "4C", "5C", "6C", "7C", "8C", "9C", "10C", "JC", "QC", "KC", "AC",
		"2S", "3S", "4S", "5S", "6S", "7S", "8S", "9S", "10S", "JS", "QS", "KS", "AS"
	};

	private static readonly Random Random = new Random();

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
		try
		{
            using (client)
            {
				NetworkStream stream = client.GetStream();
				while (true){ 
					

					byte[] buffer = new byte[1024];
					int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
					string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

					Console.WriteLine($"Получен запрос: {DateTime.Now.ToString()} {client.Client.RemoteEndPoint}");

					if (request.Trim() == "DEAL_CARDS")
					{
						string response = DealCards();
						byte[] responseData = Encoding.UTF8.GetBytes(response);
						await stream.WriteAsync(responseData, 0, responseData.Length);
					}
				}
			}
           
        }
        catch (Exception e){
            Console.WriteLine($"{e.Message} - {e.InnerException}");
        }
        finally{
            client.Dispose();
        }
    }

	private static string DealCards()
	{
		var cards = new HashSet<string>();

		while (cards.Count < 2)
		{
			cards.Add(Cards[Random.Next(Cards.Length)]);
		}
		return string.Join(", ", cards);
	}
}
