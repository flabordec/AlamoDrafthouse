using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Chrome;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IO;

namespace com.magusoft.drafthouse.ViewModel
{
	internal class DisposableDriver : IDisposable
	{
		private static object Lock = new object();

		private readonly IWebDriver mBrowser;
		public IWebDriver Browser { get { return mBrowser; } }

		public DisposableDriver()
		{
			lock (DisposableDriver.Lock)
			{
				var service = PhantomJSDriverService.CreateDefaultService("PhantomJS");
				service.HideCommandPromptWindow = true;
				this.mBrowser = new PhantomJSDriver(service);
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					if (this.Browser != null)
						this.Browser.Quit();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion

	}

	static class InternetHelpers
	{
		internal static async Task<string> GetPageContentAsync(string url)
		{
			return await Task.Run(() =>
			{
				using (WebClient client = new WebClient())
				{
					string source = client.DownloadString(url);
					return source;
				}
			});
		}

		internal static async Task<HtmlDocument> GetPageHtmlDocumentAsync(string url)
		{
			string source = await GetPageContentAsync(url);

			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(source);

			return document;
		}

		public static async Task PushbulletPushAsync(
			string authenticationToken,
			Dictionary<string, string> parameters)
		{
			WebRequest request = WebRequest.Create("https://api.pushbullet.com/v2/pushes");
			request.Method = "POST";
			request.Headers.Add("Authorization", $"Bearer {authenticationToken}");
			request.ContentType = "application/json; charset=UTF-8";

			string parametersString = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
			byte[] parametersByteArray = Encoding.UTF8.GetBytes(parametersString);

			request.ContentLength = parametersByteArray.Length;
			using (Stream dataStream = request.GetRequestStream())
			{
				await dataStream.WriteAsync(parametersByteArray, 0, parametersByteArray.Length);
			}
		}

	}
}
