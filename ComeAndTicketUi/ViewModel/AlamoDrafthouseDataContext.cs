using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Xml.Linq;
using com.magusoft.drafthouse.ViewModel;
using NLog;
using GalaSoft.MvvmLight;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace MaguSoft.ComeAndTicket.Ui.ViewModel
{
    class AlamoDrafthouseDataContext : ViewModelBase
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #region Data
        private string _titleFilter;
        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                Set(ref _titleFilter, value);
                RefreshFilters();
            }
        }

        private DateTime? _dateFilter;
        public DateTime? DateFilter
        {
            get { return _dateFilter; }
            set
            {
                Set(ref _dateFilter, value);
                RefreshFilters();
            }
        }

        private readonly ObservableCollection<Market> _markets;
        public ObservableCollection<Market> Markets
        {
            get
            {
                return _markets;
            }
        }
        #endregion

        #region State
        private string _status;
        public string Status
        {
            get { return _status; }
            set { Set(ref _status, value); }
        }
        #endregion

        public AlamoDrafthouseDataContext()
        {
            _titleFilter = string.Empty;
            _dateFilter = null;

            _markets = new ObservableCollection<Market>();
        }

        private bool FilterShowTimes(object obj)
        {
            Debug.Assert(obj is ShowTime);
            ShowTime showTime = (ShowTime)obj;

            return !DateFilter.HasValue || showTime.Date.Value.Date.Equals(DateFilter.Value.Date);
        }

        private bool MovieTitleContains(Movie movie, string wantedTitle)
        {
            return movie.Title.ToLowerInvariant().Contains(
                wantedTitle.ToLowerInvariant());
        }

        private bool FilterMovie(object obj)
        {
            Debug.Assert(obj is Movie);
            Movie currentMovie = (Movie)obj;
            bool titleFilter = MovieTitleContains(currentMovie, TitleFilter);
            // No date time filter or...
            // any showtime matches the filter date
            bool showTimesFilter =
                !DateFilter.HasValue ||
                currentMovie.ShowTimes.Any(s => s.Date.Value.Date.Equals(DateFilter.Value.Date));
            return titleFilter && showTimesFilter;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Status = string.Format("Initializing");

                _logger.Info("Reading markets");
                await OnReloadMarketsAsync();
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";

                _logger.Error(ex, "Exception while initializing");
                throw;
            }
            finally
            {
                Status = string.Format("Finished Initializing");
            }
        }

        private async Task OnReloadMarketsAsync()
        {
            using (var db = new ComeAndTicketContext())
            {
                try
                {
                    Status = "Reloading markets";
                    await db.Markets
                        .Include(m => m.Theaters)
                            .ThenInclude(t => t.ShowTimes)
                        .LoadAsync();

                    foreach (var market in db.Markets)
                    {
                        Markets.Add(market);
                    }
                }
                finally
                {
                    Status = "Markets reloaded";
                }
            }
        }

        private void RefreshFilters()
        {
            foreach (Market market in Markets)
            {
                foreach (Theater theater in market.Theaters)
                {
                    CollectionViewSource.GetDefaultView(theater.ShowTimes).Refresh();
                    foreach (ShowTime showTime in theater.ShowTimes)
                    {
                        CollectionViewSource.GetDefaultView(showTime).Refresh();
                    }
                }
            }
        }
    }
}
