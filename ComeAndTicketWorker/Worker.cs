using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.Extensions.Hosting;

namespace ComeAndTicketWorker
{
    [NLog.Targets.Target("MicrosoftExtensionsLoggingTarget")]
    public class MicrosoftExtensionsLoggingTarget<T> : NLog.Targets.TargetWithLayout
    {
        private readonly Microsoft.Extensions.Logging.ILogger<T> _logger;
        private readonly Dictionary<NLog.LogLevel, Microsoft.Extensions.Logging.LogLevel> _levelMapping = new Dictionary<NLog.LogLevel, Microsoft.Extensions.Logging.LogLevel>()
        {
            [NLog.LogLevel.Trace] = Microsoft.Extensions.Logging.LogLevel.Trace,
            [NLog.LogLevel.Debug] = Microsoft.Extensions.Logging.LogLevel.Debug,
            [NLog.LogLevel.Info] = Microsoft.Extensions.Logging.LogLevel.Information,
            [NLog.LogLevel.Warn] = Microsoft.Extensions.Logging.LogLevel.Warning,
            [NLog.LogLevel.Error] = Microsoft.Extensions.Logging.LogLevel.Error,
            [NLog.LogLevel.Fatal] = Microsoft.Extensions.Logging.LogLevel.Critical,
        };

        public MicrosoftExtensionsLoggingTarget(Microsoft.Extensions.Logging.ILogger<T> logger)
        {
            _logger = logger;
        }

        protected override void Write(NLog.LogEventInfo logEvent)
        {
            Microsoft.Extensions.Logging.LogLevel level;
            if (!_levelMapping.TryGetValue(logEvent.Level, out level))
            {
                level = Microsoft.Extensions.Logging.LogLevel.Information;
            }

            _logger.Log(level, 0, logEvent, logEvent.Exception, MessageFormatter);
        }
        
        private string MessageFormatter(NLog.LogEventInfo state, Exception error)
        {
            string logMessage = Layout.Render(state);
            return logMessage;
        }
    }

    public class Worker : BackgroundService
    {
        private static readonly NLog.ILogger _logger = NLog.LogManager.GetCurrentClassLogger();

        public Worker(Microsoft.Extensions.Logging.ILogger<Worker> logger)
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            string logLayout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${onexception:inner=${newline}${exception:format=toString}}";
            var logService = new MicrosoftExtensionsLoggingTarget<Worker>(logger)
            {
                Layout = logLayout,
            };

            // Rules for mapping loggers to targets
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logService);

            // Apply config
            NLog.LogManager.Configuration = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var db = new ComeAndTicketContext())
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.Info("Worker running at: {time}", DateTimeOffset.Now);

                        _logger.Info("Updating Drafthouse data from web");
                        await ComeAndTicketContext.UpdateDatabaseFromWebAsync(db);
                        _logger.Info("Writing to database");
                        await db.SaveChangesAsync();
                    } 
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Exception while updating drafthouse data from web");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(20));
                }
            }
        }
    }
}
