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
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

            
            using (var db = new ComeAndTicketContext())
            {
                try
                {
                    _logger.Info("Creating database");
                    if (db.Database.IsSqlite())
                    {
                        await db.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        await db.Database.MigrateAsync();
                    }
                        

                    _logger.Info("Reading configuration from DB");
                    _configuration = await db.Configuration.ToDictionaryAsync(c => c.Name, c => c.Value);
                    return await RunAndReturnExitCodeAsync(db);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception while running");
                    return -1;
                }
            }
        }

        private static async Task<int> RunAndReturnExitCodeAsync(ComeAndTicketContext db)
        {
            _logger.Info("Updating Drafthouse data from web");
            //await ComeAndTicketContext.UpdateDatabaseFromWebAsync(db);

            //await
            //    db.ShowTimes
            //        .Include(s => s.Movie)
            //        .Include(s => s.Theater)
            //            .ThenInclude(t => t.Market)
            //    .LoadAsync();
            await PushMoviesAsync(db);
            return 0;
        }

        private static async Task<IEnumerable<ShowTime>> FindMoviesAsync(User user, ComeAndTicketContext db)
        {
            // The database does not support doing a case-insensitive IN operation (to make the 
            // showtime.Movie.Title IN opts.Movies)
            // So we just get all the movies in the user's market, and then filter further later
            var partialShowTimes = await
                db.ShowTimes
                .Include(st => st.Movie)
                .Include(st => st.UsersUpdated)
                .Where(s => s.Theater.Market == user.HomeMarket)
                .Where(s => s.SeatsLeft > 0)
                .Where(s => s.TicketsStatus == TicketsStatus.OnSale)
                .ToArrayAsync();

            return partialShowTimes;
        }

        private static async Task PushMoviesAsync(ComeAndTicketContext db)
        {
            var users = await db.Users
                .Include(u => u.HomeMarket)
                .Include(u => u.Notifications)
                .Include(u => u.MovieTitlesToWatch)
                .Include(u => u.DeviceNicknames)
                .ToListAsync();
            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(user.PushbulletApiKey))
                {
                    _logger.Warn($"User is not configured with a pushbullet API key: {user.EMail}");
                    continue;
                }

                _logger.Info($"Getting devices from Pushbullet for user {user.EMail}");
                var pushbulletApi = new Pushbullet(user.PushbulletApiKey);
                var retrieveDevicesByNickname = user.DeviceNicknames.Select(async nickname => await pushbulletApi.GetDeviceByNickname(nickname.Value));
                var devices = await Task.WhenAll(retrieveDevicesByNickname);

                if (!devices.Any())
                    continue;

                _logger.Info($"Finding movies for user {user.EMail}");
                var showTimes = await FindMoviesAsync(user, db);

                IEnumerable<IGrouping<Movie, ShowTime>> moviesOnSale = (
                    from s in showTimes
                    where MovieTitleContains(s.Movie, user.MovieTitlesToWatch)
                    where !MovieAlreadySent(s, user)
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

                foreach (var device in devices)
                {
                    await pushbulletApi.PushNoteAsync(
                        "New tickets available",
                        messageBuilder.ToString(),
                        device.Id);
                }
                MarkMovieSent(showTimesSent, user);
            }

            await db.SaveChangesAsync();
        }

        private static bool MovieTitleContains(Movie movie, IEnumerable<MovieTitleToWatch> wantedTitles)
        {
            return wantedTitles.Any(wantedTitle => movie.Title.Contains(wantedTitle.Value, StringComparison.CurrentCultureIgnoreCase));
        }

        private static bool MovieAlreadySent(ShowTime showTime, User user)
        {
            return user.Notifications.Any(n => n.ShowTime == showTime);
        }

        private static void MarkMovieSent(IEnumerable<ShowTime> showTimesSent, User user)
        {
            foreach (ShowTime showTime in showTimesSent)
            {
                showTime.UsersUpdated.Add(new ShowTimeNotification(showTime, user));
            }
        }
    }
}
