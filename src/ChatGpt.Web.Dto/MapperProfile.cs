using AutoMapper;
using ChatGpt.Web.Dto.Dtos;
using ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin;
using ChatGpt.Web.Entity;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.Dto
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ActivationCodeTypeV2, QueryPageCodeTypeDto>()
                .ForMember(dest => dest.CardTypeName, target => target.MapFrom(source => source.CodeName));
            CreateMap<ActivationCode, QueryPageActivationCodeDto>();
            CreateMap<GptWebConfig, QueryPageWebConfigDto>();
        }
    }
}
