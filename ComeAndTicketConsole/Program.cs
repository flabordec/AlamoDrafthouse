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
using Microsoft.EntityFrameworkCore;

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

        private static async Task<int> RunAndReturnExitCodeAsync(Options opts)
        {
            logger.Info("Reading markets");
            using (var db = new ComeAndTicketContext())
            {
                //await db.Database.EnsureCreatedAsync();

                await ComeAndTicketContext.UpdateDatabaseFromWeb(db);
                await db.SaveChangesAsync();

                //await
                //    db.ShowTimes
                //        .Include(s => s.Movie)
                //        .Include(s => s.Theater)
                //            .ThenInclude(t => t.Market)
                //    .LoadAsync();
                IEnumerable<ShowTime> showTimes = await FindMovies(opts, db);

                if (showTimes.Any())
                {
                    await PushMoviesAsync(showTimes, opts);
                    return 0;
                }
                else
                {
                    logger.Warn($"No show times for market {opts.Market}");
                    return -3;
                }
            }
        }

        private static async Task<IEnumerable<ShowTime>> FindMovies(Options opts, ComeAndTicketContext db)
        {
            // The database does not support doing a case-insensitive IN operation (to make the 
            // showtime.Movie.Title IN opts.Movies)
            // So we are stuck doing as much as we can in the database, and then converting to 
            // a list and filtering further...
            var partialShowTimes =
                db.ShowTimes
                .Where(s => EF.Functions.ILike(s.Theater.Market.Name, $"%{opts.Market}%"))
                .Where(s => s.SeatsLeft > 0)
                .Where(s => s.TicketsStatus == TicketsStatus.OnSale)
                .AsAsyncEnumerable();
            var partialShowTimesEnumeration = partialShowTimes.GetAsyncEnumerator();
            var showTimes = new List<ShowTime>();
            while (await partialShowTimesEnumeration.MoveNextAsync())
            {
                var partialShowTime = partialShowTimesEnumeration.Current;
                if (opts.Movies.Any(m => partialShowTime.Movie.Title.Contains(m, StringComparison.CurrentCultureIgnoreCase)))
                {
                    showTimes.Add(partialShowTime);
                }
            }

            return showTimes;
        }

        private static async Task PushMoviesAsync(IEnumerable<ShowTime> showTimes, Options opts)
        {
            XDocument configuration = GetConfigurationFile();

            var moviesOnSale = (
                from s in showTimes
                where MovieTitleContains(s.Movie, opts.Movies)
                where !MovieAlreadySent(configuration, s.Movie, s)
                where s.TicketsStatus != TicketsStatus.Past
                select s
                ).GroupBy(
                    s => s.Movie,
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
                    if (s.TicketsStatus == TicketsStatus.OnSale)
                        messageBuilder.AppendLine($" - {s.Date} (Left: {s.SeatsLeft} seats, Buy: {s.TicketsUrl} )");
                    else if (s.TicketsStatus == TicketsStatus.SoldOut)
                        messageBuilder.AppendLine($" - {s.Date} (Sold out)");
                    else
                        messageBuilder.AppendLine($" - {s.Date} (Unknown ticket status)");

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
                where movie.Attribute("TicketState").Value == s.TicketsStatus.ToString()
                select movie
                ).Any();
        }

        private static void MarkMovieSent(XDocument configuration, Movie m, ShowTime s)
        {
            configuration.Root.Add(
                new XElement("Movie",
                    new XAttribute("Title", m.Title),
                    new XAttribute("TicketsURL", s.TicketsUrl),
                    new XAttribute("TicketState", s.TicketsStatus)
                    )
                );
        }
    }
}
