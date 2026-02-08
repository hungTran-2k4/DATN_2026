using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using MyProject.Domain.Entities.Stores;
using MyProject.Application.Models.Store;

namespace MyProject.Application.Mapping
{
    public class PublisherProfile : Profile
    {
        public PublisherProfile()
        {
            CreateMap<Publisher, PublisherBaseRespone>();
            CreateMap<PublisherBaseRespone, Publisher>();
        }
    }
}
