using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.IO;

namespace com.magusoft.drafthouse.ViewModel
{
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
