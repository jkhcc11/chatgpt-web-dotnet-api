namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 导出卡密
    /// </summary>
    public class ExportActivationCodeInput
    {
        public ExportActivationCodeInput(string generalCodeKey)
        {
            GeneralCodeKey = generalCodeKey;
        }

        /// <summary>
        /// 生成密钥
        /// </summary>
        public string GeneralCodeKey { get; set; }

        /// <summary>
        /// 卡密类型Id
        /// </summary>
        public long? CodeTypeId { get; set; }
    }
}
