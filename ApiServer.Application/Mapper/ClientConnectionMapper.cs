using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Application.Mapper
{
    public class ClientConnectionMapper
    {
        private static readonly Lazy<IMapper> Lazy = new Lazy<IMapper>(() => {
            var config = new MapperConfiguration(cfg => {
                cfg.AddProfile<ClientConnectionMappingProfile>();
            });
            var mapper = config.CreateMapper();
            return mapper;
        });
        public static IMapper Mapper => Lazy.Value;
    }
}
