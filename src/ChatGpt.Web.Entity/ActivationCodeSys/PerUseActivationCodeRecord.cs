using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Entity.ActivationCodeSys
{
    /// <summary>
    /// 按次卡密记录
    /// </summary>
    public class PerUseActivationCodeRecord : BaseEntity<long>
    {
        public PerUseActivationCodeRecord(long id, string cardNo) : base(id)
        {
            CardNo = cardNo;
        }

        /// <summary>
        /// 卡密号
        /// </summary>
        public string CardNo { get; set; }
    }
}
