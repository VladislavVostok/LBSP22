using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;

namespace WpfChatMVVM
{
	public class ChatViewModel : INotifyPropertyChanged
	{
		private string _username;
		private string _password;
		private string _message;
		private string _chatHistory;
		private TcpClient _chatClient;
		private TcpClient _authClient;
		private NetworkStream _chatStream;
		private NetworkStream _authStream;

		public string Username
		{
			get => _username;
			set { _username = value; OnPropertyChanged(nameof(Username)); }
		}

		public string Password
		{
			get => _password;
			set { _password = value; OnPropertyChanged(nameof(Password)); }
		}

		public string Message
		{
			get => _message;
			set { _message = value; OnPropertyChanged(nameof(Message)); }
		}

		public string ChatHistory
		{
			get => _chatHistory;
			set { _chatHistory = value; OnPropertyChanged(nameof(ChatHistory)); }
		}

		public ICommand LoginCommand { get; }
		public ICommand SendMessageCommand { get; }

		public ChatViewModel()
		{
			LoginCommand = new RelayCommand(async () => await Login());
			SendMessageCommand = new RelayCommand(async () => await SendMessage());
		}

		private async Task Login()
		{
			try
			{
				_authClient = new TcpClient("127.0.0.1", 4000);
				_authStream = _authClient.GetStream();

				string credentials = $"{{\"Username\":\"{Username}\",\"Password\":\"{Password}\"}}";
				byte[] data = Encoding.UTF8.GetBytes(credentials);
				await _authStream.WriteAsync(data, 0, data.Length);

				byte[] buffer = new byte[1024];
				int bytesRead = await _authStream.ReadAsync(buffer, 0, buffer.Length);
				string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				if (response.Contains("success:true"))
				{
					ChatHistory = "Login successful! Connecting to chat...\n";
					await ConnectToChat();
				}
				else
				{
					ChatHistory = "Login failed!\n";
				}
			}
			catch (Exception ex)
			{
				ChatHistory = $"Login error: {ex.Message}\n";
			}
		}

		private async Task ConnectToChat()
		{
			_chatClient = new TcpClient("127.0.0.1", 6000);
			_chatStream = _chatClient.GetStream();
			ChatHistory += "Connected to chat server.\n";
		}

		private async Task SendMessage()
		{
			if (_chatClient == null || !_chatClient.Connected) return;

			byte[] data = Encoding.UTF8.GetBytes(Message);
			await _chatStream.WriteAsync(data, 0, data.Length);

			byte[] buffer = new byte[1024];
			int bytesRead = await _chatStream.ReadAsync(buffer, 0, buffer.Length);
			ChatHistory += $"{Encoding.UTF8.GetString(buffer, 0, bytesRead)}\n";
			Message = string.Empty;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class RelayCommand : ICommand
	{
		private readonly Func<Task> _execute;
		private readonly Func<bool> _canExecute;

		public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
		public async void Execute(object parameter) => await _execute();

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}
	}
}