using System.Text;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.IService.OpenAiApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// ChatGpt-web api适配
    /// </summary>
    [ApiController]
    [Route("gpt-web-api")]
    public class GptWebController : ControllerBase
    {
        private readonly IOpenAiHttpApi _openAiHttpApi;
        private readonly IGptWebMessageRepository _webMessageRepository;
        private readonly IActivationCodeRepository _activationCodeRepository;
        private readonly IPerUseActivationCodeRecordRepository _perUseActivationCodeRecordRepository;
        private readonly IdGenerateExtension _idGenerateExtension;
        private readonly IMemoryCache _memoryCache;
        private readonly ChatGptWebConfig _chatGptWebConfig;
        private readonly ILogger<GptWebController> _logger;
        private readonly WebResourceConfig _webResourceConfig;

        public GptWebController(IOpenAiHttpApi openAiHttpApi, IGptWebMessageRepository webMessageRepository,
            ILogger<GptWebController> logger, IdGenerateExtension idGenerateExtension,
            IActivationCodeRepository activationCodeRepository, IMemoryCache memoryCache,
            IOptions<ChatGptWebConfig> options,
            IOptions<WebResourceConfig> recourseOptions,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository)
        {
            _openAiHttpApi = openAiHttpApi;
            _webMessageRepository = webMessageRepository;
            _logger = logger;
            _idGenerateExtension = idGenerateExtension;
            _activationCodeRepository = activationCodeRepository;
            _memoryCache = memoryCache;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _chatGptWebConfig = options.Value;
            _webResourceConfig = recourseOptions.Value;
        }

        /// <summary>
        /// 检测是否开启验证
        /// </summary>
        /// <returns></returns>
        [HttpPost("session")]
        public async Task<IActionResult> Session()
        {
            //auth 是否需要验证
            var data = new
            {
                auth = true,
                model = "ChatGPTAPI",
            };
            var result = new BaseGptWebDto<object>()
            {
                Data = data,
                ResultCode = KdyResultCode.Success
            };
            await Task.CompletedTask;
            return new JsonResult(result);
        }

        /// <summary>
        /// 检查验证
        /// </summary>
        /// <returns></returns>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyAsync(VerifyInput input)
        {
            var check = await CheckCardNoAsync(input.Token, "gpt-3");
            if (check.IsSuccess == false)
            {
                return new JsonResult(check);
            }

            await Task.CompletedTask;
            var result = new BaseGptWebDto<string>()
            {
                Data = "login success",
                ResultCode = KdyResultCode.Success
            };
            return new JsonResult(result);
        }

        /// <summary>
        /// 配置
        /// </summary>
        /// <returns></returns>
        [HttpPost("config")]
        public async Task<IActionResult> GetConfigAsync()
        {
            //检测Token
            var check = await CheckRequestTokenAsync("gpt-3");
            if (check.IsSuccess == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = check.Message,
                    ResultCode = KdyResultCode.Error
                });
            }

            var cardNo = GetCurrentAuthCardNo();
            var cardInfo = await GetCardInfoByCacheAsync(cardNo);
            if (cardInfo.ActivateTime.HasValue == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = "获取失败",
                    ResultCode = KdyResultCode.Error
                });
            }

            var result = new BaseGptWebDto<object>()
            {
                Data = new
                {
                    expiryTime = cardInfo.ActivateTime.Value.AddDays(cardInfo.CodeType.GetHashCode())
                        .ToString("yyyy-MM-dd HH:mm:ss"),
                    activedTime = cardInfo.ActivateTime.Value
                        .ToString("yyyy-MM-dd HH:mm:ss"),
                    canModelStr = cardInfo.ModelStr ?? "gpt-3"
                },
                ResultCode = KdyResultCode.Success
            };
            return new JsonResult(result);
        }

        /// <summary>
        /// 流式返回聊天内容
        /// </summary>
        /// <returns></returns>
        [HttpPost("chat-process")]
        public async Task ChatProcessAsync(ChatProcessInput input)
        {
            Response.ContentType = "application/octet-stream";
            var writer = new StreamWriter(Response.Body);

            //检测Token
            var check = await CheckRequestTokenAsync(input.ApiModel);
            if (check.IsSuccess == false)
            {
                await writer.WriteLineAsync(check.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            var token = GetCurrentAuthCardNo();
            var key = _chatGptWebConfig.ApiKeys.RandomList();
            var isFirst = string.IsNullOrEmpty(input.Options.ParentMessageId);
            var reqMessages = new List<SendChatCompletionsMessageItem>()
            {
                new("system",input.SystemMessage),
            };

            GptWebMessage? userMessage = null;
            if (isFirst == false)
            {
                //非首次 获取上下文
                reqMessages = await BuildMsgContextAsync(input.Options.ParentMessageId);
            }

            reqMessages.Add(new("user", input.Prompt));
            var request = new SendChatCompletionsRequest(reqMessages)
            {
                Stream = true,
                Model = input.ApiModel,
                TopP = input.TopP,
                Temperature = input.Temperature
            };

            if (isFirst == false)
            {
                var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(input.Options.ParentMessageId);
                //todo:回复丢失时 未处理
                userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                    request.ToJsonStr());
            }

            var result = await _openAiHttpApi.SendChatCompletionsAsync(key, request);
            if (result.IsSuccess == false)
            {
                _logger.LogError("Gpt返回失败，{reqMsg},{responseMsg}",
                    request.ToJsonStr(),
                    result.ToJsonStr());
                var errorResult = new BaseGptWebDto<string>()
                {
                    Message = $"OpenAi返回\n\n\n{result.Msg}"
                };

                await writer.WriteLineAsync(errorResult.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            if (result.Data.ResponseStream == null)
            {
                _logger.LogError("获取相应流失效，{reqMsg},{responseMsg}",
                    request.ToJsonStr(),
                    result.ToJsonStr());
                var errorResult = new BaseGptWebDto<string>()
                {
                    Message = "服务异常, response stream is null"
                };

                await writer.WriteLineAsync(errorResult.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            await using (result.Data.ResponseStream)
            {
                using var reader = new StreamReader(result.Data.ResponseStream);
                var prefixLength = "data: ".Length;
                string roleStr = "",
                assistantId = "",
                response = "",
                chatId = "";
                var currentAnswer = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    var currentDelta = "";
                    var line = await reader.ReadLineAsync();
                    #region 流式返回
                    if (string.IsNullOrEmpty(line))
                    {
                        //为空不管
                        continue;
                    }

                    var jsonStr = line.Remove(0, prefixLength);
                    if (jsonStr == _chatGptWebConfig.StopFlag)
                    {
                        #region 结束自定义返回
                        currentDelta = $"\n\n\n\n[系统信息,当前模型：{input.ApiModel}]";
                        var endResult = new ChatProcessDto()
                        {
                            Role = roleStr,
                            Id = chatId,
                            Delta = currentDelta,
                            Text = currentAnswer + currentDelta
                        };

                        await writer.WriteLineAsync(endResult.ToJsonStr());
                        await writer.FlushAsync();
                        continue;
                        #endregion
                    }

                    #region 正常内容解析
                    var tempData = jsonStr.StrToModel<SendChatCompletionsResponse>();
                    if (string.IsNullOrEmpty(assistantId) &&
                        string.IsNullOrEmpty(tempData?.ChatId) == false)
                    {
                        //只需要获取一次即可
                        assistantId = tempData.ChatId;
                    }

                    var deltaObj = tempData?.Choices.FirstOrDefault()?.DeltaObj;
                    var roleToken = deltaObj?["role"];
                    if (roleToken != null)
                    {
                        roleStr = roleToken + "";
                    }

                    var contentToken = deltaObj?["content"];
                    if (contentToken != null)
                    {
                        currentDelta = contentToken + "";
                        currentAnswer.Append(currentDelta);
                    }

                    chatId = tempData?.ChatId ?? "";
                    #endregion

                    var currentResult = new ChatProcessDto()
                    {
                        Role = roleStr,
                        Id = chatId,
                        Delta = currentDelta,
                        Text = currentAnswer.ToString()
                    };

                    await writer.WriteLineAsync(currentResult.ToJsonStr());
                    await writer.FlushAsync();
                    #endregion
                }

                if (isFirst)
                {
                    await CreateFirstMsgAsync(input.Prompt, currentAnswer.ToString()
                        , assistantId
                        , request.ToJsonStr()
                        , response
                        , input.SystemMessage);
                }
                else
                {
                    if (userMessage == null)
                    {
                        _logger.LogWarning("非第一次请求,用户消息丢失。user:{msg}", input.Prompt);
                        return;
                    }

                    await SaveAssistantContentMessage(userMessage, assistantId, currentAnswer.ToString(),
                        response);

                }

                //记录
                await _perUseActivationCodeRecordRepository.CreateAsync(new PerUseActivationCodeRecord(
                    _idGenerateExtension.GenerateId(),
                    token,
                    input.ApiModel
                    ));
            }

        }

        /// <summary>
        /// 获取全局资源
        /// </summary>
        /// <returns></returns>
        [HttpPost("resource")]
        public async Task<IActionResult> GetResourceAsync()
        {
            var result = new BaseGptWebDto<WebResourceConfig>()
            {
                Data = _webResourceConfig,
                ResultCode = KdyResultCode.Success
            };
            await Task.CompletedTask;
            return new JsonResult(result);
        }

        #region 私有
        /// <summary>
        /// 创建用户首次消息（不带ParentId）
        /// </summary>
        /// <param name="userMsg">用户消息</param>
        /// <param name="assistantContent">回答内容</param>
        /// <param name="assistantId">回答Id</param>
        /// <param name="request">请求json</param>
        /// <param name="response">返回json todo:暂时不记录返回原始内容
        /// </param>
        /// <param name="systemMsg">系统消息（如果有）</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, string systemMsg = "")
        {
            var messages = new List<GptWebMessage>();
            var conversationId = _idGenerateExtension.GenerateId();

            if (string.IsNullOrEmpty(systemMsg) == false)
            {
                //系统消息
                var systemMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), systemMsg,
                    MsgType.System, conversationId);
                messages.Add(systemMessage);
            }

            //用户消息
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, conversationId)
            {
                GtpRequest = request
            };
            messages.Add(userMessage);

            //回复消息
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, conversationId)
            {
                ParentId = userMessage.ParentId,
                GtpResponse = response,
                GptMsgId = assistantId
            };
            messages.Add(assistantMessage);

            await _webMessageRepository.BatchCreateAsync(messages);
        }

        /// <summary>
        /// 根据Gpt消息Id生成消息上下文
        /// </summary>
        /// <returns></returns>
        private async Task<List<SendChatCompletionsMessageItem>> BuildMsgContextAsync(string gptMsgId)
        {
            var result = new List<SendChatCompletionsMessageItem>();
            var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(gptMsgId);
            if (assistantMessage == null)
            {
                return result;
            }


            //当前会话的所有消息
            var currentConversationMessage =
                await _webMessageRepository.QueryMsgByConversationIdAsync(assistantMessage.ConversationId);
            foreach (var message in currentConversationMessage.OrderBy(a => a.CreatedTime))
            {
                result.Add(new SendChatCompletionsMessageItem(message.MsgType.ToString().ToLower(),
                    message.Msg));
            }

            return result;
        }

        /// <summary>
        /// 保存用户消息
        /// </summary>
        /// <param name="parentMsg">父消息</param>
        /// <param name="userMsg">用户消息</param>
        /// <param name="request">请求消息</param>
        /// <returns></returns>
        private async Task<GptWebMessage> SaveUserMessage(GptWebMessage parentMsg, string userMsg, string request)
        {
            //用户消息
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, parentMsg.ConversationId)
            {
                GtpRequest = request,
                ParentId = parentMsg.Id,
                GptMsgId = parentMsg.GptMsgId
            };

            await _webMessageRepository.CreateAsync(userMessage);
            return userMessage;
        }

        /// <summary>
        /// 保存返回消息
        /// </summary>
        /// <returns></returns>
        private async Task SaveAssistantContentMessage(GptWebMessage userMessage,
            string assistantId, string assistantContent, string response)
        {
            //回复消息
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, userMessage.ConversationId)
            {
                ParentId = userMessage.Id,
                GtpResponse = response,
                GptMsgId = assistantId
            };

            await _webMessageRepository.CreateAsync(assistantMessage);
        }

        /// <summary>
        /// 检测CardNo
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelId">模型Id</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo, string modelId)
        {
            var cacheValue = await GetCardInfoByCacheAsync(cardNo);
            if (cacheValue.CodeType == default)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "无效卡密,请确认无误"
                };
            }

            if (cacheValue.ActivateTime.HasValue == false)
            {
                cacheValue.ActivateTime = DateTime.Now;
                await _activationCodeRepository.UpdateAsync(cacheValue);
            }

            var expiryTime = cacheValue.ActivateTime.Value.AddDays(cacheValue.CodeType.GetValidDaysByCodeType());
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "卡密已过期,请续费或重新购买"
                };
            }

            var modelArray = cacheValue.ModelStr.Split(',');
            if (modelArray.Any(modelId.StartsWith) == false)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"当前卡密不支持【{modelId}】,请切换模型或更换卡密，当前支持模型：{cacheValue.ModelStr}"
                };
            }

            #region 按次计算
            var limitCount = 1;
            switch (cacheValue.CodeType)
            {
                case ActivationCodeType.PerUse:
                    {
                        limitCount = _webResourceConfig.EveryDayFreeTimes;
                        break;
                    }
                case ActivationCodeType.PerUse4:
                    {
                        limitCount = _webResourceConfig.EveryDayFreeTimes4;
                        break;
                    }
            }

            var count = await _perUseActivationCodeRecordRepository.CountTimesAsync(DateTime.Today, cardNo);
            if (count > limitCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"今天免费额度,已用完。免费次数：{limitCount}"
                };
            }
            #endregion

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }

        /// <summary>
        /// 检测请求Token
        /// </summary>
        /// <param name="modelId">模型Id</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckRequestTokenAsync(string modelId)
        {
            var cardNo = GetCurrentAuthCardNo();
            if (string.IsNullOrEmpty(cardNo))
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Unauthorized
                };
            }

            var check = await CheckCardNoAsync(cardNo, modelId);
            if (check.IsSuccess == false)
            {
                check.ResultCode = KdyResultCode.Unauthorized;
            }

            return check;
        }

        /// <summary>
        /// 获取卡信息缓存
        /// </summary>
        /// <returns></returns>
        private async Task<ActivationCode> GetCardInfoByCacheAsync(string cardNo)
        {
            var cacheKey = $"m:cardNo:{cardNo}";
            var cacheValue = _memoryCache.Get<ActivationCode>(cacheKey);
            if (cacheValue == null)
            {
                cacheValue = await _activationCodeRepository.GetActivationCodeByCardNoAsync(cardNo)
                             ?? new ActivationCode(_idGenerateExtension.GenerateId(), cardNo, default);

                if (cacheValue.ActivateTime.HasValue)
                {
                    //有值卡密30分钟生效
                    _memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
                }
            }

            return cacheValue;
        }

        /// <summary>
        /// 获取当前授权卡号
        /// </summary>
        /// <returns></returns>
        private string GetCurrentAuthCardNo()
        {
            var typeIndex = "Bearer ".Length;
            var token = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(token) ||
                token.Length < typeIndex)
            {
                return string.Empty;
            }

            return token.Remove(0, typeIndex);
        }
        #endregion

    }
}