using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeoPlayer.Networking
{
	static class WLAN
	{
		static class Win32
		{
			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
			public struct DOT11_SSID
			{
				public int uSSIDLength;
				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string ucSSID;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS
			{
				public DOT11_SSID hostedNetworkSSID;
				public int dwMaxNumberOfPeers;
			}

			[DllImport("Wlanapi.dll")]
			public static extern int WlanOpenHandle(int dwClientVersion, IntPtr pReserved, [Out] out int pdwNegotiatedVersion, out IntPtr ClientHandle);
			[DllImport("Wlanapi.dll")]
			public static extern int WlanHostedNetworkInitSettings(IntPtr hClientHandle, IntPtr pFailReason, IntPtr pReserved);
			[DllImport("Wlanapi.dll")]
			public static extern int WlanHostedNetworkSetProperty(IntPtr hClientHandle, WLAN_HOSTED_NETWORK_OPCODE OpCode, int dwDataSize, ref WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS pvData, IntPtr pFailReason, IntPtr pReserved);
			[DllImport("Wlanapi.dll")]
			public static extern int WlanHostedNetworkSetProperty(IntPtr hClientHandle, WLAN_HOSTED_NETWORK_OPCODE OpCode, int dwDataSize, ref bool pvData, IntPtr pFailReason, IntPtr pReserved);
			[DllImport("Wlanapi.dll")]
			public static extern int WlanHostedNetworkSetSecondaryKey(IntPtr hClientHandle, int dwKeyLength, [MarshalAs(UnmanagedType.LPStr)] string pucKeyData, [MarshalAs(UnmanagedType.Bool)] bool bIsPassPhrase, [MarshalAs(UnmanagedType.Bool)] bool bPersistent, IntPtr pFailReason, IntPtr pvReserved);
			[DllImport("Wlanapi.dll")]
			public static extern int WlanHostedNetworkStartUsing(IntPtr hClientHandle, IntPtr pFailReason, IntPtr pReserved);

			public enum WLAN_HOSTED_NETWORK_OPCODE
			{
				wlan_hosted_network_opcode_connection_settings,
				wlan_hosted_network_opcode_security_settings,
				wlan_hosted_network_opcode_station_profile,
				wlan_hosted_network_opcode_enable,
			}

			public const int ERROR_SUCCESS = 0;
		}

		public static void Start(string ssid, string password)
		{
			try
			{
				if (Win32.WlanOpenHandle(2, IntPtr.Zero, out int version, out IntPtr handle) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanOpenHandle)} failed");

				if (Win32.WlanHostedNetworkInitSettings(handle, IntPtr.Zero, IntPtr.Zero) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanHostedNetworkInitSettings)} failed");

				var settings = new Win32.WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS
				{
					dwMaxNumberOfPeers = 100,
					hostedNetworkSSID = new Win32.DOT11_SSID
					{
						ucSSID = ssid,
						uSSIDLength = ssid.Length,
					},
				};

				if (Win32.WlanHostedNetworkSetProperty(handle, Win32.WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_connection_settings, Marshal.SizeOf(settings), ref settings, IntPtr.Zero, IntPtr.Zero) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanHostedNetworkSetProperty)} failed");

				var enable = true;
				if (Win32.WlanHostedNetworkSetProperty(handle, Win32.WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_enable, 4, ref enable, IntPtr.Zero, IntPtr.Zero) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanHostedNetworkSetProperty)} failed");

				if (Win32.WlanHostedNetworkSetSecondaryKey(handle, password.Length + 1, password, true, true, IntPtr.Zero, IntPtr.Zero) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanHostedNetworkSetSecondaryKey)} failed");

				if (Win32.WlanHostedNetworkStartUsing(handle, IntPtr.Zero, IntPtr.Zero) != Win32.ERROR_SUCCESS)
					throw new Exception($"{nameof(Win32.WlanHostedNetworkStartUsing)} failed");

				MessageBox.Show($"Network {ssid} started with password {password}", "Success");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
			}
		}
	}
}
