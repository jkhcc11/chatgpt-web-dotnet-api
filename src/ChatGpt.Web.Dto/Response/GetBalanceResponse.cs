using System;

namespace ChatGpt.Web.Dto.Response
{
    /// <summary>
    /// 总额
    /// </summary>
    public class GetBalanceResponse
    {
        /// <summary>
        /// 免费额度到期时间
        /// </summary>
        public DateTime AccessUntil { get; set; }

        /// <summary>
        /// 总额度
        /// </summary>
        public decimal HardLimitUsd { get; set; }
    }
}
