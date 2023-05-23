using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.AspNetCore.Mvc;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// ¿ØÖÆÌ¨Api
    /// </summary>
    [ApiController]
    [Route("gpt-api")]
    public class OpenAiController : ControllerBase
    {
        private readonly IOpenAiDashboardApiHttpApi _openAiDashboardApiHttpApi;
        public OpenAiController(IOpenAiDashboardApiHttpApi openAiDashboardApiHttpApi)
        {
            _openAiDashboardApiHttpApi = openAiDashboardApiHttpApi;
        }

        [HttpGet("dashboard/get-account-billing/{apiKey}")]
        public async Task<KdyResult<GetAccountBillingResponse>> GetAccountBillingAsync(string apiKey)
        {
            var request = new GetAccountBillingRequest(apiKey);
            var result = await _openAiDashboardApiHttpApi.GetAccountBillingAsync(request);
            return result;
        }
    }
}