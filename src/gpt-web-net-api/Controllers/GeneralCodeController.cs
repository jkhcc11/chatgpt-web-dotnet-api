using System.Text;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using Microsoft.AspNetCore.Mvc;

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
        private readonly IConfiguration _configuration;
        private readonly IdGenerateExtension _idGenerateExtension;

        public GeneralCodeController(IActivationCodeRepository activationCodeRepository,
            IConfiguration configuration, IdGenerateExtension idGenerateExtension)
        {
            _activationCodeRepository = activationCodeRepository;
            _configuration = configuration;
            _idGenerateExtension = idGenerateExtension;
        }

        [HttpPost("batch-general")]
        public async Task<IActionResult> BatchGeneralCodeAsync(BatchGeneralCodeInput input)
        {
            var generalKey = _configuration.GetValue<string>("GeneralCodeKey");
            if (generalKey != input.GeneralCodeKey)
            {
                return Content("密钥错误");
            }

            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次生成数量：{input.Number},类型：{input.ActivationCodeType.GetHashCode()} 天");
            var dbActivationCode = new List<ActivationCode>();
            if (input.ActivationCodeType == ActivationCodeType.PerUse)
            {
                if (string.IsNullOrEmpty(input.FreeCode))
                {
                    return Content("无卡密");
                }

                dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                    input.FreeCode, input.ActivationCodeType));
                resultSb.AppendLine(input.FreeCode);
            }
            else
            {
                for (var i = 0; i < input.Number; i++)
                {
                    var code = Guid.NewGuid().ToString();
                    dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                        code, input.ActivationCodeType));
                    resultSb.AppendLine(code);
                }
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

            var activationCode = await _activationCodeRepository.QueryActivationCodeByTypeAsync(input.ActivationCodeType);
            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次导出数量：{activationCode.Count}");
            foreach (var item in activationCode)
            {
                resultSb.AppendLine($"卡号：{item.CardNo}，" +
                                    $"类型：{(item.CodeType.GetHashCode())}天 体验卡，" +
                                    $"是否激活：{(item.ActivateTime.HasValue ? "是" : "否")}，" +
                                    $"创建时间：{item.CreatedTime}，" +
                                    $"激活时间：{(item.ActivateTime?.ToString())}");
            }

            return Content(resultSb.ToString(), "text/plain");
        }
    }
}
