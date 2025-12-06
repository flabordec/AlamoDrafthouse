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
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using MaguSoft.ComeAndTicket.Core.Migrations;
using MaguSoft.ComeAndTicket.Core.ExtensionMethods;
using AutoMapper;
using static System.Collections.Specialized.BitVector32;

namespace MaguSoft.ComeAndTicket.Console
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static async Task<int> Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
#else
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
#endif
                .AddUserSecrets(typeof(Program).Assembly, optional: true)
                .AddEnvironmentVariables();

            if (args != null)
            {
                builder.AddCommandLine(args);
            }

            var config = builder.Build();

            ConfigureLogging();

            try
            {
                bool useInMemoryDatabase = config.GetValue<bool>("UseInMemoryDatabase");
                using (var context = new ComeAndTicketContext(useInMemoryDatabase))
                {
                    // await context.Database.EnsureDeletedAsync();
                    await context.Database.EnsureCreatedAsync();

                    var user = await context.GetUserFromDbAsync("flabordec");
                    if (user == null)
                    {
                        user = new User() { UserName = "flabordec" };
                        context.Users.Add(user);
                    }

                    IConfigurationSection notificationsSection = config.GetSection("Notifications");
                    var notifications = notificationsSection.Get<NotificationConfiguration[]>();
                    if (notifications == null)
                        throw new Exception("The notification configurations must be specified");

                    var pushbulletAccessToken = config.GetValue<string>("PushbulletAccessToken");
                    if (string.IsNullOrEmpty(pushbulletAccessToken))
                        throw new Exception("The pushbullet API token must be specified");
                    var pushbulletApi = new Pushbullet(pushbulletAccessToken);

                    int returnCode = await RunAndReturnExitCodeAsync(context, user, notifications, pushbulletApi);

                    return returnCode;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Uncaught exception while running");
                return -1;
            }
        }

        private static void ConfigureLogging()
        {
            var logConfig = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            string logLayout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${onexception:inner=${newline}${exception:format=toString}}";
            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                Layout = logLayout,
                FileName = "output.log",
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                MaxArchiveFiles = 30,
                ArchiveSuffixFormat = "{1:yyyyMMdd}_{0:00}",
                ArchiveAboveSize = 512 * 1024, // 512 KB
            };
            var logConsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                Layout = logLayout,
            };

            // Rules for mapping loggers to targets
            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);

            // Apply config
            LogManager.Configuration = logConfig;
        }

        private static async Task<int> RunAndReturnExitCodeAsync(ComeAndTicketContext context, User user, IEnumerable<NotificationConfiguration> notifications, Pushbullet pushbulletApi)
        {
            var marketsToUpdate =
                from n in notifications
                from m in n.Markets
                select m.Name;

            _logger.Info("Updating Drafthouse data from web");
            var markets = await context.GetMarketsFromWebAsync(marketsToUpdate);

            _logger.Info("Pushing notifications");
            await PushMoviesAsync(user, markets, notifications, pushbulletApi);

            await context.SaveChangesAsync();

            return 0;
        }

        private static async Task PushMoviesAsync(User user, IEnumerable<Market> markets, IEnumerable<NotificationConfiguration> notifications, Pushbullet pushbulletApi)
        {
            var marketsByName = markets.ToDictionary(m => m.Name, StringComparer.CurrentCultureIgnoreCase);

            bool newShowsAvailable = false;

            var messageBuilder = new StringBuilder();
            foreach (var notification in notifications)
            {
                foreach (var marketNotificationConfiguration in notification.Markets)
                {
                    var marketName = marketNotificationConfiguration.Name;
                    var market = marketsByName[marketName];
                    HashSet<string> cinemaNamesToNotify = new HashSet<string>(marketNotificationConfiguration.Cinemas, StringComparer.CurrentCultureIgnoreCase);

                    var presentationsToNotify = new HashSet<Presentation>();

                    foreach (var showTitle in notification.Titles)
                    {
                        var presentationsToNotifyByTitle =
                            from p in market.Presentations
                            where p.Show.Title.Contains(showTitle, StringComparison.CurrentCultureIgnoreCase)
                            select p;
                        presentationsToNotify.UnionWith(presentationsToNotifyByTitle);
                    }
                    foreach (var superTitle in notification.SuperTitles)
                    {
                        var presentationsToNotifyBySuperTitle =
                            from p in market.Presentations
                            where p.SuperTitle != null
                            where p.SuperTitle.Name.Contains(superTitle, StringComparison.CurrentCultureIgnoreCase)
                            select p;
                        presentationsToNotify.UnionWith(presentationsToNotifyBySuperTitle);
                    }

                    var potentialNotificationsByCinema = (
                        from presentation in presentationsToNotify
                        from session in presentation.Sessions
                        select (presentation, session)
                        )
                        .GroupBy(s => s.session.Cinema.Name);

                    foreach (var potentialNotificationPairs in potentialNotificationsByCinema)
                    {
                        bool addedMessageForCinema = false;
                        var cinemaName = potentialNotificationPairs.Key;

                        var potentialNotificationsByPresentationTitle = potentialNotificationPairs.GroupBy(p =>
                            {
                                var presentation = p.presentation;
                                string presentationTitle;
                                if (presentation.SuperTitle != null)
                                {
                                    presentationTitle = $"{presentation.SuperTitle.Name} - {presentation.Show.Title}";
                                }
                                else
                                {
                                    presentationTitle = presentation.Show.Title;
                                }
                                return presentationTitle;
                            });

                        foreach (var potentialNotificationByPresentationTitle in potentialNotificationsByPresentationTitle)
                        {
                            bool addedMessageForPresentation = false;
                            var presentationTitle = potentialNotificationByPresentationTitle.Key;
                            foreach (var potentialNotification in potentialNotificationByPresentationTitle)
                            {
                                var presentation = potentialNotification.presentation;
                                var session = potentialNotification.session;

                                if (Session.StringToTicketsSaleStatus(session.TicketStatus) != TicketsStatus.OnSale)
                                    continue;
                                if (notification.After != null && session.ShowTimeUtc < notification.After.Value.ToDateTime(new TimeOnly(0, 0)))
                                    continue;
                                if (notification.Before != null && session.ShowTimeUtc > notification.Before.Value.ToDateTime(new TimeOnly(0, 0)))
                                    continue;
                                if (notification.DayOfWeek.Any() && !notification.DayOfWeek.Contains(session.ShowTimeUtc.DayOfWeek))
                                    continue;
                                if (!cinemaNamesToNotify.Contains("All") && !cinemaNamesToNotify.Contains(session.Cinema.Name))
                                    continue;
                                if (user.SessionsNotified.Contains(session))
                                    continue;

                                if (!addedMessageForCinema)
                                {
                                    messageBuilder.AppendLine("=====================");
                                    messageBuilder.AppendLine(cinemaName);
                                    addedMessageForCinema = true;
                                }

                                if (!addedMessageForPresentation)
                                {
                                    messageBuilder.AppendLine("---------------------");
                                    messageBuilder.AppendLine($" - {presentationTitle}");
                                    addedMessageForPresentation = true;
                                }

                                var showTimeLocal = session.ShowTimeUtc.ToLocalTime();
                                var showTimeLocalString = showTimeLocal.ToString("ddd d MMM h:mm tt");
                                messageBuilder.AppendLine($"     - {showTimeLocalString} (Buy: {session.TicketsUrl} )");
                                user.SessionsNotified.Add(session);

                                newShowsAvailable = true;
                            }
                        }
                    }
                }
            }

            if (newShowsAvailable)
            {
                await pushbulletApi.PushNoteAsync(
                    "New shows available",
                    messageBuilder.ToString(),
                    (IDevice?)null);
            }
        }
    }
}
