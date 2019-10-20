using CommandLine;
using HtmlAgilityPack;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MaguSoft.ComeAndTicket.Core.Model;
using MaguSoft.ComeAndTicket.Core.Helpers;
using PushbulletDotNet;

namespace MaguSoft.ComeAndTicket.Console
{
    public class Options
    {
        [Option('m', "market", Required = true, HelpText = "Set the market for the movies (for example: 'Austin').")]
        public string Market { get; set; }
        [Option('v', "movie", Required = true, HelpText = "The movie to search for.")]
        public IEnumerable<string> Movies { get; set; }
        [Option('p', "pushbullet-api-token", Required = true, HelpText = "The PushBullet token to use to push messages")]
        public string PushbulletApiToken { get; set; }
        [Option('i', "device-ids", Required = false, HelpText = "The PushBullet device identifier to push to")]
        public IEnumerable<string> DeviceIdentifiers { get; set; }
        [Option('n', "device-nicknames", Required = false, HelpText = "The PushBullet device nicknames to push to")]
        public IEnumerable<string> DeviceNicknames { get; set; }
    }

    class Program
    {
        private const string CONFIG_FILE_NAME = "ComeAndTicket.config";
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static Pushbullet _pushbulletApi;
        private static HashSet<IDevice> _devices;

        static async Task<int> Main(string[] args)
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "output.log",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 10,
            };
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            // Apply config
            LogManager.Configuration = config;

            var results = Parser.Default.ParseArguments<Options>(args);
            if (results.Tag == ParserResultType.Parsed)
            {
                var options = ((Parsed<Options>)results).Value;
                _pushbulletApi = new Pushbullet(options.PushbulletApiToken);
                var devicesById = options.DeviceIdentifiers.Select(async id => await _pushbulletApi.GetDeviceById(id));
                var devicesByNickname = options.DeviceNicknames.Select(async nickname => await _pushbulletApi.GetDeviceByNickname(nickname));
                var devices = await Task.WhenAll(devicesById.Concat(devicesByNickname));
                _devices = new HashSet<IDevice>(devices);
                
                try
                {
                    return await RunAndReturnExitCodeAsync(options);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception while running");

                    await _pushbulletApi.PushNoteAsync(
                        "Error while getting tickets",
                        ex.Message,
                        _devices);
                    return -1;
                }
            }
            else
            {
                logger.Error("Invalid command line arguments");
                return -2;
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

                await PushMoviesAsync(market, opts);

                return 0;
            }
            else
            {
                logger.Warn($"Market not found {market}");
                return -3;
            }
        }

        private static async Task PushMoviesAsync(Market market, Options opts)
        {
            XDocument configuration = GetConfigurationFile();

            var moviesOnSale = (
                from t in market.Theaters
                from m in t.Movies
                from s in m.ShowTimes
                where MovieTitleContains(m, opts.Movies)
                where !MovieAlreadySent(configuration, m, s)
                where s.MyTicketsStatus != TicketsStatus.Past
                select (s, m)
                ).GroupBy(
                    ((ShowTime ShowTime, Movie Movie) p) => p.Movie,
                    ((ShowTime ShowTime, Movie Movie) p) => p.ShowTime,
                    MovieComparer.TitleCurrentCultureIgnoreCase);

            if (!moviesOnSale.Any())
                return;

            var moviesSent = new List<(Movie, ShowTime)>();
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(Environment.MachineName);
            foreach (var movieOnSale in moviesOnSale)
            {
                Movie m = movieOnSale.Key;
                messageBuilder.AppendLine(m.Title);
                foreach (ShowTime s in movieOnSale)
                {
                    if (s.MyTicketsStatus == TicketsStatus.OnSale)
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Left: {s.SeatsLeft} seats, Buy: {s.TicketsUrl} )");
                    else if (s.MyTicketsStatus == TicketsStatus.SoldOut)
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Sold out)");
                    else
                        messageBuilder.AppendLine($" - {s.MyShowTime} (Unknown ticket status)");

                    moviesSent.Add((m, s));
                }

                messageBuilder.AppendLine();
            }
            logger.Info(messageBuilder.ToString());

            await _pushbulletApi.PushNoteAsync(
                "New tickets available",
                messageBuilder.ToString(),
                _devices);

            SaveConfiguration(configuration, moviesSent);
        }

        private static void SaveConfiguration(XDocument configuration, List<(Movie Movie, ShowTime ShowTime)> moviesSent)
        {
            foreach (var tuple in moviesSent)
                MarkMovieSent(configuration, tuple.Movie, tuple.ShowTime);
            configuration.Save(CONFIG_FILE_NAME);
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

        private static bool MovieTitleContains(Movie movie, IEnumerable<string> wantedTitles)
        {
            return wantedTitles.Any(wantedTitle => movie.Title.Contains(wantedTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        private static bool MovieAlreadySent(XDocument configuration, Movie m, ShowTime s)
        {
            return (
                from movie in configuration.Descendants("Movie")
                where movie.Attribute("Title").Value == m.Title
                where movie.Attribute("TicketsURL").Value == s.TicketsUrl
                where movie.Attribute("TicketState").Value == s.MyTicketsStatus.ToString()
                select movie
                ).Any();
        }

        private static void MarkMovieSent(XDocument configuration, Movie m, ShowTime s)
        {
            configuration.Root.Add(
                new XElement("Movie",
                    new XAttribute("Title", m.Title),
                    new XAttribute("TicketsURL", s.TicketsUrl),
                    new XAttribute("TicketState", s.MyTicketsStatus)
                    )
                );
        }
    }
}
