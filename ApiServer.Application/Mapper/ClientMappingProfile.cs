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
            CreateMap<Entities.Client, Infrastructure.Models.Client>().ReverseMap();
        }
    }
}
