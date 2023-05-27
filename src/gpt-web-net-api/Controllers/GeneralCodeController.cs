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

            //模型校验
            var modelStr = input.ModelStr;
            if (string.IsNullOrEmpty(modelStr))
            {
                modelStr = "gpt-3";
            }

            if (modelStr == "gpt-4")
            {
                //单独gpt4 添加默认gpt-3
                modelStr = "gpt-3,gpt-4";
            }

            var modelArray = modelStr.Split(',');
            if (modelArray.Any(a => a == "gpt-3") == false &&
                modelArray.Any(a => a == "gpt-4") == false)
            {
                return Content("无效模型");
            }


            var resultSb = new StringBuilder();
            resultSb.AppendLine($"本次生成数量：{input.Number},类型：{input.ActivationCodeType.GetValidDaysByCodeType()} 天");
            var dbActivationCode = new List<ActivationCode>();
            if (input.ActivationCodeType == ActivationCodeType.PerUse ||
                input.ActivationCodeType == ActivationCodeType.PerUse4)
            {
                #region 体验卡
                if (string.IsNullOrEmpty(input.FreeCode))
                {
                    return Content("无卡密");
                }

                dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                    input.FreeCode, input.ActivationCodeType)
                {
                    ModelStr = modelStr
                });
                resultSb.AppendLine(input.FreeCode); 
                #endregion
            }
            else
            {
                #region 生成数量
                for (var i = 0; i < input.Number; i++)
                {
                    var code = Guid.NewGuid().ToString();
                    dbActivationCode.Add(new ActivationCode(_idGenerateExtension.GenerateId(),
                        code, input.ActivationCodeType)
                    {
                        ModelStr = modelStr
                    });
                    resultSb.AppendLine(code);
                } 
                #endregion
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
                                    $"类型：{(item.CodeType.GetValidDaysByCodeType())}天 体验卡，" +
                                    $"模型：{(item.ModelStr)}天，" +
                                    $"是否激活：{(item.ActivateTime.HasValue ? "是" : "否")}，" +
                                    $"创建时间：{item.CreatedTime}，" +
                                    $"激活时间：{(item.ActivateTime?.ToString())}");
            }

            return Content(resultSb.ToString(), "text/plain");
        }
    }
}
