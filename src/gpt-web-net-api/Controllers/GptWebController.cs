using System.Diagnostics;
using System.Text;
using AI.Dev.OpenAI.GPT;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto;
using ChatGpt.Web.Dto.Dtos;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository.ActivationCodeSys;
using ChatGpt.Web.IRepository.MessageHistory;
using ChatGpt.Web.IService;
using ChatGpt.Web.IService.ActivationCodeSys;
using ChatGpt.Web.IService.OpenAiApi;
using GptWeb.DotNet.Api.ServicesExtensiones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GptWeb.DotNet.Api.Controllers
{
    /// <summary>
    /// ChatGpt-web api适配
    /// </summary>
    [ApiController]
    [Route("gpt-web-api")]
    public class GptWebController : BaseController
    {
        private readonly IOpenAiHttpApi _openAiHttpApi;
        private readonly IGptWebMessageRepository _webMessageRepository;
        private readonly IPerUseActivationCodeRecordRepository _perUseActivationCodeRecordRepository;

        private readonly IdGenerateExtension _idGenerateExtension;
        private readonly ChatGptWebConfig _chatGptWebConfig;
        private readonly ILogger<GptWebController> _logger;
        private readonly IActivationCodeService _activationCodeService;
        private readonly IWebConfigService _webConfigService;

        public GptWebController(IOpenAiHttpApi openAiHttpApi, IGptWebMessageRepository webMessageRepository,
            ILogger<GptWebController> logger, IdGenerateExtension idGenerateExtension, IOptions<ChatGptWebConfig> options,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository, IActivationCodeService activationCodeService,
            IWebConfigService webConfigService)
        {
            _openAiHttpApi = openAiHttpApi;
            _webMessageRepository = webMessageRepository;
            _logger = logger;
            _idGenerateExtension = idGenerateExtension;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _activationCodeService = activationCodeService;
            _webConfigService = webConfigService;
            _chatGptWebConfig = options.Value;
        }

        /// <summary>
        /// 检测是否开启验证
        /// </summary>
        /// <returns></returns>
        [HttpPost("session")]
        [AllowAnonymous]
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
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAsync(VerifyInput input)
        {
            var check = await _activationCodeService.CheckCardNoIsValidWithFirstAsync(input.Token);
            if (check.IsSuccess == false)
            {
                return new JsonResult(new BaseGptWebDto<string?>()
                {
                    Data = check.Msg,
                    ResultCode = KdyResultCode.Unauthorized
                });
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
            var cardNo = User.GetUserId();
            //卡信息
            var cardInfo = await _activationCodeService.GetCardInfoByCacheAsync(cardNo);
            if (cardInfo == null ||
                cardInfo.ActivateTime.HasValue == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = "获取失败",
                    ResultCode = KdyResultCode.Error
                });
            }

            //卡类型
            var cardType = await _activationCodeService.GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);
            var result = new BaseGptWebDto<object>()
            {
                Data = new
                {
                    expiryTime = cardInfo.ActivateTime.Value.AddDays(cardType.ValidDays)
                        .ToString("yyyy-MM-dd HH:mm:ss"),
                    activedTime = cardInfo.ActivateTime.Value
                        .ToString("yyyy-MM-dd HH:mm:ss"),
                    canModelStr = string.Join(","
                    , cardType.SupportModelItems
                        .Select(a => a.ModeGroupName)
                        .Distinct())
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

            var token = User.GetUserId();
            #region 卡信息
            var cardInfo = await _activationCodeService.GetCardInfoByCacheAsync(token);
            if (cardInfo == null)
            {
                await writer.WriteLineAsync("card info is null");
                await writer.FlushAsync();
                return;
            }

            var codeType = await _activationCodeService.GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);
            var supportModelItem = codeType.SupportModelItems.First(a => a.ModeId == input.ApiModel);
            var maxCountItem =
                codeType.MaxCountItems?.FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
            #endregion

            #region 权限校验
            //权限
            var isAccess = await _activationCodeService.CheckCardNoIsAccessAsync(cardInfo, codeType, input.ApiModel);
            if (isAccess.IsSuccess == false)
            {
                await writer.WriteLineAsync(new BaseGptWebDto<string?>()
                {
                    Data = isAccess.Msg,
                    ResultCode = KdyResultCode.Forbidden
                }.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            //次数
            KdyResult checkTimes;
            if (codeType.IsEveryDayResetCount)
            {
                checkTimes = await _activationCodeService.CheckTodayCardNoTimesAsync(cardInfo, codeType, supportModelItem);
            }
            else
            {
                checkTimes = await _activationCodeService.CheckCardNoTimesAsync(cardInfo, codeType, supportModelItem);
            }

            if (checkTimes.IsSuccess == false)
            {
                await writer.WriteLineAsync(new BaseGptWebDto<string?>()
                {
                    Data = checkTimes.Msg,
                    ResultCode = KdyResultCode.Forbidden
                }.ToJsonStr());
                await writer.FlushAsync();
                return;
            }
            #endregion

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            #region 根据模型组选择对应的api key
            var keyItems = _chatGptWebConfig.ApiKeys
                  .Where(a => a.ModelGroupName == supportModelItem.ModeGroupName)
                  .ToList();
            if (keyItems.Any() == false)
            {
                keyItems = _chatGptWebConfig.ApiKeys;
            }
            #endregion

            var keyItem = keyItems.RandomList();
            var isFirst = string.IsNullOrEmpty(input.Options.ParentMessageId);
            var reqMessages = new List<SendChatCompletionsMessageItem>()
            {
                new("system",input.SystemMessage),
            };

            GptWebMessage? userMessage = null;
            if (isFirst == false)
            {
                //非首次 获取上下文
                reqMessages = await BuildMsgContextAsync(input.Options.ParentMessageId
                    , maxCountItem);
            }

            reqMessages.Add(new("user", input.Prompt));
            var request = new SendChatCompletionsRequest(reqMessages)
            {
                Stream = true,
                Model = input.ApiModel,
                TopP = input.TopP,
                Temperature = input.Temperature,
                MaxTokens = maxCountItem?.MaxResponseToken ?? 1000
            };

            if (isFirst == false)
            {
                var assistantMessage = await _webMessageRepository.GetMessageByParentMsgIdAsync(input.Options.ParentMessageId);
                if (assistantMessage != null)
                {
                    //todo:回复丢失时 未处理
                    userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                        request.ToJsonStr(), GPT3Tokenizer.Encode(input.Prompt).Count);
                }
            }

            #region 计算请求token
            var maxRequestToken = maxCountItem?.MaxRequestToken;
            if (maxRequestToken.HasValue)
            {
                var requestContextSb = new StringBuilder();
                foreach (var item in reqMessages.Where(a => a.Role != MsgType.System.ToString().ToLower()))
                {
                    requestContextSb.Append(item.Content);
                }
                var requestTokenizer = GPT3Tokenizer.Encode(requestContextSb);
                if (requestTokenizer.Count > maxRequestToken)
                {
                    var errorResult = new BaseGptWebDto<string>()
                    {
                        Message = $"系统提示：当前请求超出最大请求字符{maxCountItem?.MaxRequestToken}tokens，当前：{requestTokenizer.Count} tokens. 解决方法如下：\n" +
                                  "1、尝试关掉上下文开关或减少输入次数\n" +
                                  "2、新建聊天窗口\n" +
                                  "3、前往设置页面切换模型（gpt3所有模型不限制）\n" +
                                  "4、购买卡密，放宽限制\n" +
                                  "温馨提示：一个汉字算2tokens,一个单词算1tokens"
                    };

                    await writer.WriteLineAsync(errorResult.ToJsonStr());
                    await writer.FlushAsync();
                    return;
                }
            }
            #endregion

            #region 发送聊天请求和异常处理
            var result = await _openAiHttpApi.SendChatCompletionsAsync(keyItem.ApiKey
                , request
                , keyItem.OpenAiBaseHost
                , keyItem.OrgId);
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
            #endregion

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
                        string.IsNullOrEmpty(tempData.ChatId) == false)
                    {
                        //只需要获取一次即可
                        assistantId = tempData.ChatId;
                    }

                    var deltaObj = tempData.Choices.FirstOrDefault()?.DeltaObj;
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

                    chatId = tempData.ChatId;
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

                stopWatch.Stop();
                var duration = stopWatch.ElapsedMilliseconds;
                if (isFirst)
                {
                    await CreateFirstMsgAsync(input.Prompt, currentAnswer.ToString()
                        , assistantId
                        , request.ToJsonStr()
                        , response
                        , duration
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
                        response, GPT3Tokenizer.Encode(currentAnswer).Count, duration);

                }

                //记录
                await _perUseActivationCodeRecordRepository.CreateAsync(new PerUseActivationCodeRecord(
                    _idGenerateExtension.GenerateId(),
                    token,
                    supportModelItem.ModeId,
                    supportModelItem.ModeGroupName
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
            var refUrl = HttpContext.Request.Headers.Referer + "";
            string? host = null;
            if (string.IsNullOrEmpty(refUrl) == false)
            {
                //匹配当前的
                host = new Uri(refUrl).Host;
            }

            var resultDto = await _webConfigService.GetResourceByHostAsync(host);
            var result = new BaseGptWebDto<GetResourceByHostDto>()
            {
                Data = resultDto,
                ResultCode = KdyResultCode.Success,
            };
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
        /// <param name="duration">响应时长</param>
        /// <param name="systemMsg">系统消息（如果有）</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, long duration, string systemMsg = "")
        {
            var cardNo = User.GetUserId();
            var messages = new List<GptWebMessage>();
            var conversationId = _idGenerateExtension.GenerateId();

            if (string.IsNullOrEmpty(systemMsg) == false)
            {
                //系统消息
                var systemMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), systemMsg,
                    MsgType.System, conversationId, cardNo);
                messages.Add(systemMessage);
            }

            //用户消息
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, conversationId, cardNo)
            {
                GtpRequest = request,
                Tokens = GPT3Tokenizer.Encode(userMsg).Count
            };
            messages.Add(userMessage);

            //回复消息
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, conversationId, cardNo)
            {
                ParentId = userMessage.ParentId,
                GtpResponse = response,
                GptMsgId = assistantId,
                Tokens = GPT3Tokenizer.Encode(assistantContent).Count,
                ResponseDuration = duration
            };
            messages.Add(assistantMessage);

            await _webMessageRepository.CreateAsync(messages);
        }

        /// <summary>
        /// 根据Gpt消息Id生成消息上下文
        /// </summary>
        /// <returns></returns>
        private async Task<List<SendChatCompletionsMessageItem>> BuildMsgContextAsync(string gptMsgId
            , MaxCountItem? maxCountItem)
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
            if (currentConversationMessage.Any() == false)
            {
                //todo: 未记录的 暂且丢失
                return result;
            }

            var validMessage = currentConversationMessage
                .OrderBy(a => a.CreatedTime)
                .ToList();
            if (maxCountItem is { MaxHistoryCount: { } })
            {
                validMessage = currentConversationMessage
                    .OrderBy(a => a.CreatedTime)
                    .Take(maxCountItem.MaxHistoryCount.Value)
                    .ToList();
            }

            foreach (var message in validMessage)
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
        /// <param name="tokens">消耗Tokens</param>
        /// <returns></returns>
        private async Task<GptWebMessage> SaveUserMessage(GptWebMessage parentMsg, string userMsg,
            string request, int tokens)
        {
            var cardNo = User.GetUserId();
            //用户消息
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, parentMsg.ConversationId, cardNo)
            {
                GtpRequest = request,
                ParentId = parentMsg.Id,
                GptMsgId = parentMsg.GptMsgId,
                Tokens = tokens
            };

            await _webMessageRepository.CreateAsync(userMessage);
            return userMessage;
        }

        /// <summary>
        /// 保存返回消息
        /// </summary>
        /// <returns></returns>
        private async Task SaveAssistantContentMessage(GptWebMessage userMessage,
            string assistantId, string assistantContent, string response, int tokens, long duration)
        {
            var cardNo = User.GetUserId();
            //回复消息
            var assistantMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), assistantContent,
                MsgType.Assistant, userMessage.ConversationId, cardNo)
            {
                ParentId = userMessage.Id,
                GtpResponse = response,
                GptMsgId = assistantId,
                Tokens = tokens,
                ResponseDuration = duration
            };

            await _webMessageRepository.CreateAsync(assistantMessage);
        }
        #endregion

    }
}