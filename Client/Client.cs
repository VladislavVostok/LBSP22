using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using Client.DTOModels;


namespace Client;

class Client
{
	private const string AuthServerIp = "127.0.0.1";
	private const int AuthServerPort = 4000;
	private const string BalancerIp = "127.0.0.1";
	private const int BalancerPort = 5000;
	private const string ChatServerIp = "127.0.0.1";
	private const int ChatServerPort = 6000;


	static async Task Main()
	{


		Console.Write("Введите логин: ");
		string username = Console.ReadLine();

		Console.Write("Введите пароль: ");
		string password = Console.ReadLine();

		string token = await GetAuthToken(username, password);

		if (string.IsNullOrEmpty(token))
		{
			Console.WriteLine("Ошибка авторизации. Завершаем работу.");
			return;
		}

		var pokerTask = ConnectToBalancer(token);
		var chatTask = ConnectToChatServer();

		await Task.WhenAll(pokerTask, chatTask);
	}

	private static async Task<string> GetAuthToken(string username, string password)
	{
		try
		{
			using TcpClient client = new TcpClient();
			await client.ConnectAsync(AuthServerIp, AuthServerPort);
			NetworkStream stream = client.GetStream();

			var loginRequest = new { Username = username, Password = password };
			string jsonRequest = JsonSerializer.Serialize(loginRequest);
			byte[] requestData = Encoding.UTF8.GetBytes(jsonRequest);

			await stream.WriteAsync(requestData, 0, requestData.Length);
			byte[] buffer = new byte[1024];
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			var response = JsonSerializer.Deserialize<AuthResponse>(responseJson);
			return response?.Success == true ? response.Token : null;

		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка подключения к серверу авторизации: {ex.Message}");
			return null;
		}
	}

	private static async Task ConnectToChatServer()
	{
		try
		{
			using (TcpClient client = new())
			{
				await client.ConnectAsync(ChatServerIp, ChatServerPort);
				NetworkStream stream = client.GetStream();
				_ = Task.Run(async () =>
				{
					byte[] buffer = new byte[1024];
					while (true)
					{
						int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
						string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
						Console.WriteLine($"Чат: {message}");
					}
				});

				while (true)
				{
					string message = Console.ReadLine();
					byte[] data = Encoding.UTF8.GetBytes(message);
					await stream.WriteAsync(data, 0, data.Length);
				}

			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{nameof(ConnectToChatServer)}: {ex.Message}");
		}
	}

	private static async Task ConnectToBalancer(string token)
	{
		try
		{
			using TcpClient client = new TcpClient();
			await client.ConnectAsync(BalancerIp, BalancerPort);
			NetworkStream stream = client.GetStream();

			// Отправляем токен балансировщику
			byte[] tokenData = Encoding.UTF8.GetBytes(token);
			await stream.WriteAsync(tokenData, 0, tokenData.Length);

			// Читаем ответ от покерного сервера через балансировщик
			byte[] buffer = new byte[1024];
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			Console.WriteLine($"Ответ от сервера: {response}");


			while (true)
			{

				string request = "DEAL_CARDS";
				byte[] requestData = Encoding.UTF8.GetBytes(request);
				await stream.WriteAsync(requestData, 0, requestData.Length);


				buffer = new byte[1024];
				bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);


				if (bytesRead > 0)
					Console.WriteLine(Encoding.UTF8.GetString(buffer));

				await Task.Delay(500);
			}

		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка подключения к балансировщику: {ex.Message}");
		}
	}


}