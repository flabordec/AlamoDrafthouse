using ComeAndTicketBlazor.Data;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComeAndTicketBlazor.Pages
{
    [Flags]
    public enum MovieDetailsState
    {
        NotInitialized = 0b000,
        LoadingMovie   = 0b010,
        LoadedMovie    = 0b100,
    }

    public class MovieDetailsBase : ComponentBase
    {
        [Inject]
        protected IComeAndTicketDataService DataService { get; set; }

        [Parameter]
        public string MovieTitle { get; set; }

        [Parameter]
        public string MarketName { get; set; }

        [Parameter]
        public string TheaterName { get; set; }

        public IEnumerable<ShowTime> ShowTimes { get; set; }

        public MovieDetailsState State { get; private set; }

        protected async override Task OnInitializedAsync()
        {
            State |= MovieDetailsState.LoadingMovie;
            try
            {
                await base.OnInitializedAsync();

                ShowTimes = await DataService.GetShowTimesAsync(MovieTitle, MarketName, TheaterName);
            }
            finally
            {
                State &= ~MovieDetailsState.LoadingMovie;
                State |= MovieDetailsState.LoadedMovie;
            }
        }
    }
}
