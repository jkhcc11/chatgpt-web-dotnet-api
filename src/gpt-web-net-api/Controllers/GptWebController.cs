using System.Diagnostics;
using System.Text;
using AI.Dev.OpenAI.GPT;
using ChatGpt.Web.BaseInterface;
using ChatGpt.Web.BaseInterface.Extensions;
using ChatGpt.Web.BaseInterface.Options;
using ChatGpt.Web.Dto;
using ChatGpt.Web.Dto.Inputs;
using ChatGpt.Web.Dto.Request;
using ChatGpt.Web.Dto.Response;
using ChatGpt.Web.Entity;
using ChatGpt.Web.Entity.ActivationCodeSys;
using ChatGpt.Web.Entity.Enums;
using ChatGpt.Web.Entity.MessageHistory;
using ChatGpt.Web.IRepository;
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
        private readonly IActivationCodeTypeV2Repository _activationCodeTypeV2Repository;
        private readonly IGptWebConfigRepository _gptWebConfigRepository;

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
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository,
            IActivationCodeTypeV2Repository activationCodeTypeV2Repository,
            IGptWebConfigRepository gptWebConfigRepository)
        {
            _openAiHttpApi = openAiHttpApi;
            _webMessageRepository = webMessageRepository;
            _logger = logger;
            _idGenerateExtension = idGenerateExtension;
            _activationCodeRepository = activationCodeRepository;
            _memoryCache = memoryCache;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _activationCodeTypeV2Repository = activationCodeTypeV2Repository;
            _gptWebConfigRepository = gptWebConfigRepository;
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
            var check = await CheckCardNoAsync(input.Token, ActivationCodeTypeV2.DefaultModelId);
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
            var check = await CheckRequestTokenAsync(ActivationCodeTypeV2.DefaultModelId);
            if (check.IsSuccess == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = check.Message,
                    ResultCode = KdyResultCode.Error
                });
            }

            var cardNo = GetCurrentAuthCardNo();
            //卡信息
            var cardInfo = await GetCardInfoByCacheAsync(cardNo);
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
            var cardType = await GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);
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

            //检测Token
            var check = await CheckRequestTokenAsync(input.ApiModel);
            if (check.IsSuccess == false)
            {
                await writer.WriteLineAsync(check.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            var token = GetCurrentAuthCardNo();
            #region 卡密信息
            var cardInfo = await GetCardInfoByCacheAsync(token);
            if (cardInfo == null)
            {
                await writer.WriteLineAsync("card info is null");
                await writer.FlushAsync();
                return;
            }

            var codeType = await GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);
            var supportModelItem = codeType.SupportModelItems.First(a => a.ModeId == input.ApiModel);
            var maxCountItem =
                codeType.MaxCountItems?.FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
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
            GptWebConfig? currentConfig = null;
            var allConfig = await _gptWebConfigRepository.GetAllConfigAsync();
            var refUrl = HttpContext.Request.Headers.Referer + "";
            if (string.IsNullOrEmpty(refUrl) == false)
            {
                //匹配当前的
                var host = new Uri(refUrl).Host;
                currentConfig = allConfig.FirstOrDefault(a => a.SubDomainHost == host);
            }

            currentConfig ??= allConfig.FirstOrDefault(a => string.IsNullOrEmpty(a.SubDomainHost));

            var config = _webResourceConfig;
            var codeType = await _activationCodeTypeV2Repository.GetAllActivationCodeTypeAsync();
            var freeCodeType = codeType.First(a => a.ValidDays == 999);
            var cardInfo = await _activationCodeRepository.QueryActivationCodeByTypeAsync(freeCodeType.Id);

            config.FreeCode = cardInfo.First().CardNo;
            config.FreeCode4 = cardInfo.First().CardNo;
            config.Description = currentConfig?.Description ?? "";
            config.HomeBtnHtml = currentConfig?.HomeBtnHtml ?? "";
            var result = new BaseGptWebDto<WebResourceConfig>()
            {
                Data = config,
                ResultCode = KdyResultCode.Success,
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
        /// <param name="duration">响应时长</param>
        /// <param name="systemMsg">系统消息（如果有）</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, long duration, string systemMsg = "")
        {
            var cardNo = GetCurrentAuthCardNo();
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
            var cardNo = GetCurrentAuthCardNo();
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
            var cardNo = GetCurrentAuthCardNo();
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

        /// <summary>
        /// 检测CardNo
        /// </summary>
        /// <param name="cardNo">卡号</param>
        /// <param name="modelId">模型Id</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo, string modelId)
        {
            var cacheValue = await GetCardInfoByCacheAsync(cardNo);
            if (cacheValue == null)
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

            //卡类型
            var codeType = await GetCodeTypeByCacheAsync(cacheValue.CodyTypeId);
            var expiryTime = cacheValue.ActivateTime.Value.AddDays(codeType.ValidDays);
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "卡密已过期,请续费或重新购买"
                };
            }

            //模型分组信息
            var supportModelItem = codeType.SupportModelItems.FirstOrDefault(a => a.ModeId == modelId);
            if (supportModelItem == null)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"当前卡密不支持【{modelId}】,请切换模型或更换卡密" +
                              $"，当前支持模型：{string.Join(",", codeType.SupportModelItems.Select(a => a.ModeId))}"
                };
            }

            if (codeType.IsEveryDayResetCount)
            {
                return await CheckTodayCardNoTimesAsync(cardNo, codeType, supportModelItem);
            }

            return await CheckCardNoTimesAsync(cardNo, codeType, supportModelItem);
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

        /// <summary>
        /// 获取卡信息缓存
        /// </summary>
        /// <returns></returns>
        private async Task<ActivationCode?> GetCardInfoByCacheAsync(string cardNo)
        {
            var cacheKey = $"m:cardNo:{cardNo}";
            var cacheValue = _memoryCache.Get<ActivationCode>(cacheKey);
            if (cacheValue == null)
            {
                cacheValue = await _activationCodeRepository.GetActivationCodeByCardNoAsync(cardNo);
                if (cacheValue == null)
                {
                    return default;
                }

                if (cacheValue.ActivateTime.HasValue)
                {
                    //有值卡密30分钟生效
                    _memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
                }
            }

            return cacheValue;
        }

        /// <summary>
        /// 获取卡密类型缓存
        /// </summary>
        /// <returns></returns>
        private async Task<ActivationCodeTypeV2> GetCodeTypeByCacheAsync(long codeTypeId)
        {
            var cacheKey = $"m:codyType:{codeTypeId}";
            var cacheValue = _memoryCache.Get<ActivationCodeTypeV2>(cacheKey);
            if (cacheValue == null)
            {
                cacheValue = await _activationCodeTypeV2Repository.GetEntityByIdAsync(codeTypeId);
                _memoryCache.Set(cacheKey, cacheValue);
            }

            return cacheValue;
        }

        /// <summary>
        /// 检查卡密当天请求次数
        /// </summary>
        /// <param name="cardNo">卡密</param>
        /// <param name="codeType">卡配置信息</param>
        /// <param name="supportModelItem">模型分组信息</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckTodayCardNoTimesAsync(string cardNo
            , ActivationCodeTypeV2 codeType
            , SupportModeItem supportModelItem)
        {
            if (codeType.IsEveryDayResetCount == false)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "检查异常,today,请联系管理员"
                };
            }

            //当前模型组最大配置
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
            if (modelMax == null)
            {
                //未配置不限制
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Success
                };
            }

            //按次计费
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardNo, supportModelItem.ModeGroupName, DateTime.Today);
            if (count > modelMax.MaxCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"模型：{supportModelItem.ModeGroupName},今天额度已耗尽。请更换卡密。\r\n卡密今日最大次数：{modelMax.MaxCount}"
                };
            }

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }

        /// <summary>
        /// 检查卡密请求次数
        /// </summary>
        /// <param name="cardNo">卡密</param>
        /// <param name="codeType">卡配置信息</param>
        /// <param name="supportModelItem">模型分组信息</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoTimesAsync(string cardNo
            , ActivationCodeTypeV2 codeType
            , SupportModeItem supportModelItem)
        {
            if (codeType.IsEveryDayResetCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "检查异常,cardNo times,请联系管理员"
                };
            }

            //当前模型组最大配置
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
            if (modelMax == null)
            {
                //未配置不限制
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Success
                };
            }

            //按次计费
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardNo, supportModelItem.ModeGroupName, null);
            if (count > modelMax.MaxCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"模型：{supportModelItem.ModeGroupName},额度已耗尽。请更换卡密。\r\n卡密最大次数：{modelMax.MaxCount}"
                };
            }

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }
        #endregion

    }
}