using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ComeAndTicketWebUi
{
    public class DetailsModel : PageModel
    {
        private readonly MaguSoft.ComeAndTicket.Core.Model.ComeAndTicketContext _context;

        public DetailsModel(MaguSoft.ComeAndTicket.Core.Model.ComeAndTicketContext context)
        {
            _context = context;
        }

        public Movie Movie { get; set; }
        public IList<ShowTime> ShowTimes { get; set; }
        public IList<ShowTime> FilteredShowTimes { get; set; }
        public IList<Market> Markets { get; set; }
        public string MarketFilter { get; set; }

        public async Task<IActionResult> OnGetAsync(string id, string marketFilter)
        {
            MarketFilter = marketFilter;

            if (id == null)
            {
                return NotFound();
            }

            Movie = await _context.Movies
                .Include(m => m.ShowTimes)
                    .ThenInclude(st => st.Theater)
                        .ThenInclude(t => t.Market)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Title == id);

            if (Movie == null)
            {
                return NotFound();
            }
            
            Markets = await _context.Markets
                .AsNoTracking()
                .ToListAsync();

            ShowTimes = Movie.ShowTimes
                .Where(st => 
                    st.Date >= DateTime.Now && 
                    st.SeatsLeft > 0)
                .ToList();
            if (marketFilter.Equals("all-movies"))
            {
                FilteredShowTimes = ShowTimes;
            }
            else 
            { 
                FilteredShowTimes = ShowTimes
                    .Where(st => st.Theater.Market.Name == marketFilter)
                    .ToList();
            }
            return Page();
        }
    }
}
