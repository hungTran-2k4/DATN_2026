using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MyProject.Application.Models.Store;
using MyProject.Domain.Entities.Stores;

namespace MyProject.Application.Mapping
{
    public class GameProfile : Profile
    {
        public GameProfile()
        {
            CreateMap<Game, GameDto>();

        }
    }
}