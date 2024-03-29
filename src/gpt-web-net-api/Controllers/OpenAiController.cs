using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// ����̨Api
    /// </summary>
    [ApiController]
    [Route("gpt-api")]
    [Authorize(Roles = nameof(CommonExtension.CommonRoleName.Root))]
    public class OpenAiController : BaseController
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