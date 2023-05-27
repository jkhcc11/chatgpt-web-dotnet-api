using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ChatGpt.Web.NetCore.Services
{
    /// <summary>
    /// OpenAi 官方
    /// </summary>
    public class OpenAiHttpApi : IOpenAiHttpApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ChatGptWebConfig _config;
        private readonly ILogger<OpenAiHttpApi> _logger;

        public OpenAiHttpApi(IHttpClientFactory httpClientFactory,
            IOptions<ChatGptWebConfig> options, ILogger<OpenAiHttpApi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 发起聊天
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult<SendChatCompletionsResponse>> SendChatCompletionsAsync(string apiKey, SendChatCompletionsRequest request)
        {
            var client = BuildClient();
            var requestUrl = "/v1/chat/completions";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            if (request.Stream == false)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var apiRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            apiRequest.Content = new StringContent(JsonConvert.SerializeObject(request)
                , Encoding.UTF8
                , "application/json");
            var apiResponse = await client.SendAsync(apiRequest,
                request.Stream ? HttpCompletionOption.ResponseHeadersRead :
                    HttpCompletionOption.ResponseContentRead);
            if (apiResponse.IsSuccessStatusCode == false)
            {
                var errorInfo = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"{requestUrl} 返回：{errorInfo}");
                return KdyResult.Error<SendChatCompletionsResponse>(KdyResultCode.HttpError,
                    errorInfo ?? apiResponse.StatusCode.ToString());
            }

            if (request.Stream)
            {
                //流式返回
                var stream = await apiResponse.Content.ReadAsStreamAsync();
                var result = new SendChatCompletionsResponse
                {
                    ResponseStream = stream
                };
                return KdyResult.Success(result);
            }

            //正常返回
            var responseJsonStr = await apiResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<SendChatCompletionsResponse>(responseJsonStr);
            _logger.LogDebug($"{requestUrl} 返回：{JsonConvert.SerializeObject(response)}");
            return response == null ?
                KdyResult.Error<SendChatCompletionsResponse>(KdyResultCode.Error, "解析返回结果失败") :
                KdyResult.Success(response);
        }

        /// <summary>
        /// 生成Client
        /// </summary>
        /// <returns></returns>
        private HttpClient BuildClient()
        {
            var baseHost = "https://api.openai.com";
            if (string.IsNullOrEmpty(_config.OpenAiBaseHost) == false)
            {
                baseHost = _config.OpenAiBaseHost;
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(_config.ApiTimeoutMilliseconds);
            client.BaseAddress = new Uri(baseHost);
            return client;
        }
    }
}
