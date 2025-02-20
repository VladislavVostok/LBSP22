using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace LoadBalancer;

class LoadBalancer
{
	private static readonly List<IPEndPoint> _server = new(){
		new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001),
		new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002),
		new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003)
	};

	private static readonly object _lock = new object();
	private static int _currentServerIndex = 0;
	private static readonly string _secretKey = "SuperSecretKijdfgjaeoiyj34o9uyjhuwierfjhoiqejy0ju5490hjueoifrhoijaasdeyForJWTToken123!";

	static async Task Main(string[] args)
	{

		var certificate = new X509Certificate2("serverCert.pfx", "qwerty123");


		TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 5000);
		listener.Start();

		Console.WriteLine("Балансировочный сервер запущен на порту 5000");


		while (true)
		{
			TcpClient client = await listener.AcceptTcpClientAsync();

			_ = Task.Run(() => HandleClientAsync(client, certificate));
		}
	}


	private static async Task HandleClientAsync(TcpClient client, X509Certificate2 certificate)
	{


		using (client)
		{

			SslStream sslStreamClient = new SslStream(client.GetStream(), false,
					(sender, cert, chain, sslPolicyErrors) => true
			);

			try
			{
				// Аутентификация как сервер (LoadBalancer)
				sslStreamClient.AuthenticateAsServer(
					certificate,
					clientCertificateRequired: false,
					enabledSslProtocols: SslProtocols.Tls12,
					checkCertificateRevocation: false
				);



				byte[] buffer = new byte[1024];
				int bytesRead = await sslStreamClient.ReadAsync(buffer, 0, buffer.Length);

				if (bytesRead == 0)
				{
					Console.WriteLine("[LoadBalancer] Клиент отключился, не прислав токен");
					return;
				}

				string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

				if (!ValidateJwtToken(request))
				{
					Console.WriteLine("Неверный JWT токен. Отклоняем соединение.");
					client.Close();
					return;
				}

				byte[] response = Encoding.UTF8.GetBytes("OK");
				await sslStreamClient.WriteAsync(response, 0, response.Length);

				IPEndPoint selectedServer = GetNextServer();

				if (!await IsServerAvailableAsync(selectedServer))
				{
					Console.WriteLine($"Сервер {selectedServer} недоступен. Закрываем соединение с клиентом...");
					client.Dispose();
				}


				// Валидация токена

				Console.WriteLine($"{client.Client.RemoteEndPoint}");

				using (TcpClient server = new TcpClient())
				{
					try
					{
						await server.ConnectAsync(selectedServer);

						using var sslServer = new SslStream(server.GetStream(), false,
							(sender, cert, chain, sslPolicyErrors) => true);

						await sslServer.AuthenticateAsClientAsync("PokerServer", null, SslProtocols.Tls12, false);

						await Task.WhenAll(
							RedirectDataAsync(sslStreamClient, sslServer),
							RedirectDataAsync(sslServer, sslStreamClient)
						);
					}
					catch (Exception e)
					{
						Console.WriteLine($"{e.Message} - {e.InnerException}");
					}
					finally
					{
						client.Dispose();
					}
				}
			}
			catch (Exception ex) { }
		}
	}

	private static async Task RedirectDataAsync(SslStream fromSsl, SslStream toSll)
	{
		byte[] buffer = new byte[1024];
		int bytesRead;

		try
		{
			while ((bytesRead = await fromSsl.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				await toSll.WriteAsync(buffer, 0, bytesRead);
			}
		}
		catch (Exception ex)
		{

			Console.WriteLine($"Ошибка перенаправления данных: {ex.Message}");
		}
		finally
		{
			await fromSsl.DisposeAsync();
			await toSll.DisposeAsync();
		}
	}

	private static async Task<bool> IsServerAvailableAsync(IPEndPoint selectedServer)
	{
		try
		{
			using (var pingClient = new TcpClient())
			{
				await pingClient.ConnectAsync(selectedServer.Address, selectedServer.Port);
			}

			return true;
		}
		catch
		{
			return false;
		}
	}
	private static bool ValidateJwtToken(string token)
	{
		try
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes(_secretKey);

			tokenHandler.ValidateToken(token, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false
			}, out SecurityToken validatedToken);

			return validatedToken != null;
		}
		catch
		{
			return false;
		}
	}
	private static IPEndPoint GetNextServer()
	{
		lock (_lock)
		{
			IPEndPoint server = _server[_currentServerIndex];
			_currentServerIndex = (_currentServerIndex + 1) % _server.Count;
			return server;
		}
	}

}
