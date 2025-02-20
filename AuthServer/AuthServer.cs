using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using AuthServer.DTOModels;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace AuthServer
{

	internal class AuthServer
	{

		private static readonly Dictionary<string, string> _users = new()
		{
			{ "pl1", "qwerty" },
			{ "pl2", "qwerty" }
		};

		private static readonly string _secretKey = "SuperSecretKijdfgjaeoiyj34o9uyjhuwierfjhoiqejy0ju5490hjueoifrhoijaasdeyForJWTToken123!";

		static async Task Main(string[] args)
		{

			var certificate = new X509Certificate2("serverCert.pfx", "qwerty123");


			TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 4000);
			listener.Start();
			Console.WriteLine("[AuthServer] Auth Server запущен на порту 4000...");

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
				NetworkStream stream = client.GetStream();
				SslStream sslStream = new SslStream(stream, false);

				try
				{
					await sslStream.AuthenticateAsServerAsync(certificate,
						clientCertificateRequired: false,
						enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12,
						checkCertificateRevocation: false

						);



					byte[] buffer = new byte[1024];
					int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);

					if (bytesRead == 0)
					{
						// Клиент отключился раньше
						return;
					}

					string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);


					var credentials = JsonSerializer.Deserialize<LoginRequest>(request);
					
					string response;

					if (_users.TryGetValue(credentials.Username, out var password) && password == credentials.Password)
					{
						string token = GenerateJwtToken(credentials.Username);
						response = JsonSerializer.Serialize(new { Success = true, Token = token });
					}
					else
					{
						response = JsonSerializer.Serialize(new { Success = false, Token = "Invalid credentials" });
					}

					byte[] responseData = Encoding.UTF8.GetBytes(response);
					await sslStream.WriteAsync(responseData, 0, responseData.Length);
				}
				catch(Exception ex)
				{
					Console.WriteLine($"[AuthServer] TLS error: {ex.Message}");
				}

			}
		}

		private static string GenerateJwtToken(string username)
		{
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
				new Claim(ClaimTypes.Name, username),
			};

			var token = new JwtSecurityToken(
				issuer: "PokerGame",
				audience: "Players",
				claims: claims,
				expires: DateTime.UtcNow.AddHours(1),
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}

