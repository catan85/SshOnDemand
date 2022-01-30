using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApiServer.Infrastructure;

namespace ApiServer.Application.Mapper
{
    public class ClientMappingProfile : Profile
    {
        public ClientMappingProfile()
        {
            CreateMap<Core.Entities.Client, Infrastructure.Models.Client>().ReverseMap();
            CreateMap<Requests.ManagementRequestAddDevice, Infrastructure.Models.Client>()
                .AfterMap((s, d) => {
                    d.IsDevice = true;
                    d.IsDeveloper = false;
                    });
            CreateMap<Requests.ManagementRequestAddDeveloper, Infrastructure.Models.Client>()
                .AfterMap((s, d) => {
                    d.IsDevice = false;
                    d.IsDeveloper = true;
                });
            
        }
    }
}
