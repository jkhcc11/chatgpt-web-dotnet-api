using System.ComponentModel.DataAnnotations;

namespace ChatGpt.Web.Dto.Inputs.ActivationCodeAdmin
{
    /// <summary>
    /// 批量创建卡密
    /// </summary>
    public class BatchCreateActivationCodeInput
    {
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
