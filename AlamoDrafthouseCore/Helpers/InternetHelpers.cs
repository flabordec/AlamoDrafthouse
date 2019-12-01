using HtmlAgilityPack;
using NLog;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Helpers
{
    public static class PolicyHelpers
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        public static AsyncRetryPolicy RetryPolicy { get; }

        static PolicyHelpers()
        {
            IEnumerable<TimeSpan> delay = Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromSeconds(1),
                retryCount: 5);
            RetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    delay,
                    (ex, ts) => _logger.Warn(ex, "Retrying in {TimeSpan}", ts));
        }
    }

    public static class InternetHelpers
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static async Task<string> GetPageContentAsync(string url)
        {
            return await PolicyHelpers.RetryPolicy.ExecuteAsync(async () => await InnerGetPageContentAsync(url));
        }

        private static async Task<string> InnerGetPageContentAsync(string url)
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
    }
}
