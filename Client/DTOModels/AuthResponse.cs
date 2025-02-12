using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.DTOModels
{
	public class AuthResponse
	{
		public bool Success { get; set; }
		public string Token { get; set; }
	}
}
