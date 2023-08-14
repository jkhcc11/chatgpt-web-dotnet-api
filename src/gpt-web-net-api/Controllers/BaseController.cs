using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GptWeb.DotNet.Api.Controllers
{
    [Authorize]
    public abstract class BaseController: ControllerBase
    {

    }
}
