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
            ActivationCodeType codeType) : base(id)
        {
            CardNo = cardNo;
            CodeType = codeType;
        }

        /// <summary>
        /// 卡密号
        /// </summary>
        public string CardNo { get; set; }

        /// <summary>
        /// 卡密类型
        /// </summary>
        public ActivationCodeType CodeType { get; set; }

        /// <summary>
        /// 激活时间
        /// </summary>
        public DateTime? ActivateTime { get; set; }
    }
}
