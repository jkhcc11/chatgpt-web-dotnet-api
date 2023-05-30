using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;

namespace ChatGpt.Web.IService.OpenAiApi
{
    /// <summary>
    /// OpenAi 官方
    /// </summary>
    /// <remarks>
    /// https://platform.openai.com/docs/api-reference/chat
    /// </remarks>
    public interface IOpenAiHttpApi
    {
        /// <summary>
        /// 发起聊天
        /// </summary>
        /// <param name="apiKey">key</param>
        /// <param name="request"></param>
        /// <param name="orgId">组织Id</param>
        /// <param name="proxyHost">反代host</param>
        /// <returns></returns>
        Task<KdyResult<SendChatCompletionsResponse>> SendChatCompletionsAsync(string apiKey,
            SendChatCompletionsRequest request, string? proxyHost, string? orgId = null);
    }
}
