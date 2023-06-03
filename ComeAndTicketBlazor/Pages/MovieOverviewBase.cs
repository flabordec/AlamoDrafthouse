﻿using ComeAndTicketBlazor.Data;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComeAndTicketBlazor.Pages
{
    [Flags]
    public enum MovieOverviewState
    {
        NotInitialized     = 0b0000,
        LoadingMarkets     = 0b0001,
        LoadedMarkets      = 0b0010,
        LoadingMovies   = 0b0100,
        LoadedMovies    = 0b1000,
    }

    public class MovieOverviewBase : ComponentBase
    {
        [Inject]
        protected IComeAndTicketDataService DataService { get; set; }

        public MovieOverviewState State { get; private set; }

        private readonly HashSet<Movie> _movies = new HashSet<Movie>();
        public IEnumerable<Movie> Movies => _movies;

        public IEnumerable<Market> Markets { get; private set; }

        public Market SelectedMarket { get; set; }
        public string SelectedMarketName
        {
            get => SelectedMarket?.Name;
            set
            {
                if (SelectedMarketName != value)
                {
                    SelectedMarket = Markets.FirstOrDefault(m => m.Name == value);
                }
            }
        }

        public Cinema SelectedTheater { get; set; }
        public string SelectedTheaterUrl
        {
            get => SelectedTheater?.Url ?? "<All>";
            set
            {
                if (SelectedTheaterUrl != value)
                {
                    SelectedTheater = SelectedMarket.Cinemas.FirstOrDefault(t => t.Url == value);
                }
            }
        }

        public Movie SelectedMovie { get; set; }
        public string SelectedMovieTitle 
        {
            get => SelectedMovie?.Title;
            set
            {
                if (SelectedMovieTitle != value)
                {
                    SelectedMovie = Movies.FirstOrDefault(m => m.Title == value);
                }
            }
        }

        public string MovieTitleFilter { get; set; }

        protected async Task SelectedMovieChanged(ChangeEventArgs args)
        {
            await Task.Delay(0);
        }

        protected async Task SelectedMarketChangedAsync(ChangeEventArgs args) => 
            await ReloadMoviesAsync((string)args.Value, SelectedTheaterUrl, MovieTitleFilter);
        protected async Task SelectedTheaterChangedAsync(ChangeEventArgs args) =>
            await ReloadMoviesAsync(SelectedMarketName, (string)args.Value, MovieTitleFilter);
        protected async Task SelectedMovieTitleFilterChangedAsync(ChangeEventArgs args) =>
            await ReloadMoviesAsync(SelectedMarketName, SelectedTheaterUrl, (string)args.Value);

        protected async Task ReloadMoviesAsync(string marketName, string theaterUrl, string movieTitleFilter)
        {
            State |= MovieOverviewState.LoadingMovies;
            try
            {
                SelectedMarketName = marketName;
                SelectedTheaterUrl = theaterUrl;
                MovieTitleFilter = movieTitleFilter;
                
                IEnumerable<Movie> movies = await DataService.GetMoviesForMarketAsync(SelectedMarket, SelectedTheater, null, MovieTitleFilter);
                _movies.Clear();
                _movies.UnionWith(movies);
            }
            finally
            {
                State &= ~MovieOverviewState.LoadingMovies;
                State |= MovieOverviewState.LoadedMovies;

                StateHasChanged();
            }
        }

        

        protected async override Task OnInitializedAsync()
        {
            State |= MovieOverviewState.LoadingMarkets;
            try
            {
                await base.OnInitializedAsync();

                Markets = await DataService.GetMarketsAsync();
                
                SelectedMarket = Markets.FirstOrDefault();
                await ReloadMoviesAsync(SelectedMarketName, SelectedTheaterUrl, MovieTitleFilter);

                SelectedTheater = null;
            }
            finally
            {
                State &= ~MovieOverviewState.LoadingMarkets;
                State |= MovieOverviewState.LoadedMarkets;
            }
        }
    }
}
