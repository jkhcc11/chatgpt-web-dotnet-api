using ChatGpt.Web.BaseInterface;

namespace ChatGpt.Web.Entity.ActivationCodeSys
{
    /// <summary>
    /// 按次卡密记录
    /// </summary>
    public class PerUseActivationCodeRecord : BaseEntity<long>
    {
        public PerUseActivationCodeRecord(long id,
            string cardNo, string modelId, string modelGroupName) : base(id)
        {
            CardNo = cardNo;
            ModelId = modelId;
            ModelGroupName = modelGroupName;
        }

        /// <summary>
        /// 卡密号
        /// </summary>
        public string CardNo { get; set; }

        /// <summary>
        /// 模型Id
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// 模型分组名
        /// </summary>
        public string ModelGroupName { get; set; }
    }
}
