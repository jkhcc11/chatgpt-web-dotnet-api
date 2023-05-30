using System.ComponentModel.DataAnnotations;
using ChatGpt.Web.Entity.Enums;

namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 生成卡密Input
    /// </summary>
    public class BatchGeneralCodeInput
    {
        public BatchGeneralCodeInput(string generalCodeKey)
        {
            GeneralCodeKey = generalCodeKey;
        }

        /// <summary>
        /// 生成密钥
        /// </summary>
        public string GeneralCodeKey { get; set; }

        /// <summary>
        /// 类型Id
        /// </summary>
        public long CodeTypeId { get; set; }

        /// <summary>
        /// 生成数量
        /// </summary>
        [Range(1, 500, ErrorMessage = "最大500")]
        public int Number { get; set; } = 10;
    }
}
