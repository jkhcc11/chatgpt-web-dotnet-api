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
        /// <returns></returns>
        Task<KdyResult<SendChatCompletionsResponse>> SendChatCompletionsAsync(string apiKey, SendChatCompletionsRequest request);
    }
}
