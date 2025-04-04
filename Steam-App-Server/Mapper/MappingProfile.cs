﻿using AutoMapper;
using SteamApp.Models;
using SteamApp.Models.Dto;
using SteamApp.Models.DTOs;
using SteamApp.Models.Models;

namespace SteamApp.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Basic mapping (AutoMapper automatically maps properties with the same name and type)
            CreateMap<ProductDto, Product>();
                //.ForMember(dest => dest.Id, opt => opt.Ignore()); //Ignore ID

            // Reverse mapping
            CreateMap<Product, ProductDto>();

            CreateMap<ItemDto, Item>();

            CreateMap<Item, ItemDto>();

            // Customize mappings (optional)
            //CreateMap<ProductDto, Product>()
            //    .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignore Id if it shouldn't be updated
            //    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name)) // Explicit mapping for clarity
            //    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Description + "Kotka")); // Custom mapping
        }
    }
}
