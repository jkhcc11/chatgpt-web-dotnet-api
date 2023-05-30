using System;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Entity.Enums;

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

        /// <summary>
        /// 卡密类型
        /// </summary>
        [Obsolete("待移除，上线自定义类型后移除此")]
        public ActivationCodeType CodeType { get; set; }

        /// <summary>
        /// 可用模型
        /// </summary>
        /// <remarks>
        /// gpt-3|gpt-4 逗号隔开
        /// </remarks>
        [Obsolete("待移除，上线自定义类型后移除此")]
        public string ModelStr { get; set; } = "gpt-3";

    }
}
