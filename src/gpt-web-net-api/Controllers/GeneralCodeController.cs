using System.Text;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Entity;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.IRepository;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// 生成Code
    /// </summary>
    [ApiController]
    [Route("general-code")]
    public class GeneralCodeController : Controller
    {
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IGptWebConfigRepository _gptWebConfigRepository;

        private readonly IConfiguration _configuration;
        private readonly IdGenerateExtension _idGenerateExtension;
        private readonly WebResourceConfig _webResourceConfig;

        public GeneralCodeController(IActivationCodeRepository activationCodeRepository,
            IConfiguration configuration, IdGenerateExtension idGenerateExtension,
            IOptions<WebResourceConfig> recourseOptions,
            IActivationCodeTypeV2Repository activationCodeTypeV2Repository,
            IGptWebConfigRepository gptWebConfigRepository)
        {
            _activationCodeRepository = activationCodeRepository;
            _configuration = configuration;
            _idGenerateExtension = idGenerateExtension;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
            _gptWebConfigRepository = gptWebConfigRepository;
            _webResourceConfig = recourseOptions.Value;
        }

        #region 卡类型
        [HttpDelete("clear-code-type")]
        public async Task<IActionResult> DeleteAllCodeTypeAsync(string codeKey)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != codeKey)
            {
                return Content("密钥错误");
            }

            var result = await _activationCodeTypeV2Repository.DeleteAllAsync();
            return Content(result + "", "text/plain");
        }

        [HttpGet("export-code-type")]
        public async Task<IActionResult> ExportActivationCodeTypeAsync(string codeKey)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != codeKey)
            {
                return Content("密钥错误");
            }

            var codeType = await _activationCodeTypeV2Repository.GetAllActivationCodeTypeAsync();
            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次导出数量：{codeType.Count}");
            foreach (var item in codeType)
            {
                resultSb.AppendLine($"类型名：{item.CodeName}\r\n" +
                                    $"Json：{item.ToJsonStr()}\r\n" +
                                    $"Id：{(item.Id)}");
            }

            return Content(resultSb.ToString(), "text/plain");
        }

        /// <summary>
        /// 创建卡类型
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-card-type")]
        public async Task<IActionResult> CreateCardTypeAsync(CreateCardTypeInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            if (input.SupportModelGroupNameItems.Any() == false)
            {
                return Content("无效支持模型");
            }

            if (input.LimitItems.Any() == false)
            {
                return Content("限制未配置限制");
            }

            var exits = await _activationCodeTypeV2Repository.CheckNameAsync(input.CardTypeName);
            if (exits)
            {
                return Content("当前名称已存在,操作失败");
            }

            #region 补全支持模型
            var supportModelItems = new List<SupportModeItem>();
            foreach (var item in input.SupportModelGroupNameItems)
            {
                supportModelItems.AddRange(item.GetSupportModeItemsByGroupName());
            }
            #endregion

            //构建卡类型
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
            return Content($"新增成功，卡密名：{dbCodeType.CodeName},Id:{dbCodeType.Id}");
        }

        [HttpPost("update-card-type/{typeId}")]
        public async Task<IActionResult> UpdateCardTypeAsync(long typeId, CreateCardTypeInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            if (input.SupportModelGroupNameItems.Any() == false)
            {
                return Content("无效支持模型");
            }

            if (input.LimitItems.Any() == false)
            {
                return Content("限制未配置限制");
            }

            #region 补全支持模型
            var supportModelItems = new List<SupportModeItem>();
            foreach (var item in input.SupportModelGroupNameItems)
            {
                supportModelItems.AddRange(item.GetSupportModeItemsByGroupName());
            }
            #endregion

            var codeType = await _activationCodeTypeV2Repository.GetEntityByIdAsync(typeId);
            codeType.SupportModelItems = supportModelItems;
            codeType.ValidDays = input.ValidDays;
            codeType.IsEveryDayResetCount = input.IsEveryDayResetCount;
            codeType.MaxCountItems = input.LimitItems
                     .Select(a => new MaxCountItem(a.SupportModelGroupName, a.EveryDayTimes)
                     {
                         MaxHistoryCount = a.MaxHistoryCount,
                         MaxRequestToken = a.MaxRequestTokens,
                         MaxResponseToken = a.MaxResponseTokens ?? 500
                     })
                     .ToList();

            var result = await _activationCodeTypeV2Repository.UpdateAsync(codeType);
            return Content($"操作成功：{result},Id:{codeType.Id}");
        }

        #endregion

        #region 卡密

        [HttpPost("batch-general")]
        public async Task<IActionResult> BatchGeneralCodeAsync(BatchGeneralCodeInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            var codeType = await _activationCodeTypeV2Repository.GetEntityByIdAsync(input.CodeTypeId);
            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次生成数量：{input.Number}" +
                                $",模型：{string.Join(",", codeType.SupportModelItems.Select(a => a.ModeId))}" +
                                $",类型：{codeType.ValidDays} 天");
            var dbActivationCode = new List<ActivationCode>();
            for (var i = 0; i < input.Number; i++)
            {
                var code = Guid.NewGuid().ToString();
                dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                    code, codeType.Id));
                resultSb.AppendLine(code);
            }

            await _activationCodeRepository.CreateAsync(dbActivationCode);
            return Content(resultSb.ToString(), "text/plain");
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportActivationCodeAsync(ExportActivationCodeInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            var codeType = await _activationCodeTypeV2Repository.GetAllActivationCodeTypeAsync();

            var activationCode = await _activationCodeRepository
                .QueryActivationCodeByTypeAsync(input.CodeTypeId);
            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次导出数量：{activationCode.Count}");
            foreach (var item in activationCode)
            {
                var currentCodeType = codeType.First(a => a.Id == item.CodyTypeId);
                resultSb.AppendLine($"卡号：{item.CardNo}，" +
                                    $"类型：{currentCodeType.ValidDays}天 体验卡，" +
                                    $"模型：{string.Join(",", currentCodeType.SupportModelItems.Select(a => a.ModeId))}，" +
                                    $"是否激活：{(item.ActivateTime.HasValue ? "是" : "否")}，" +
                                    $"创建时间：{item.CreatedTime}，" +
                                    $"激活时间：{(item.ActivateTime?.ToString())}");
            }

            return Content(resultSb.ToString(), "text/plain");
        }

        [HttpDelete("delete-code")]
        public async Task<IActionResult> DeleteActivationCodeAsync(string codeKey, string code)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != codeKey)
            {
                return Content("密钥错误");
            }

            var result = await _activationCodeRepository.DeleteAsync(new ActivationCode(1, code, 1));
            return Content(result + "", "text/plain");
        }

        #endregion

        #region 站点配置
        [HttpGet("export-web-config")]
        public async Task<IActionResult> ExportGptWebConfigAsync(string codeKey)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != codeKey)
            {
                return Content("密钥错误");
            }

            var allConfig = await _gptWebConfigRepository.GetAllConfigAsync();
            var resultSb = new StringBuilder();
            foreach (var item in allConfig)
            {
                resultSb.AppendLine($"Host：{item.SubDomainHost}，" +
                                    $"Des：{item.Description}，" +
                                    $"BtnHtml：{item.HomeBtnHtml}，" +
                                    $"Id：{(item.Id)}");
            }

            return Content(resultSb.ToString(), "text/plain");
        }

        /// <summary>
        /// 创建配置
        /// </summary>
        /// <returns></returns>
        [HttpPost("create-web-config")]
        public async Task<IActionResult> CreateGptWebConfigAsync(CreateGptWebConfigInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            var allConfig = await _gptWebConfigRepository.GetAllConfigAsync();
            if (allConfig.Exists(a => a.SubDomainHost == input.SubDomainHost) ||
                (string.IsNullOrEmpty(input.SubDomainHost) &&
                 allConfig.Exists(a => string.IsNullOrEmpty(a.SubDomainHost))))
            {
                return Content("当前名称已存在,修改失败");
            }

            //构建配置
            var dbEntity = new GptWebConfig(_idGenerateExtension.GenerateId())
            {
                SubDomainHost = input.SubDomainHost,
                Description = input.Description,
                HomeBtnHtml = input.HomeBtnHtml
            };
            await _gptWebConfigRepository.CreateAsync(dbEntity);
            return Content($"新增成功：{dbEntity.SubDomainHost},Id:{dbEntity.Id}");
        }

        [HttpPost("update-web-config/{configId}")]
        public async Task<IActionResult> UpdateGptWebConfigAsync(long configId, CreateGptWebConfigInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            var allConfig = await _gptWebConfigRepository.GetAllConfigAsync();

            var entity = allConfig.First(a => a.Id == configId);
            entity.SubDomainHost = input.SubDomainHost;
            entity.Description = input.Description;
            entity.HomeBtnHtml = input.HomeBtnHtml;
            var result = await _gptWebConfigRepository.UpdateAsync(entity);
            return Content($"操作成功：{result},Id:{entity.Id}");
        }
        #endregion
    }
}
