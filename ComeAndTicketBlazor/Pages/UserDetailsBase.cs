using ComeAndTicketBlazor.Data;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ComeAndTicketBlazor.Pages
{
    [Flags]
    public enum UserDetailsState
    {
        NotInitialized = 0b0_0000_0000,
        Initializing   = 0b0_0000_0001,
        Initialized    = 0b0_0000_0010,
        SavedChanges   = 0b1_0000_0000,
    }

    public class UserDetailsBase : ComponentBase
    {
        [Inject]
        protected IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject]
        protected IComeAndTicketDataService DataService { get; set; }

        public string UserName { get; set; }

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
            await base.OnInitializedAsync();
            try
            {
                // Set the user to determine if they are logged in
                ClaimsPrincipal claimsPrincipal = HttpContextAccessor.HttpContext.User;

                if (claimsPrincipal.Identity.Name != null)
                {
                    var eMail = claimsPrincipal.FindFirst(ClaimTypes.Email);
                    if (eMail != null)
                    {
                        User = await DataService.GetUserAsync(eMail.Value);
                        Markets = await DataService.GetMarketsAsync();
                        if (User.HomeMarket == null)
                        {
                            User.HomeMarket = Markets.FirstOrDefault();
                        }
                    }
                }
            }
            finally
            {
                State &= ~UserDetailsState.Initializing;
                State |= UserDetailsState.Initialized;
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
