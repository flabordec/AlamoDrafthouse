using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ComeAndTicketWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var db = new ComeAndTicketContext())
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                        _logger.LogInformation("Updating Drafthouse data from web");
                        await ComeAndTicketContext.UpdateDatabaseFromWebAsync(db);
                        _logger.LogInformation("Writing to database");
                        await db.SaveChangesAsync();
                    } 
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Exception while updating drafthouse data from web");
                    }
                }
            }
        }
    }
}
