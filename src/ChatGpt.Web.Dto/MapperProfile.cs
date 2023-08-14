using AutoMapper;
using ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin;
using ChatGpt.Web.Entity.ActivationCodeSys;

namespace ChatGpt.Web.Dto
{
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            CreateMap<ActivationCodeTypeV2, QueryPageCodeTypeDto>();
            CreateMap<ActivationCode, QueryPageActivationCodeDto>();
        }
    }
}
