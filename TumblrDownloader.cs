using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace NeoRemote
{
	static class TumblrDownloader
	{
		public static void Run()
		{
			RunAsync();
		}

		static Task<bool> WaitForPageLoadAsync(WebBrowser browser, CancellationToken token)
		{
			var tcs = new TaskCompletionSource<bool>();
			token.Register(() => tcs.TrySetCanceled());
			LoadCompletedEventHandler handler = null;
			handler = (s, e) =>
			{
				browser.LoadCompleted -= handler;
				tcs.TrySetResult(true);
			};
			browser.LoadCompleted += handler;
			return tcs.Task;
		}

		async static void RunAsync()
		{
			var window = new Window { Width = 300, Height = 300, WindowState = WindowState.Maximized };
			window.Closed += (s, e) => Environment.Exit(0);
			var browser = new WebBrowser();
			window.Content = browser;
			window.Show();

			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var uri = new Uri("https://www.tumblr.com");

			//ClearCookies(uri);

			browser.Source = uri;
			await WaitForPageLoadAsync(browser, token);

			var doc = browser.Document as mshtml.HTMLDocument;

			//await LoginToTumblr(doc, token);

			var found = new HashSet<string>();
			while (true)
			{
				token.ThrowIfCancellationRequested();

				var posts = doc.getElementById("posts");
				if (posts == null)
					break;

				foreach (var child in posts.children)
				{
					if (child.className != "post_container")
						continue;

					var asdf = child;
					foreach (var childNode in child.all)
					{
						//	if ((childNode.tagName != "IMG") || (childNode.className == "post_avatar_image") || (childNode.className == "reblog-avatar-image-thumb"))
						//		continue;

						//	var src = new Uri(browser.Source, childNode.src).AbsoluteUri;
						//	if (!found.Contains(src))
						//		found.Add(src);
					}
				}

				if (found.Count >= 100)
					break;

				doc.parentWindow.scroll(0, 10000000);

				await Task.Delay(250);
			}
		}

		async static Task LoginToTumblr(WebBrowser browser, CancellationToken token)
		{
			var doc = browser.Document as mshtml.HTMLDocument;
			var loginButton = doc.getElementById("signup_login_button");
			if (loginButton != null)
			{
				loginButton.click();
				await WaitForPageLoadAsync(browser, token);
			}

			var emailField = doc.getElementById("signup_determine_email");
			if (emailField != null)
				((dynamic)emailField).value = "<username>";

			var emailSubmit = doc.getElementById("signup_forms_submit");
			if (emailSubmit != null)
				emailSubmit.click();

			var passwordField = doc.getElementById("signup_password");
			if (passwordField != null)
				((dynamic)passwordField).value = "<password>";

			await Task.Delay(1000);

			var emailSubmit2 = doc.getElementById("signup_forms_submit");
			if (emailSubmit2 != null)
				emailSubmit2.click();

			await WaitForPageLoadAsync(browser, token);
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

		class Win32
		{
			[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, int dwFlags, IntPtr lpReserved);

			[DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
			public static extern int InternetSetCookieEx(string url, string cookieName, string cookieData, int flags, int reserved);

			public const int InternetCookieHttponly = 0x2000;
		}
	}
}
