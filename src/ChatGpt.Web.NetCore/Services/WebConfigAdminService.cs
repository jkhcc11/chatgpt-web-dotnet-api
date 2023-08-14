﻿using ChatGpt.Web.IService;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Dtos;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.IRepository;
using System.Collections.Generic;
using AutoMapper;
using ChatGpt.Web.Entity;

namespace ChatGpt.Web.NetCore.Services
{
    /// <summary>
    /// 配置管理 服务实现
    /// </summary>
    public class WebConfigAdminService: BaseService, IWebConfigAdminService
    {
        private readonly IGptWebConfigRepository _gptWebConfigRepository;
        private readonly IMapper _mapper;
        private readonly IdGenerateExtension _idGenerateExtension;

        public WebConfigAdminService(IGptWebConfigRepository gptWebConfigRepository, IMapper mapper, 
            IdGenerateExtension idGenerateExtension)
        {
            _gptWebConfigRepository = gptWebConfigRepository;
            _mapper = mapper;
            _idGenerateExtension = idGenerateExtension;
        }

        /// <summary>
        /// 分页获取站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult<QueryPageDto<QueryPageWebConfigDto>>> QueryPageWebConfigAsync(QueryPageWebConfigInput input)
        {
            var query = await _gptWebConfigRepository.GetQueryableAsync();
            var pageResult = await _gptWebConfigRepository.QueryPageListAsync(query, input.Page, input.PageSize);
            var result = new QueryPageDto<QueryPageWebConfigDto>()
            {
                Total = pageResult.Total,
                Items =
                    _mapper.Map<IReadOnlyList<GptWebConfig>, IReadOnlyList<QueryPageWebConfigDto>>(pageResult
                        .Items)
            };
            return KdyResult.Success(result);
        }

        /// <summary>
        /// 创建/修改 站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> CreateAndUpdateWebConfigAsync(CreateAndUpdateWebConfigInput input)
        {
            if (input.Id.HasValue)
            {
                #region 修改
                var codeType = await _gptWebConfigRepository.GetEntityByIdAsync(input.Id.Value);
                codeType.SubDomainHost=input.SubDomainHost;
                codeType.Description = input.Description;
                codeType.HomeBtnHtml = input.HomeBtnHtml;

                codeType.Avatar = input.Avatar;
                codeType.Name = input.Name;

                codeType.FreeCode3 = input.FreeCode3;
                codeType.FreeCode4 = input.FreeCode4;
                codeType.FreeTimesWith3 = input.FreeTimesWith3;
                codeType.FreeTimesWith4 = input.FreeTimesWith4;

                codeType.Wximg = input.Wximg;
                codeType.Wxremark = input.Wxremark;
                codeType.SupportModel = input.SupportModel;
                await _gptWebConfigRepository.UpdateAsync(codeType);
                return KdyResult.Success();
                #endregion
            }

            #region 新增
            var dbCodeType = new GptWebConfig(_idGenerateExtension.GenerateId())
            {
                SubDomainHost = input.SubDomainHost,
                Description = input.Description,
                HomeBtnHtml= input.HomeBtnHtml,

                Avatar = input.Avatar,
                Name = input.Name,

                FreeCode3 = input.FreeCode3,
                FreeCode4 = input.FreeCode4,
                FreeTimesWith3 = input.FreeTimesWith3,
                FreeTimesWith4 = input.FreeTimesWith4,

                Wximg = input.Wximg,
                Wxremark = input.Wxremark,
                SupportModel = input.SupportModel,
            };

            await _gptWebConfigRepository.CreateAsync(dbCodeType);
            return KdyResult.Success();
            #endregion
        }

        /// <summary>
        /// 删除站点配置
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> DeleteWebConfigAsync(long id)
        {
            var entity = await _gptWebConfigRepository.GetEntityByIdAsync(id);
            var result = await _gptWebConfigRepository.DeleteAsync(entity);
            return result ? KdyResult.Success() : KdyResult.Error(KdyResultCode.Error, "操作失败");
        }
    }
}
