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
    public enum UserDetailsState
    {
        NotInitialized = 0b0_0000_0000,
        Initializing   = 0b0_0000_0001,
        Initialized    = 0b0_0000_0010,
        LoadingUser    = 0b0_0000_0100,
        LoadedUser     = 0b0_0000_1000,
        CreatedUser    = 0b0_0001_0000,
        LoadingMarkets = 0b0_0010_0000,
        LoadedMarkets  = 0b0_0100_0000,
        InvalidUser    = 0b0_1000_0000,
        SavedChanges   = 0b1_0000_0000,
    }

    public class UserDetailsBase : ComponentBase
    {
        [Inject]
        protected IComeAndTicketDataService DataService { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }

        public string Message { get; set; }

        public UserDetailsState State { get; private set; }

        public IEnumerable<Market> Markets { get; private set; }

        public User User { get; private set; }
        public string UserMarketName
        {
            get => User?.HomeMarket?.Name; 
            set
            {
                User.HomeMarket = Markets.FirstOrDefault(m => m.Name == value);
            }
        }
        
        public string MovieTitleToWatch { get; set; }
        public string DeviceNickname { get; set; }

        protected override async Task OnInitializedAsync()
        {
            State |= UserDetailsState.Initializing;
            try
            {
                await base.OnInitializedAsync();
            }
            finally
            {
                State &= ~UserDetailsState.Initializing;
                State |= UserDetailsState.Initialized;
            }
        }

        protected async Task HandleLogIn()
        {
            State &= ~UserDetailsState.InvalidUser;
            State |= UserDetailsState.LoadingUser;

            User = await DataService.GetUserAsync(UserName, Password);
            State &= ~UserDetailsState.LoadingUser;
            if (User == null)
            {
                Message = "Invalid user";
                State |= UserDetailsState.InvalidUser;
                return;
            }
            
            State |= UserDetailsState.LoadedUser;

            State |= UserDetailsState.LoadingMarkets;
            Markets = await DataService.GetMarketsAsync();
            State &= ~UserDetailsState.LoadingMarkets;
            State |= UserDetailsState.LoadedMarkets;

            if (User.HomeMarket == null)
            {
                User.HomeMarket = Markets.FirstOrDefault();
            }
        }

        protected async Task HandleRegister()
        {
            State &= ~UserDetailsState.InvalidUser;
            State |= UserDetailsState.LoadingUser;

            User = await DataService.RegisterUserAsync(UserName, Password);
            State &= ~UserDetailsState.LoadingUser;
            if (User == null)
            {
                Message = "Could not create user";
                State |= UserDetailsState.InvalidUser;
                return;
            }

            State |= UserDetailsState.CreatedUser;

            State |= UserDetailsState.LoadingMarkets;
            Markets = await DataService.GetMarketsAsync();
            State &= ~UserDetailsState.LoadingMarkets;
            State |= UserDetailsState.LoadedMarkets;

            if (User.HomeMarket == null)
            {
                User.HomeMarket = Markets.FirstOrDefault();
            }
        }

        protected async Task HandleSaveChanges()
        {
            await DataService.Save();
            State |= UserDetailsState.SavedChanges;
            Message = "Saved changes";
        }

        protected void HandleAddMovieToWatch()
        {
            MovieTitleToWatch movieTitleToWatch = new MovieTitleToWatch(MovieTitleToWatch);
            User.MovieTitlesToWatch.Add(movieTitleToWatch);
            MovieTitleToWatch = string.Empty;
        }

        protected void HandleRemoveMovieToWatch(MovieTitleToWatch movieTitleToWatch)
        {
            User.MovieTitlesToWatch.Remove(movieTitleToWatch);
        }

        protected void HandleAddDeviceNickname()
        {
            DeviceNickname deviceNickname = new DeviceNickname(DeviceNickname);
            User.DeviceNicknames.Add(deviceNickname);
            DeviceNickname = string.Empty;
        }

        protected void HandleRemoveDeviceNickname(DeviceNickname deviceNickname)
        {
            User.DeviceNicknames.Remove(deviceNickname);
        }
    }
}
