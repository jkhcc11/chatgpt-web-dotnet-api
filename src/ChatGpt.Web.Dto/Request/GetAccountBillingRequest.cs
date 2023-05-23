namespace ChatGpt.Web.Dto.Request
{
    /// <summary>
    /// 获取账号消费信息
    /// </summary>
    public class GetAccountBillingRequest
    {
        public GetAccountBillingRequest(string apiKey)
        {
            ApiKey = apiKey;
        }

        /// <summary>
        /// ApiKey
        /// </summary>
        public string ApiKey { get; protected set; }


    }
}
