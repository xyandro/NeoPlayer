using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NeoPlayer
{
	class Cryptor
	{
		public static string Encrypt(string str)
		{
			using (var alg = new AesCryptoServiceProvider())
			{
				alg.Key = Convert.FromBase64String("uSuggboVimnGAZ1cO8SOi+/GVAebh7lHKzc03OeiLBc=");

				using (var encryptor = alg.CreateEncryptor())
				using (var ms = new MemoryStream())
				{
					ms.Write(BitConverter.GetBytes(alg.IV.Length), 0, sizeof(int));
					ms.Write(alg.IV, 0, alg.IV.Length);
					var encrypted = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(str), 0, str.Length);
					ms.Write(encrypted, 0, encrypted.Length);
					return Convert.ToBase64String(ms.ToArray());
				}
			}
		}

		public static string Decrypt(string str)
		{
			using (var alg = new AesCryptoServiceProvider())
			{
				alg.Key = Convert.FromBase64String("uSuggboVimnGAZ1cO8SOi+/GVAebh7lHKzc03OeiLBc=");

				var data = Convert.FromBase64String(str);
				var iv = new byte[BitConverter.ToInt32(data, 0)];
				Array.Copy(data, sizeof(int), iv, 0, iv.Length);
				alg.IV = iv;

				using (var decryptor = alg.CreateDecryptor())
					return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(data, sizeof(int) + iv.Length, data.Length - sizeof(int) - iv.Length));
			}
		}

	}
}
