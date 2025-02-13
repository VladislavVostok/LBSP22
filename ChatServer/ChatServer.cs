using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
	internal class ChatServer
	{

		private static readonly List<TcpClient> _clients = new List<TcpClient>();
		static async Task Main(string[] args)
		{
			int port = 6000; // Порт для чат-сервера
			TcpListener listener = new TcpListener(IPAddress.Any, port);
			listener.Start();

			Console.WriteLine($"Chat Server запущен на порту {port}");

			while (true)
			{
				TcpClient client = await listener.AcceptTcpClientAsync();
				_clients.Add(client);
				_ = Task.Run(() => HandleClientAsync(client));
			}

		}

		private static async Task HandleClientAsync(TcpClient client)
		{
			using (client)
			{
				NetworkStream stream = client.GetStream();
				byte[] buffer = new byte[1024];

				while (true)
				{
					try
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

						if (bytesRead == 0) break;
						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						Console.WriteLine($"{DateTime.Now.ToString()}Получено сообщение: {message}");

						await BroadcastMessageAsync(message);
					}
					catch (Exception ex)
					{
						_clients.Remove(client);
						Console.WriteLine($"{nameof(HandleClientAsync)}: {ex.Message} - {ex.InnerException}");
					}
				}

				_clients.Remove(client);
			}
		}

		private static async Task BroadcastMessageAsync(string message)
		{
			byte[] data = Encoding.UTF8.GetBytes(message);

			foreach (var client in _clients)
			{
				try
				{
					await client.GetStream().WriteAsync(data, 0, data.Length);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"{nameof(BroadcastMessageAsync)}: {ex.Message}");
				}
			}
		}
	}
}
