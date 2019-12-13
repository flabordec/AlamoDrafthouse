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
        [Option('i', "device-ids", Required = false, HelpText = "The PushBullet device identifier to push to")]
        public IEnumerable<string> DeviceIdentifiers { get; set; }
        [Option('n', "device-nicknames", Required = false, HelpText = "The PushBullet device nicknames to push to")]
        public IEnumerable<string> DeviceNicknames { get; set; }
    }

    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static Pushbullet _pushbulletApi;
        
        private static Dictionary<string, string> _configuration;
        

        static async Task<int> Main(string[] args)
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            string logLayout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${onexception:inner=${newline}${exception:format=toString}}";
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                Layout = logLayout,
                FileName = "output.log",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 30,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                ArchiveAboveSize = 512 * 1024, // 512 KB
            };
            var logConsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                Layout = logLayout,
            };

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            // Apply config
            LogManager.Configuration = config;

            var results = Parser.Default.ParseArguments<Options>(args);
            if (results.Tag == ParserResultType.Parsed)
            {
                using (var db = new ComeAndTicketContext())
                {
                    HashSet<IDevice> devicesSet = null;
                    try
                    {
                        var options = ((Parsed<Options>)results).Value;

                        _logger.Info("Options read {Options}", options);

                        _logger.Info("Reading configuration from DB");
                        _configuration = await db.Configuration.ToDictionaryAsync(c => c.Name, c => c.Value);
                        string pushbulletApiToken = _configuration["pushbullet-api-token"];

                        _logger.Info("Getting devices from Pushbullet");
                        _pushbulletApi = new Pushbullet(pushbulletApiToken);
                        var devicesById = options.DeviceIdentifiers.Select(async id => await _pushbulletApi.GetDeviceById(id));
                        var devicesByNickname = options.DeviceNicknames.Select(async nickname => await _pushbulletApi.GetDeviceByNickname(nickname));
                        var devices = await Task.WhenAll(devicesById.Concat(devicesByNickname));
                        devicesSet = new HashSet<IDevice>(devices);

                        return await RunAndReturnExitCodeAsync(options, devicesSet, db);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Exception while running");
                        if (devicesSet != null)
                        {
                            await _pushbulletApi.PushNoteAsync(
                                "Error while getting tickets",
                                ex.Message,
                                devicesSet);
                        }
                        return -1;
                    }
                }
            }
            else
            {
                _logger.Error("Invalid command line arguments {Arguments}", args);
                return -2;
            }
        }

        private static async Task<int> RunAndReturnExitCodeAsync(Options opts, HashSet<IDevice> devices, ComeAndTicketContext db)
        {
            // Convert into a service
            // https://devblogs.microsoft.com/dotnet/net-core-and-systemd/

            _logger.Info("Updating Drafthouse data from web");
            await ComeAndTicketContext.UpdateDatabaseFromWebAsync(db);
            _logger.Info("Updating devices from Pushbullet");
            await ComeAndTicketContext.UpdateDevicesAsync(db, devices);

            //await
            //    db.ShowTimes
            //        .Include(s => s.Movie)
            //        .Include(s => s.Theater)
            //            .ThenInclude(t => t.Market)
            //    .LoadAsync();
            IEnumerable<ShowTime> showTimes = await FindMovies(opts, db);

            if (showTimes.Any())
            {
                _logger.Info("Some movies found");
                await PushMoviesAsync(showTimes, opts, devices, db);
                _logger.Info("All new movies pushed");
                return 0;
            }
            else
            {
                _logger.Warn("No show times for market {Market}", opts.Market);
                return -3;
            }
        }

        private static async Task<IEnumerable<ShowTime>> FindMovies(Options opts, ComeAndTicketContext db)
        {
            // The database does not support doing a case-insensitive IN operation (to make the 
            // showtime.Movie.Title IN opts.Movies)
            // So we are stuck doing as much as we can in the database, and then converting to 
            // a list and filtering further...
            var partialShowTimes = await
                db.ShowTimes
                .Where(s => EF.Functions.ILike(s.Theater.Market.Name, $"%{opts.Market}%"))
                .Where(s => s.SeatsLeft > 0)
                .Where(s => s.TicketsStatus == TicketsStatus.OnSale)
                .ToArrayAsync();
            
            var showTimes = new List<ShowTime>();
            foreach (var partialShowTime in partialShowTimes)
            {
                if (opts.Movies.Any(m => partialShowTime.Movie.Title.Contains(m, StringComparison.CurrentCultureIgnoreCase)))
                {
                    showTimes.Add(partialShowTime);
                }
            }

            return showTimes;
        }

        private static async Task PushMoviesAsync(IEnumerable<ShowTime> showTimes, Options opts, IEnumerable<IDevice> devices, ComeAndTicketContext db)
        {
            var targetsById = await db.Targets.ToDictionaryAsync(t => t.Id);
            foreach (var device in devices)
            {
                var target = targetsById[device.Id];

                IEnumerable<IGrouping<Movie, ShowTime>> moviesOnSale = (
                    from s in showTimes
                    where MovieTitleContains(s.Movie, opts.Movies)
                    where !MovieAlreadySent(s, target)
                    where s.TicketsStatus != TicketsStatus.Past
                    select s
                    ).GroupBy(
                        s => s.Movie,
                        MovieComparer.TitleCurrentCultureIgnoreCase);

                if (!moviesOnSale.Any())
                    continue;

                var showTimesSent = new List<ShowTime>();
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

                        showTimesSent.Add(s);
                    }

                    messageBuilder.AppendLine();
                }
                _logger.Info("Pushing new tickets to {DeviceName}\n{Message}", device.Nickname, messageBuilder.ToString());

                await _pushbulletApi.PushNoteAsync(
                    "New tickets available",
                    messageBuilder.ToString(),
                    device.Id);

                MarkMovieSent(showTimesSent, target);
            }

            await db.SaveChangesAsync();
        }

        private static bool MovieTitleContains(Movie movie, IEnumerable<string> wantedTitles)
        {
            return wantedTitles.Any(wantedTitle => movie.Title.Contains(wantedTitle, StringComparison.CurrentCultureIgnoreCase));
        }

        private static bool MovieAlreadySent(ShowTime showTime, Target target)
        {
            return showTime.TargetsUpdated.Select(tu => tu.Target).Contains(target);
        }

        private static void MarkMovieSent(IEnumerable<ShowTime> showTimesSent, Target target)
        {
            foreach (ShowTime showTime in showTimesSent)
            {
                showTime.TargetsUpdated.Add(new ShowTimeTarget(showTime, target));
            }
        }
    }
}
