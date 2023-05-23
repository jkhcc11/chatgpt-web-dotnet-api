using System;

namespace ChatGpt.Web.Dto.Response
{
    /// <summary>
    /// 获取账号消费信息
    /// </summary>
    public class GetAccountBillingResponse
    {
        /// <summary>
        /// 免费额度到期时间
        /// </summary>
        public DateTime AccessUntil { get; set; }

        /// <summary>
        /// 余额
        /// </summary>
        public decimal Balance { get; set; }

        /// <summary>
        /// 已用(美元)
        /// </summary>
        public decimal Usage { get; set; }

        /// <summary>
        /// 可用余额(美元)
        /// </summary>
        public decimal UseBalance => Balance - Usage;
    }
}
