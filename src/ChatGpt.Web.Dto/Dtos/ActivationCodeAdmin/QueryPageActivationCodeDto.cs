using System;

namespace ChatGpt.Web.Dto.Dtos.ActivationCodeAdmin
{
    /// <summary>
    /// 分页获取卡密
    /// </summary>
    public class QueryPageActivationCodeDto
    {
        /// <summary>
        /// 卡密号
        /// </summary>
        public string CardNo { get; set; } = "";

        /// <summary>
        /// 激活时间
        /// </summary>
        public DateTime? ActivateTime { get; set; }

        /// <summary>
        /// 卡密类型Id
        /// </summary>
        public long CodyTypeId { get; set; }

        /// <summary>
        /// 卡密类型名
        /// </summary>
        public string CodeTypeName { get; set; } = "";
    }
}
