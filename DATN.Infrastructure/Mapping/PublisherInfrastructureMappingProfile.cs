using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DATN.EntityClasses;
using MyProject.Domain.Entities.Stores;

namespace MyProject.Infrastructure.Mapping
{
    public class PublisherInfrastructureMappingProfile : Profile
    {
       public PublisherInfrastructureMappingProfile()
        {
            CreateMap<PublisherEntity, Publisher>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
            CreateMap<Publisher, PublisherEntity>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        }
    }
}
