using System.Collections.Generic;
using ChatGpt.Web.IService.ActivationCodeSys;
using System.Threading.Tasks;
using AutoMapper;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin;
using ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.Entity.ActivationCodeSys;
using System;
using System.Linq;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.NetCore.ActivationCodeSys
{
    /// <summary>
    /// 卡密管理 服务实现
    /// </summary>
    public class ActivationCodeAdminService : BaseService, IActivationCodeAdminService
    {
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IMapper _mapper;
        private readonly IdGenerateExtension _idGenerateExtension;

        public ActivationCodeAdminService(IActivationCodeRepository activationCodeRepository,
            IActivationCodeTypeV2Repository activationCodeTypeV2Repository,
            IMapper mapper, IdGenerateExtension idGenerateExtension)
        {
            _activationCodeRepository = activationCodeRepository;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
            _mapper = mapper;
            _idGenerateExtension = idGenerateExtension;
        }

        /// <summary>
        /// 分页获取卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult<QueryPageDto<QueryPageCodeTypeDto>>> QueryPageCodeTypeAsync(QueryPageCodeTypeInput input)
        {
            var query = await _activationCodeTypeV2Repository.GetQueryableAsync();
            var pageResult = await _activationCodeTypeV2Repository.QueryPageListAsync(query, input.Page, input.PageSize);
            var result = new QueryPageDto<QueryPageCodeTypeDto>()
            {
                Total = pageResult.Total,
                Items =
                    _mapper.Map<IReadOnlyList<ActivationCodeTypeV2>, IReadOnlyList<QueryPageCodeTypeDto>>(pageResult
                        .Items)
            };
            return KdyResult.Success(result);
        }

        /// <summary>
        /// 创建/修改 卡密类型
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> CreateAndUpdateCodeTypeAsync(CreateAndUpdateCodeTypeInput input)
        {
            var supportModelItems = new List<SupportModeItem>();
            foreach (var item in input.SupportModelGroupNameItems)
            {
                supportModelItems.AddRange(item.GetSupportModeItemsByGroupName());
            }

            if (input.Id.HasValue)
            {
                //修改
                #region 修改
                var codeType = await _activationCodeTypeV2Repository.GetEntityByIdAsync(input.Id.Value);
                codeType.CodeName = input.CardTypeName;
                codeType.ValidDays = input.ValidDays;
                codeType.IsEveryDayResetCount = input.IsEveryDayResetCount;
                if (input.IsEveryDayResetCount)
                {
                    codeType.MaxCountItems = input.LimitItems
                        .Select(a => new MaxCountItem(a.SupportModelGroupName, a.EveryDayTimes)
                        {
                            MaxHistoryCount = a.MaxHistoryCount,
                            MaxRequestToken = a.MaxRequestTokens,
                            MaxResponseToken = a.MaxResponseTokens ?? 500
                        })
                        .ToList();
                }

                codeType.SupportModelItems = supportModelItems;
                await _activationCodeTypeV2Repository.UpdateAsync(codeType);
                return KdyResult.Success(); 
                #endregion
            }

            #region 新增
            var dbCodeType = new ActivationCodeTypeV2(_idGenerateExtension.GenerateId()
                , input.CardTypeName
                , supportModelItems)
            {
                ValidDays = input.ValidDays,
                IsEveryDayResetCount = input.IsEveryDayResetCount
            };

            if (input.IsEveryDayResetCount)
            {
                dbCodeType.MaxCountItems = input.LimitItems
                    .Select(a => new MaxCountItem(a.SupportModelGroupName, a.EveryDayTimes)
                    {
                        MaxHistoryCount = a.MaxHistoryCount,
                        MaxRequestToken = a.MaxRequestTokens,
                        MaxResponseToken = a.MaxResponseTokens ?? 500
                    })
                    .ToList();
            }

            await _activationCodeTypeV2Repository.CreateAsync(new List<ActivationCodeTypeV2>()
            {
                dbCodeType
            });
            return KdyResult.Success(); 
            #endregion
        }

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> DeleteCodeTypeAsync(long id)
        {
            var entity = await _activationCodeTypeV2Repository.GetEntityByIdAsync(id);
            var result = await _activationCodeTypeV2Repository.DeleteAsync(entity);
            return result ? KdyResult.Success() : KdyResult.Error(KdyResultCode.Error, "操作失败");
        }

        /// <summary>
        /// 分页获取卡密
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult<QueryPageDto<QueryPageActivationCodeDto>>> QueryPageActivationCodeAsync(QueryPageActivationCodeInput input)
        {
            var query = await _activationCodeRepository.GetQueryableAsync();
            var pageResult = await _activationCodeRepository.QueryPageListAsync(query, input.Page, input.PageSize);
            var result = new QueryPageDto<QueryPageActivationCodeDto>()
            {
                Total = pageResult.Total,
                Items =
                    _mapper.Map<IReadOnlyList<ActivationCode>, IReadOnlyList<QueryPageActivationCodeDto>>(pageResult
                        .Items)
            };
            return KdyResult.Success(result);
        }

        /// <summary>
        /// 批量创建卡密
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> BatchCreateActivationCodeAsync(BatchCreateActivationCodeInput input)
        {
            var codeType = await _activationCodeTypeV2Repository.GetEntityByIdAsync(input.CodeTypeId);
            var dbActivationCode = new List<ActivationCode>();
            for (var i = 0; i < input.Number; i++)
            {
                var code = Guid.NewGuid().ToString();
                dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                    code, codeType.Id));
            }

            await _activationCodeRepository.CreateAsync(dbActivationCode);
            return KdyResult.Success($"数量：{dbActivationCode.Count}");
        }

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult> DeleteActivationCodeAsync(long id)
        {
            var entity = await _activationCodeRepository.GetEntityByIdAsync(id);
            var result = await _activationCodeRepository.DeleteAsync(entity);
            return result ? KdyResult.Success() : KdyResult.Error(KdyResultCode.Error, "操作失败");
        }
    }
}
