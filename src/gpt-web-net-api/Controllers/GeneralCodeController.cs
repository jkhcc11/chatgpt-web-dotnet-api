using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.Dto.Dtos;
using ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IService;
using ChatGpt.Web.IService.ActivationCodeSys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// 生成Code
    /// </summary>
    [ApiController]
    [Route("config-v2")]
    [Authorize(Roles = nameof(CommonExtension.CommonRoleName.Root))]
    public class GeneralCodeController : BaseController
    {
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IActivationCodeAdminService _activationCodeAdminService;
        private readonly IWebConfigAdminService _webConfigAdminService;
        public GeneralCodeController(IActivationCodeAdminService activationCodeAdminService,
            IWebConfigAdminService webConfigAdminService,
            IActivationCodeTypeV2Repository activationCodeTypeV2Repository)
        {
            _activationCodeAdminService = activationCodeAdminService;
            _webConfigAdminService = webConfigAdminService;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        [HttpGet("init-data")]
        [AllowAnonymous]
        public async Task<KdyResult> DeleteCodeTypeAsync()
        {
            return await _activationCodeAdminService.InitRootCardNoInfoAsync();
        }

        #region 卡类型
        /// <summary>
        /// 删除卡类型
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete-card-type")]
        public async Task<KdyResult> DeleteCodeTypeAsync(long id)
        {
            return await _activationCodeAdminService.DeleteCodeTypeAsync(id);
        }

        /// <summary>
        /// 查询卡类型
        /// </summary>
        /// <returns></returns>
        [HttpGet("query-card-type")]
        public async Task<KdyResult<QueryPageDto<QueryPageCodeTypeDto>>> QueryPageCodeTypeAsync([FromQuery] QueryPageCodeTypeInput input)
        {
            return await _activationCodeAdminService.QueryPageCodeTypeAsync(input);
        }

        /// <summary>
        /// 创建/更新卡类型
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-update-card-type")]
        public async Task<KdyResult> CreateAndUpdateCodeTypeAsync(CreateAndUpdateCodeTypeInput input)
        {
            if (input.SupportModelGroupNameItems.Any() == false)
            {
                return KdyResult.Error(KdyResultCode.ParError, "无效支持模型");
            }

            if (input.LimitItems.Any() == false)
            {
                return KdyResult.Error(KdyResultCode.ParError, "限制未配置限制");
            }

            return await _activationCodeAdminService.CreateAndUpdateCodeTypeAsync(input);
        }
        #endregion

        #region 卡密
        /// <summary>
        /// 创建卡密
        /// </summary>
        /// <returns></returns>
        [HttpPost("batch-create-activation-code")]
        public async Task<KdyResult> BatchCreateActivationCodeAsync(BatchCreateActivationCodeInput input)
        {
            return await _activationCodeAdminService.BatchCreateActivationCodeAsync(input);
        }

        /// <summary>
        /// 查询卡密
        /// </summary>
        /// <returns></returns>
        [HttpGet("query-activation-code")]
        public async Task<KdyResult<QueryPageDto<QueryPageActivationCodeDto>>> QueryPageActivationCodeAsync([FromQuery] QueryPageActivationCodeInput input)
        {
            var resultDto = await _activationCodeAdminService.QueryPageActivationCodeAsync(input);
            var allCodeType = await _activationCodeTypeV2Repository.GetAllListAsync();
            foreach (var item in resultDto.Data.Items)
            {
                item.CodeTypeName = allCodeType.FirstOrDefault(a => a.Id == item.CodyTypeId)?.CodeName;
            }

            return resultDto;
        }

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete-code")]
        public async Task<KdyResult> DeleteActivationCodeAsync(long id)
        {
            return await _activationCodeAdminService.DeleteActivationCodeAsync(id);
        }

        #endregion

        #region 站点配置
        /// <summary>
        /// 查询站点配置
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpGet("query-web-config")]
        public async Task<KdyResult<QueryPageDto<QueryPageWebConfigDto>>> QueryPageWebConfigAsync([FromQuery] QueryPageWebConfigInput input)
        {
            return await _webConfigAdminService.QueryPageWebConfigAsync(input);
        }

        /// <summary>
        /// 创建/更新 配置
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-update-web-config")]
        public async Task<KdyResult> CreateAndUpdateWebConfigAsync(CreateAndUpdateWebConfigInput input)
        {
            return await _webConfigAdminService.CreateAndUpdateWebConfigAsync(input);
        }

        /// <summary>
        /// 删除卡密
        /// </summary>
        /// <returns></returns>
        [HttpDelete("delete-web-config")]
        public async Task<KdyResult> DeleteWebConfigAsync(long id)
        {
            return await _webConfigAdminService.DeleteWebConfigAsync(id);
        }
        #endregion
    }
}
