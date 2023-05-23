using System.ComponentModel.DataAnnotations;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 导出卡密
    /// </summary>
    public class ExportActivationCodeInput
    {
        public ExportActivationCodeInput(string generalCodeKey)
        {
            GeneralCodeKey = generalCodeKey;
        }

        /// <summary>
        /// 生成密钥
        /// </summary>
        public string GeneralCodeKey { get; set; }

        /// <summary>
        /// 卡密类型
        /// </summary>
        [EnumDataType(typeof(ActivationCodeType), ErrorMessage = "卡密类型错误")]
        public ActivationCodeType? ActivationCodeType { get; set; }
    }
}
