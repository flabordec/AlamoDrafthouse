using com.magusoft.drafthouse.Helpers;
using CommandLine;
using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using com.magusoft.drafthouse.Model;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;

namespace ComeAndTicketConsole
{
    public class Options
    {
        [Option('m', "market", Required = true, HelpText = "Set the market for the movies (for example: 'Austin').")]
        public string Market { get; set; }
        [Option('v', "movie", Required = true, HelpText = "The movie to search for.")]
        public string Movie { get; set; }
        [Option('p', "pushbulletApiToken", Required = true, HelpText = "The PushBullet token to use to push messages")]
        public string PushbulletApiToken { get; set; }
    }

    class Program
    {
        private const string CONFIG_FILE_NAME = "ComeAndTicket.config";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static async Task<int> Main(string[] args)
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logFile = new NLog.Targets.FileTarget("logfile") { FileName = "output.log" };
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            // Apply config
            LogManager.Configuration = config;

            var results = Parser.Default.ParseArguments<Options>(args);
            if (results.Tag == ParserResultType.Parsed)
            {
                try
                {
                    Options options = null;
                    results.WithParsed(o => options = o);
                    Debug.Assert(options != null);
                    return await RunAndReturnExitCodeAsync(options);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error while running");
                    Console.Error.WriteLine(ex);
                    logger.Error(ex);
                    return -1;
                }
            }
            else
            {
                return -2;
            }
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                logger.Error($"Invalid value for parameter: {error.Tag}");
            }
        }

        private static async Task<IEnumerable<Market>> OnReloadMarketsAsync()
        {
            HtmlDocument marketsDocument = await InternetHelpers.GetPageHtmlDocumentAsync("https://drafthouse.com/markets");

            var markets =
                from node in marketsDocument.DocumentNode.Descendants("a")
                where node.Attributes["id"]?.Value == "markets-page"
                let url = node.Attributes["href"].Value
                select new Market(url, node.InnerText);

            return markets;
        }

        private static async Task<int> RunAndReturnExitCodeAsync(Options opts)
        {

            logger.Info("Reading markets");
            var markets = await OnReloadMarketsAsync();

            var market = (
                from m in markets
                where m.Name.Equals(opts.Market, StringComparison.CurrentCultureIgnoreCase)
                select m
                ).SingleOrDefault();

            if (market != null)
            {

                await market.OnLoadTheatersAsync();

                await Task.WhenAll(
                    from t in market.Theaters
                    select t.OnLoadMoviesAsync()
                    );

                await PushMovies(market, opts);

                return 0;
            }
            else
            {
                logger.Warn($"Market not found {market}");
                return -3;
            }
        }

        private static async Task PushMovies(Market market, Options opts)
        {
            var moviesOnSale =
                from t in market.Theaters
                from m in t.Movies
                from s in m.ShowTimes
                where MovieTitleContains(m, opts.Movie)
                where !MovieAlreadySent(t, m, s)
                group s by new { Theater = t, Movie = m } into showtimes
                select showtimes;

            if (!moviesOnSale.Any())
                return;

            var moviesSent = new List<Tuple<Theater, Movie, ShowTime>>();
            var messageBuilder = new StringBuilder();
            foreach (var movieOnSale in moviesOnSale)
            {
                Theater t = movieOnSale.Key.Theater;
                Movie m = movieOnSale.Key.Movie;
                messageBuilder.AppendLine($"{t.Name} has {m.Title}:");
                foreach (ShowTime s in movieOnSale)
                {
                    if (s.MyTicketsStatus == TicketsStatus.OnSale)
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Left: {s.SeatsLeft} seats, Buy: {s.TicketsUrl})");
                    else if (s.MyTicketsStatus == TicketsStatus.SoldOut)
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Sold out)");
                    else if (s.MyTicketsStatus == TicketsStatus.Past)
                        continue;
                    else
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Unknown ticket status)");

                    moviesSent.Add(new Tuple<Theater, Movie, ShowTime>(t, m, s));
                }
            }
            logger.Info(messageBuilder.ToString());

            var parameters = new Dictionary<string, string>()
            {
                ["type"] = "note",
                ["title"] = "New tickets available",
                ["body"] = messageBuilder.ToString()
            };
            await InternetHelpers.PushbulletPushAsync(opts.PushbulletApiToken, parameters);

            // Mark all movies that were sent
            foreach (var tuple in moviesSent)
                MarkMovieSent(tuple.Item1, tuple.Item2, tuple.Item3);
        }

        private static XDocument GetConfigurationFile()
        {
            if (File.Exists(CONFIG_FILE_NAME))
            {
                return XDocument.Load(CONFIG_FILE_NAME);
            }
            else
            {
                return new XDocument(
                    new XElement("SentMovies"));
            }
        }

        private static bool MovieTitleContains(Movie movie, string wantedTitle)
        {
            return movie.Title.Contains(wantedTitle, StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool MovieAlreadySent(Theater t, Movie m, ShowTime s)
        {
            XDocument doc = GetConfigurationFile();
            return (
                from movie in doc.Descendants("Movie")
                where movie.Attribute("Theater").Value == t.Name
                where movie.Attribute("Title").Value == m.Title
                where movie.Attribute("TicketsURL").Value == s.TicketsUrl
                where movie.Attribute("TicketState").Value == s.MyTicketsStatus.ToString()
                select movie
                ).Any();
        }

        private static void MarkMovieSent(Theater t, Movie m, ShowTime s)
        {
            XDocument doc = GetConfigurationFile();
            doc.Root.Add(
                new XElement("Movie",
                    new XAttribute("Theater", t.Name),
                    new XAttribute("Title", m.Title),
                    new XAttribute("TicketsURL", s.TicketsUrl),
                    new XAttribute("TicketState", s.MyTicketsStatus)
                    )
                );
            doc.Save(CONFIG_FILE_NAME);
        }

    }
}
