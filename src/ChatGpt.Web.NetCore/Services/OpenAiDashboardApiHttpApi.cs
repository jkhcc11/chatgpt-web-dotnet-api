using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace ChatGpt.Web.NetCore.Services
{
    /// <summary>
    /// OpenAi控制台 实现
    /// </summary>
    public class OpenAiDashboardApiHttpApi : IOpenAiDashboardApiHttpApi
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ChatGptWebConfig _config;
        private readonly ILogger<OpenAiDashboardApiHttpApi> _logger;

        public OpenAiDashboardApiHttpApi(IHttpClientFactory httpClientFactory, IOptions<ChatGptWebConfig> options,
            ILogger<OpenAiDashboardApiHttpApi> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _config = options.Value;
        }

        /// <summary>
        /// 获取账号账单
        /// </summary>
        /// <returns></returns>
        public async Task<KdyResult<GetAccountBillingResponse>> GetAccountBillingAsync(GetAccountBillingRequest request)
        {
            var hostRandom = _config.ApiKeys.RandomList();
            var client = BuildClient(hostRandom.OpenAiBaseHost);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var usage = await GetUsageAsync(client);
            if (usage.IsSuccess == false)
            {
                return KdyResult.Error<GetAccountBillingResponse>(KdyResultCode.HttpError,
                    usage.Msg ?? "获取使用记录异常");
            }

            var balance = await GetBalanceAsync(client);
            if (balance.IsSuccess == false)
            {
                return KdyResult.Error<GetAccountBillingResponse>(KdyResultCode.HttpError,
                    balance.Msg ?? "获取余额异常");
            }

            var result = new GetAccountBillingResponse()
            {
                AccessUntil = balance.Data.AccessUntil,
                Balance = balance.Data.HardLimitUsd,
                Usage = usage.Data / 1000
            };

            return KdyResult.Success(result);
        }

        /// <summary>
        /// 已使用额度
        /// </summary>
        /// <returns></returns>
        private async Task<KdyResult<decimal>> GetUsageAsync(HttpClient client)
        {
            var today = DateTime.Today;
            var startDateStr = $"{today:yyyy-MM-01}";
            var endDateStr = $"{today.AddMonths(1):yyyy-MM-01}";

            var apiRequest = new HttpRequestMessage(HttpMethod.Get,
                $"/dashboard/billing/usage?start_date={startDateStr}&end_date={endDateStr}");
            var apiResponse = await client.SendAsync(apiRequest);
            if (apiResponse.IsSuccessStatusCode == false)
            {
                var errorInfo = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"/dashboard/billing/usage 返回：{errorInfo}");
                return KdyResult.Error<decimal>(KdyResultCode.HttpError, apiResponse.StatusCode.ToString());
            }

            var response = await apiResponse.Content.ReadAsStringAsync();
            _logger.LogDebug($"/dashboard/billing/usage 返回：{response}");

            var jObject = JObject.Parse(response);
            return KdyResult.Success(jObject["total_usage"]?.Value<decimal>() ?? default);
        }

        /// <summary>
        /// 总额
        /// </summary>
        /// <returns></returns>
        private async Task<KdyResult<GetBalanceResponse>> GetBalanceAsync(HttpClient client)
        {
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, "/dashboard/billing/subscription");
            var apiResponse = await client.SendAsync(apiRequest);
            if (apiResponse.IsSuccessStatusCode == false)
            {
                var errorInfo = await apiResponse.Content.ReadAsStringAsync();
                _logger.LogWarning($"/dashboard/billing/subscription 返回：{errorInfo}");
                return KdyResult.Error<GetBalanceResponse>(KdyResultCode.HttpError, apiResponse.StatusCode.ToString());
            }

            var response = await apiResponse.Content.ReadAsStringAsync();
            _logger.LogDebug($"/dashboard/billing/subscription 返回：{response}");
            var jObject = JObject.Parse(response);
            var result = new GetBalanceResponse()
            {
                AccessUntil = (jObject["access_until"]?.Value<int>() ?? 0).ToDataTimeByTimestamp(),
                HardLimitUsd = jObject["hard_limit_usd"]?.Value<decimal>() ?? 0
            };

            return KdyResult.Success(result);
        }

        /// <summary>
        /// 生成Client
        /// </summary>
        /// <returns></returns>
        private HttpClient BuildClient(string? host)
        {
            var baseHost = "https://api.openai.com";
            if (string.IsNullOrEmpty(host) == false)
            {
                baseHost = host;
            }

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseHost);
            return client;
        }
    }
}
