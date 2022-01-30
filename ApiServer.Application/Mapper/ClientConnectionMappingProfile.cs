using ApiServer.Core.Entities;
using ApiServer.Infrastructure.Models;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiServer.Application.Mapper
{
    class ClientConnectionMappingProfile : Profile
    {
        public ClientConnectionMappingProfile()
        {

            CreateMap<ApiServer.Infrastructure.Models.ClientConnection, DeviceConnectionStatus>()
                .ForMember(dest => dest.SshHost, opt => opt.MapFrom(src => src.SshIp))
                .ForMember(dest => dest.State, opt => opt.MapFrom(src => (int)src.Status));
        }
    }
}
