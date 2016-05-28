using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;

namespace com.magusoft.drafthouse.ViewModel
{
	internal class DisposableDriver : IDisposable
	{
		private static object Lock = new object();

		private readonly IWebDriver mBrowser;
		public IWebDriver Browser { get { return mBrowser; } }

		public DisposableDriver()
		{
			lock(DisposableDriver.Lock)
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

	static class WebDriverHelper
	{
		internal static HtmlDocument GetPageHtmlDocument(string url)
		{
			using (DisposableDriver driver = new DisposableDriver())
			{
				driver.Browser.Navigate().GoToUrl(url);
				string source = driver.Browser.PageSource;

				HtmlDocument marketsDocument = new HtmlDocument();
				marketsDocument.LoadHtml(source);

				return marketsDocument;
			}
		}

		internal static async Task<HtmlDocument> GetPageHtmlDocumentAsync(string url)
		{
			return await Task.Run(() =>
			{
				return GetPageHtmlDocument(url);
			});
		}
	}
}
