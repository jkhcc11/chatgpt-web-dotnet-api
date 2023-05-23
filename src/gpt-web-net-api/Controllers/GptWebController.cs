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

        public GptWebController(IOpenAiHttpApi openAiHttpApi, IGptWebMessageRepository webMessageRepository,
            ILogger<GptWebController> logger, IdGenerateExtension idGenerateExtension,
            IActivationCodeRepository activationCodeRepository, IMemoryCache memoryCache,
            IOptions<ChatGptWebConfig> options,
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
            var check = await CheckCardNoAsync(input.Token);
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
            var check = await CheckRequestTokenAsync();
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
                        .ToString("yyyy-MM-dd HH:mm:ss")
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
            var check = await CheckRequestTokenAsync();
            if (check.IsSuccess == false)
            {
                await writer.WriteLineAsync(Newtonsoft.Json.JsonConvert.SerializeObject(check));
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
                Stream = true
            };

            if (isFirst == false)
            {
                var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(input.Options.ParentMessageId);
                //todo:回复丢失时 未处理
                userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                    Newtonsoft.Json.JsonConvert.SerializeObject(request));
            }

            var result = await _openAiHttpApi.SendChatCompletionsAsync(key, request);
            if (result.IsSuccess == false)
            {
                _logger.LogError("Gpt返回失败，{reqMsg},{responseMsg}",
                    Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Newtonsoft.Json.JsonConvert.SerializeObject(result));
                await writer.WriteLineAsync(result.Msg);
                await writer.FlushAsync();
                return;
            }

            if (result.Data.ResponseStream == null)
            {
                _logger.LogError("获取相应流失效，{reqMsg},{responseMsg}",
                    Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Newtonsoft.Json.JsonConvert.SerializeObject(result));
                await writer.WriteLineAsync("no");
                await writer.FlushAsync();
                return;
            }

            await using (result.Data.ResponseStream)
            {
                using var reader = new StreamReader(result.Data.ResponseStream);
                var prefixLength = "data: ".Length;
                var roleStr = "";
                var assistantId = "";
                var response = "";
                var currentAnswer = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    #region 流式返回
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line) ||
                        line.Contains("[DONE]"))
                    {
                        //结束或者为空不管
                        continue;
                    }

                    var jsonStr = line.Remove(0, prefixLength);
                    var tempData = Newtonsoft.Json.JsonConvert.DeserializeObject<SendChatCompletionsResponse>(jsonStr);
                    if (string.IsNullOrEmpty(assistantId) &&
                        string.IsNullOrEmpty(tempData?.ChatId) == false)
                    {
                        //只需要获取一次即可
                        assistantId = tempData.ChatId;
                    }

                    var deltaObj = tempData?.Choices.FirstOrDefault()?.DeltaObj;
                    var currentDelta = "";
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

                    var newObj = new
                    {
                        role = roleStr,
                        id = tempData?.ChatId,
                        parentMessageId = "", //
                        delta = currentDelta,
                        text = currentAnswer.ToString()
                    };

                    await writer.WriteLineAsync(Newtonsoft.Json.JsonConvert.SerializeObject(newObj));
                    await writer.FlushAsync();
                    #endregion
                }

                if (isFirst)
                {
                    await CreateFirstMsgAsync(input.Prompt, currentAnswer.ToString()
                        , assistantId
                        , Newtonsoft.Json.JsonConvert.SerializeObject(request)
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
                    token));
            }

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
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo)
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

            var expiryTime = cacheValue.ActivateTime.Value.AddDays(cacheValue.CodeType.GetHashCode());
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "卡密已过期,请续费或重新购买"
                };
            }

            //按次计算
            if (cacheValue.CodeType == ActivationCodeType.PerUse)
            {
                var count = await _perUseActivationCodeRecordRepository.CountTimesAsync(DateTime.Today, cardNo);
                if (count > _chatGptWebConfig.EveryDayFreeTimes)
                {
                    return new BaseGptWebDto<object>()
                    {
                        ResultCode = KdyResultCode.Error,
                        Message = $"今天免费额度,已用完。免费次数：{_chatGptWebConfig.EveryDayFreeTimes}"
                    };
                }
            }

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }

        /// <summary>
        /// 检测请求Token
        /// </summary>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckRequestTokenAsync()
        {
            var cardNo = GetCurrentAuthCardNo();
            if (string.IsNullOrEmpty(cardNo))
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Unauthorized
                };
            }

            var check = await CheckCardNoAsync(cardNo);
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