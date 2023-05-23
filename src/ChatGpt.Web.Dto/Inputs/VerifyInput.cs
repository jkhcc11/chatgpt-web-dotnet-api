namespace ChatGpt.Web.Dto.Inputs
{
    /// <summary>
    /// 校验
    /// </summary>
    public class VerifyInput
    {
        public VerifyInput(string token)
        {
            Token = token;
        }

        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; }
    }
}
