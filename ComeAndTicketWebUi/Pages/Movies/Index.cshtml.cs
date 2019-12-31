using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MaguSoft.ComeAndTicket.Core.Model;

namespace ComeAndTicketWebUi
{
    public class IndexModel : PageModel
    {
        private readonly MaguSoft.ComeAndTicket.Core.Model.ComeAndTicketContext _context;

        public IndexModel(MaguSoft.ComeAndTicket.Core.Model.ComeAndTicketContext context)
        {
            _context = context;
        }

        public string TitleFilter { get; set; }
        public string MarketFilter { get; set; }

        public string TitleSort { get; set; }

        public IList<Market> Markets { get; set; }
        public IList<Movie> Movies { get;set; }

        public Market SelectedMarket { get; set; }

        public async Task OnGetAsync(string sortOrder, string titleFilter, string marketFilter)
        {
            MarketFilter = marketFilter;
            TitleFilter = titleFilter;

            Markets = _context.Markets
                .AsNoTracking()
                .ToList();

            if (string.IsNullOrEmpty(marketFilter))
            {
                marketFilter = Markets.First().Name;
                MarketFilter = marketFilter;
            }

            IQueryable<Movie> query = _context.Movies
                .Include(m => m.ShowTimes)
                .AsNoTracking();
            switch (sortOrder)
            {
                case null:
                case "title":
                    TitleSort = "title_desc";
                    query = query.OrderBy(m => m.Title);
                    break;
                case "title_desc":
                    TitleSort = "title";
                    query = query.OrderByDescending(m => m.Title);
                    break;
            }

            query = query.Where(m => 
                m.ShowTimes.Any(st => 
                    st.Date >= DateTime.UtcNow && 
                    st.SeatsLeft > 0 &&
                    st.Theater.Market.Name == marketFilter
                ));
            
            var moviesPreFilter = await query.ToListAsync();
            if (!string.IsNullOrEmpty(titleFilter))
            {
                Movies = moviesPreFilter.Where(m => m.Title.Contains(titleFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
            else
            {
                Movies = moviesPreFilter;
            }
        }
    }
}
