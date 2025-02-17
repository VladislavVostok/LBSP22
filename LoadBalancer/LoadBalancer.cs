using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LoadBalancer;

class LoadBalancer
{
    private static readonly List<IPEndPoint> _server = new(){
        new IPEndPoint(IPAddress.Parse("217.25.88.28"), 5001),
        new IPEndPoint(IPAddress.Parse("217.25.88.28"), 5002),
        new IPEndPoint(IPAddress.Parse("217.25.88.28"), 5003)
    };

    private static readonly object _lock = new object();
    private static int _currentServerIndex = 0;
	private static readonly string _secretKey = "SuperSecretKijdfgjaeoiyj34o9uyjhuwierfjhoiqejy0ju5490hjueoifrhoijaasdeyForJWTToken123!";

	static async Task Main(string[] args)
    {
        TcpListener listener = new TcpListener(IPAddress.Parse("217.25.88.28"), 5000);
        listener.Start();

        Console.WriteLine("Балансировочный сервер запущен на порту 5000");


        while(true){
            TcpClient client = await listener.AcceptTcpClientAsync();

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }


    private static async Task HandleClientAsync(TcpClient client){


        using (client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			if (!ValidateJwtToken(request))
			{
				Console.WriteLine("Неверный JWT токен. Отклоняем соединение.");
				client.Close();
				return;
			}

		}



			IPEndPoint selectedServer = GetNextServer();
       
        if(!await IsServerAvailableAsync(selectedServer)){
            Console.WriteLine($"Сервер {selectedServer} недоступен. Закрываем соединение с клиентом...");
            client.Dispose();
        }

       // Валидация токена

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
	private static IPEndPoint GetNextServer(){
        lock(_lock){
            IPEndPoint server = _server[_currentServerIndex];
            _currentServerIndex = (_currentServerIndex + 1) % _server.Count;
            return server;
        }
    }

}
