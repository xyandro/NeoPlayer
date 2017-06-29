using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeoPlayer
{
	static class TumblrSlideSource
	{
		async public static void Run(string username, string password, Action<string> action, CancellationToken token)
		{
			var urls = new AsyncQueue<string>();
			SlideDownloader.Run(urls, action, token);
			using (var browser = new WebBrowser())
				try
				{
					var uri = new Uri("https://www.tumblr.com");

					ClearCookies(uri);

					browser.Navigate(uri);
					await WaitForPageLoadAsync(browser, token);

					await LoginToTumblr(username, password, browser, token);

					var doc = browser.Document;
					var found = new HashSet<string>();
					while (!token.IsCancellationRequested)
					{
						var posts = doc.GetElementById("posts");
						if (posts == null)
							break;

						foreach (HtmlElement child in posts.Children)
						{
							if (child.GetAttribute("classname") != "post_container")
								continue;

							var ad = false;
							foreach (HtmlElement divs in child.GetElementsByTagName("div"))
							{
								var className = $" {divs.GetAttribute("classname").ToLowerInvariant()} ";
								if (className.Contains(" sponsored_post "))
								{
									ad = true;
									break;
								}
							}
							if (ad)
								continue;

							foreach (HtmlElement image in child.GetElementsByTagName("img"))
							{
								var className = image.GetAttribute("classname");
								if ((className == "post_avatar_image") || (className == "reblog-avatar-image-thumb"))
									continue;

								var src = new Uri(browser.Url, image.GetAttribute("src")).AbsoluteUri;
								if (!found.Contains(src))
									urls.Enqueue(src);
								found.Add(src);
							}
						}

						if (found.Count >= 200)
						{
							urls.SetFinished();
							break;
						}

						doc.Body.ScrollIntoView(false);

						await Task.Delay(250);
					}
				}
				catch { }
		}

		async static Task LoginToTumblr(string username, string password, WebBrowser browser, CancellationToken token)
		{
			var doc = browser.Document;
			var loginButton = doc.GetElementById("signup_login_button");
			if (loginButton != null)
			{
				loginButton.InvokeMember("click");
				var start = DateTime.Now;
				await WaitForPageLoadAsync(browser, token);
				var end = DateTime.Now;
				var elapsed = (end - start).TotalMilliseconds;
			}

			var emailField = doc.GetElementById("signup_determine_email");
			if (emailField != null)
				emailField.SetAttribute("value", username);

			var emailSubmit = doc.GetElementById("signup_forms_submit");
			if (emailSubmit != null)
				emailSubmit.InvokeMember("click");

			var passwordField = doc.GetElementById("signup_password");
			if (passwordField != null)
				passwordField.SetAttribute("value", password);

			await Task.Delay(2000);

			var emailSubmit2 = doc.GetElementById("signup_forms_submit");
			if (emailSubmit2 != null)
			{
				emailSubmit2.InvokeMember("click");
				await WaitForPageLoadAsync(browser, token);
			}
		}

		static void ClearCookies(Uri uri)
		{
			var cookieData = new StringBuilder { Capacity = 0 };
			while (true)
			{
				int size = cookieData.Capacity;
				if (Win32.InternetGetCookieEx(uri.ToString(), null, cookieData, ref size, Win32.InternetCookieHttponly, IntPtr.Zero))
					break;
				if (size == 0)
					return;
				cookieData.Capacity = size;
			}

			if (cookieData.Length == 0)
				return;

			var cookies = new CookieContainer();
			cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
			foreach (Cookie cookie in cookies.GetCookies(uri))
				Win32.InternetSetCookieEx(uri.ToString(), cookie.Name, "value; expires = Sat,01-Jan-2000 00:00:00 GMT", Win32.InternetCookieHttponly, 0);
		}

		static Task<bool> WaitForPageLoadAsync(WebBrowser browser, CancellationToken token)
		{
			var tcs = new TaskCompletionSource<bool>();
			token.Register(() => tcs.TrySetCanceled());
			WebBrowserDocumentCompletedEventHandler handler = null;
			handler = (s, e) =>
			{
				if (e.Url.AbsolutePath == "blank")
					return;

				browser.DocumentCompleted -= handler;
				tcs.TrySetResult(true);
			};
			browser.DocumentCompleted += handler;
			return tcs.Task;
		}

		class Win32
		{
			[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int flags, IntPtr reserved);

			[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern int InternetSetCookieEx(string url, string cookieName, string cookieData, int flags, int reserved);

			public const int InternetCookieHttponly = 0x2000;
		}
	}
}
