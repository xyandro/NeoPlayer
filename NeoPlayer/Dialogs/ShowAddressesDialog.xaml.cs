using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace NeoPlayer.Dialogs
{
	partial class ShowAddressesDialog
	{
		static DependencyProperty AddressesProperty = DependencyProperty.Register(nameof(Addresses), typeof(string), typeof(ShowAddressesDialog));

		string Addresses { get => (string)GetValue(AddressesProperty); set => SetValue(AddressesProperty, value); }

		ShowAddressesDialog()
		{
			InitializeComponent();
			DataContext = this;
			GetAddresses();
		}

		void GetAddresses()
		{
			var sb = new StringBuilder();
			foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces().OrderBy(inter => inter.NetworkInterfaceType == NetworkInterfaceType.Loopback).ThenBy(inter => inter.OperationalStatus))
				foreach (var address in networkInterface.GetIPProperties().UnicastAddresses.Select(property => property.Address).OrderBy(address => address.AddressFamily != AddressFamily.InterNetwork))
					sb.AppendLine($"{address}: ({networkInterface.NetworkInterfaceType}, {networkInterface.OperationalStatus})");
			Addresses = sb.ToString();
		}

		public static void Run() => new ShowAddressesDialog().ShowDialog();
	}
}
