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
           
            var map = CreateMap<ApiServer.Infrastructure.Models.ClientConnection, DeviceConnectionStatus>();

            // ingnore all existing binding of property
            map.ForAllMembers(opt => opt.Ignore());
            map.ForMember(dest => dest.State, opt => opt.MapFrom(src => (int)src.Status));
            map.ForMember(dest => dest.SshHost, opt => opt.MapFrom(src => src.SshIp));
            map.ForMember(dest => dest.SshPort, opt => opt.MapFrom(src => src.SshPort.Value));
            map.ForMember(dest => dest.SshForwarding, opt => opt.MapFrom(src => src.SshForwarding.Value));
            map.ForMember(dest => dest.SshUser, opt => opt.MapFrom(src => src.SshUser));
            
        }
    }
}
