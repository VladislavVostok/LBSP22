using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		private const string BalancerIp = "62.113.44.183";
		private const int BalancerPort = 5000;
		private const string ChatServerIp = "62.113.44.183";
		private const int ChatServerPort = 6000;

		private TcpClient _chatClient;
		private NetworkStream _chatStream;

		public MainWindow()
		{
			InitializeComponent();

			// Открываем окно авторизации
			var authWindow = new AuthWindow();
			if (authWindow.ShowDialog() == true)
			{
				// Авторизация прошла успешно
				InitializeClient(authWindow.Username, authWindow.Password);
			}
			else
			{
				Close(); // Закрываем приложение, если авторизация не удалась
			}

		}

		private async void InitializeClient(string username, string password)
		{
			string token = await GetAuthToken(username, password);

			if (string.IsNullOrEmpty(token))
			{
				MessageBox.Show("Ошибка авторизации. Завершаем работу.");
				Close();
				return;
			}



			// Подключаемся к балансировщику
			var conTBal = ConnectToBalancer(token);

			// Подключаемся к чат-серверу
			var conTChSer = ConnectToChatServer();



			await Task.WhenAll(conTBal, conTChSer);


		}

		private async Task<string> GetAuthToken(string username, string password)
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync("127.0.0.1", 4000);
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
				MessageBox.Show($"Ошибка подключения к серверу авторизации: {ex.Message}");
				return null;
			}
		}

		private async Task ConnectToBalancer(string token)
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(BalancerIp, BalancerPort);
				NetworkStream stream = client.GetStream();

				// Отправляем токен балансировщику
				byte[] tokenData = Encoding.UTF8.GetBytes(token);
				await stream.WriteAsync(tokenData, 0, tokenData.Length);

				// Читаем ответ от сервера
				byte[] buffer = new byte[1024];
				int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				MessageBox.Show($"Ответ от сервера: {response}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка подключения к балансировщику: {ex.Message}");
			}
		}


		private async Task ConnectToChatServer()
		{
			try
			{
				_chatClient = new TcpClient();
				await _chatClient.ConnectAsync(ChatServerIp, ChatServerPort);
				_chatStream = _chatClient.GetStream();

				// Запускаем чтение сообщений из чата
				_ = Task.Run(ReceiveChatMessages);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка подключения к чат-серверу: {ex.Message}");
			}
		}
		private async Task ReceiveChatMessages()
		{
			byte[] buffer = new byte[1024];
			while (true)
			{
				int bytesRead = await _chatStream.ReadAsync(buffer, 0, buffer.Length);
				string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				// Добавляем сообщение в лог чата
				Dispatcher.Invoke(() => ChatLogBox.Items.Add(message));
			}
		}

		private async void SendChatMessage_Click(object sender, RoutedEventArgs e)
		{
			string message = ChatInputBox.Text;
			if (!string.IsNullOrEmpty(message))
			{
				byte[] data = Encoding.UTF8.GetBytes(message);
				await _chatStream.WriteAsync(data, 0, data.Length);
				ChatInputBox.Clear();
			}
		}

		private async void DealCards_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				using TcpClient client = new TcpClient();
				await client.ConnectAsync(BalancerIp, BalancerPort);
				NetworkStream stream = client.GetStream();

				byte[] requestData = Encoding.UTF8.GetBytes("DEAL_CARDS");
				await stream.WriteAsync(requestData, 0, requestData.Length);

				byte[] buffer = new byte[1024];
				int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				MessageBox.Show($"Карты: {response}");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
	}

	public class AuthResponse
	{
		public bool Success { get; set; }
		public string Token { get; set; }
	}
}