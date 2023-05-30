using System;
using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Entity.ActivationCodeSys
{
    /// <summary>
    /// 卡密信息
    /// </summary>
    public class ActivationCode : BaseEntity<long>
    {
        public ActivationCode(long id, string cardNo,
            long codyTypeId) : base(id)
        {
            CardNo = cardNo;
            CodyTypeId = codyTypeId;
        }

        /// <summary>
        /// 卡密号
        /// </summary>
        public string CardNo { get; set; }

        /// <summary>
        /// 激活时间
        /// </summary>
        public DateTime? ActivateTime { get; set; }

        /// <summary>
        /// 卡密类型Id
        /// </summary>
        public long CodyTypeId { get; set; }
    }
}
