using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.TotalQuantity, opt => opt.MapFrom(s => s.Items.Sum(i => i.Quantity)));

        CreateMap<Product, ProductDetailDto>()
            .ForMember(d => d.TotalQuantity, opt => opt.MapFrom(s => s.Items.Sum(i => i.Quantity)))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));

        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        CreateMap<Item, ItemDto>();
        CreateMap<CreateItemDto, Item>();
    }
}
