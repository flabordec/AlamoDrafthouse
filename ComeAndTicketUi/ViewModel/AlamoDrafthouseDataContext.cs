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

namespace MaguSoft.ComeAndTicket.Ui.ViewModel
{
    class AlamoDrafthouseDataContext : ViewModelBase
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #region Data
        private string mTitleFilter;
        public string TitleFilter
        {
            get { return mTitleFilter; }
            set
            {
                Set(ref mTitleFilter, value);
                RefreshFilters();
            }
        }

        private DateTime? mDateFilter;
        public DateTime? DateFilter
        {
            get { return mDateFilter; }
            set
            {
                Set(ref mDateFilter, value);
                RefreshFilters();
            }
        }

        private readonly ObservableCollection<Market> mMarkets;
        public ObservableCollection<Market> Markets
        {
            get
            {
                return mMarkets;
            }
        }
        #endregion

        #region State
        private string mStatus;
        public string Status
        {
            get { return mStatus; }
            set { Set(ref mStatus, value); }
        }
        #endregion

        public AlamoDrafthouseDataContext()
        {
            mTitleFilter = string.Empty;
            mDateFilter = null;

            mMarkets = new ObservableCollection<Market>();
            Markets.CollectionChanged += Markets_CollectionChanged;
        }

        private void Markets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Market market in e.OldItems)
                    market.Theaters.CollectionChanged -= Theaters_CollectionChanged;
            }

            if (e.NewItems != null)
            {
                foreach (Market market in e.NewItems)
                    market.Theaters.CollectionChanged += Theaters_CollectionChanged;
            }
        }

        private void Theaters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Theater theater in e.OldItems)
                    theater.Movies.CollectionChanged -= Movies_CollectionChanged;
            }

            if (e.NewItems != null)
            {
                foreach (Theater theater in e.NewItems)
                {
                    theater.Movies.CollectionChanged += Movies_CollectionChanged;
                    CollectionViewSource.GetDefaultView(theater.Movies).Filter = FilterMovie;
                }
            }
        }

        private void Movies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Movie movie in e.NewItems)
                {
                    CollectionViewSource.GetDefaultView(movie.ShowTimes).Filter = FilterShowTimes;
                }
            }
        }

        private bool FilterShowTimes(object obj)
        {
            Debug.Assert(obj is ShowTime);
            ShowTime showTime = (ShowTime)obj;

            return !DateFilter.HasValue || showTime.MyShowTime.Value.Date.Equals(DateFilter.Value.Date);
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
                currentMovie.ShowTimes.Any(s => s.MyShowTime.Value.Date.Equals(DateFilter.Value.Date));
            return titleFilter && showTimesFilter;
        }

        public async Task InitializeAsync()
        {
            try
            {
                Status = string.Format("Initializing");

                logger.Info("Reading markets");
                await OnReloadMarketsAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception while initializing");
                throw;
            }
            finally
            {
                Status = string.Format("Finished Initializing");
            }
        }

        private async Task OnReloadMarketsAsync()
        {
            try
            {
                Status = "Reloading markets";
                var markets = await Market.LoadAllMarketsAsync();
                foreach (var market in markets)
                {
                    Markets.Add(market);
                }
            } 
            finally
            {
                Status = "Markets reloaded";
            }
        }

        private void RefreshFilters()
        {
            foreach (Market market in Markets)
            {
                foreach (Theater theater in market.Theaters)
                {
                    CollectionViewSource.GetDefaultView(theater.Movies).Refresh();
                    foreach (Movie movie in theater.Movies)
                    {
                        CollectionViewSource.GetDefaultView(movie.ShowTimes).Refresh();
                    }
                }
            }
        }
    }
}
