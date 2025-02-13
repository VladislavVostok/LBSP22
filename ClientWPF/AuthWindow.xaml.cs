using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClientWPF
{
	/// <summary>
	/// Логика взаимодействия для AuthWindow.xaml
	/// </summary>
	public partial class AuthWindow : Window
	{
		public string Username { get; private set; }
		public string Password { get; private set; }

		public AuthWindow()
		{
			InitializeComponent();
		}

		private void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			Username = UsernameBox.Text;
			Password = PasswordBox.Password;

			if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
			{
				MessageBox.Show("Введите логин и пароль.");
				return;
			}
			DialogResult = true;
			Close();

		}
	}
}
