using System.ComponentModel.DataAnnotations;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 生成卡密Input
    /// </summary>
    public class BatchGeneralCodeInput
    {
        public BatchGeneralCodeInput(string generalCodeKey, ActivationCodeType activationCodeType)
        {
            GeneralCodeKey = generalCodeKey;
            ActivationCodeType = activationCodeType;
        }

        /// <summary>
        /// 生成密钥
        /// </summary>
        public string GeneralCodeKey { get; set; }

        /// <summary>
        /// 卡密类型
        /// </summary>
        [EnumDataType(typeof(ActivationCodeType), ErrorMessage = "卡密类型错误")]
        public ActivationCodeType ActivationCodeType { get; set; }

        /// <summary>
        /// 生成数量
        /// </summary>
        [Range(1, 500, ErrorMessage = "最大500")]
        public int Number { get; set; } = 10;

        /// <summary>
        /// 免费Code
        /// </summary>
        public string? FreeCode { get; set; }

        /// <summary>
        /// 可用模型
        /// </summary>
        /// <remarks>
        /// gpt-3|gpt-4 逗号隔开
        /// </remarks>
        public string ModelStr { get; set; } = "gpt-3";
    }
}
