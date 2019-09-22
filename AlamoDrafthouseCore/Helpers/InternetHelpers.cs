using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace com.magusoft.drafthouse.Helpers
{
    public static class InternetHelpers
    {
        public static async Task<string> GetPageContentAsync(string url)
        {
            using (var clientHandler = new HttpClientHandler() { AllowAutoRedirect = false })
            using (var client = new HttpClient(clientHandler))
            {
                // The alamo put a redirect from an HTTPS site to an HTTP site, in .NET core 
                // the HttpClient will not follow these redirects becuase of security, but 
                // we need to follow the redirect to get data.
                var response = await client.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.Redirect || 
                    response.StatusCode == HttpStatusCode.PermanentRedirect ||
                    response.StatusCode == HttpStatusCode.RedirectMethod ||
                    response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    return await GetPageContentAsync(response.Headers.Location.AbsoluteUri);
                }
                else
                {
                    string source = await response.Content.ReadAsStringAsync();
                    return source;
                }
            }
        }

        public static async Task<HtmlDocument> GetPageHtmlDocumentAsync(string url)
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
