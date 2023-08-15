

using AutoMapper;
using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.NetCore
{
    public abstract class BaseService
    {
        protected IMapper BaseMapper;
        protected IdGenerateExtension BaseIdGenerate;

        protected BaseService(IMapper baseMapper, IdGenerateExtension baseIdGenerate)
        {
            BaseMapper = baseMapper;
            BaseIdGenerate = baseIdGenerate;
        }
    }
}
