using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaguSoft.ComeAndTicket.Core.Model
{
    public static class MapperSingleton
    {
        private static Lazy<IMapper> _instance;
        public static IMapper Instance => _instance.Value;

        static MapperSingleton()
        {
            _instance = new Lazy<IMapper>(() => ConfigureMappings());
        }


        private static IMapper ConfigureMappings()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Session, Session>();
            });
            return config.CreateMapper();
        }
    }
}
