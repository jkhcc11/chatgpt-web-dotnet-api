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
    /// ChatGpt-web api����
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
        /// ����Ƿ�����֤
        /// </summary>
        /// <returns></returns>
        [HttpPost("session")]
        public async Task<IActionResult> Session()
        {
            //auth �Ƿ���Ҫ��֤
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
        /// �����֤
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
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost("config")]
        public async Task<IActionResult> GetConfigAsync()
        {
            //���Token
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
            //����Ϣ
            var cardInfo = await GetCardInfoByCacheAsync(cardNo);
            if (cardInfo == null ||
                cardInfo.ActivateTime.HasValue == false)
            {
                return new JsonResult(new BaseGptWebDto<object>()
                {
                    Message = "��ȡʧ��",
                    ResultCode = KdyResultCode.Error
                });
            }

            //������
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
        /// ��ʽ������������
        /// </summary>
        /// <returns></returns>
        [HttpPost("chat-process")]
        public async Task ChatProcessAsync(ChatProcessInput input)
        {
            Response.ContentType = "application/octet-stream";
            var writer = new StreamWriter(Response.Body);

            //���Token
            var check = await CheckRequestTokenAsync(input.ApiModel);
            if (check.IsSuccess == false)
            {
                await writer.WriteLineAsync(check.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            var token = GetCurrentAuthCardNo();
            #region ������Ϣ
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
            #region ����ģ����ѡ���Ӧ��api key
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
                //���״� ��ȡ������
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
                    //todo:�ظ���ʧʱ δ����
                    userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                        request.ToJsonStr(), GPT3Tokenizer.Encode(input.Prompt).Count);
                }
            }

            #region ��������token
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
                        Message = $"ϵͳ��ʾ����ǰ���󳬳���������ַ�{maxCountItem?.MaxRequestToken}tokens����ǰ��{requestTokenizer.Count} tokens. ����������£�\n" +
                                  "1�����Թص������Ŀ��ػ�����������\n" +
                                  "2���½����촰��\n" +
                                  "3��ǰ������ҳ���л�ģ�ͣ�gpt3����ģ�Ͳ����ƣ�\n" +
                                  "4�������ܣ��ſ�����\n" +
                                  "��ܰ��ʾ��һ��������2tokens,һ��������1tokens"
                    };

                    await writer.WriteLineAsync(errorResult.ToJsonStr());
                    await writer.FlushAsync();
                    return;
                }
            }
            #endregion

            #region ��������������쳣����
            var result = await _openAiHttpApi.SendChatCompletionsAsync(keyItem.ApiKey
                , request
                , keyItem.OpenAiBaseHost
                , keyItem.OrgId);
            if (result.IsSuccess == false)
            {
                _logger.LogError("Gpt����ʧ�ܣ�{reqMsg},{responseMsg}",
                    request.ToJsonStr(),
                    result.ToJsonStr());
                var errorResult = new BaseGptWebDto<string>()
                {
                    Message = $"OpenAi����\n\n\n{result.Msg}"
                };

                await writer.WriteLineAsync(errorResult.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            if (result.Data.ResponseStream == null)
            {
                _logger.LogError("��ȡ��Ӧ��ʧЧ��{reqMsg},{responseMsg}",
                    request.ToJsonStr(),
                    result.ToJsonStr());
                var errorResult = new BaseGptWebDto<string>()
                {
                    Message = "�����쳣, response stream is null"
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
                    #region ��ʽ����
                    if (string.IsNullOrEmpty(line))
                    {
                        //Ϊ�ղ���
                        continue;
                    }

                    var jsonStr = line.Remove(0, prefixLength);
                    if (jsonStr == _chatGptWebConfig.StopFlag)
                    {
                        #region �����Զ��巵��
                        currentDelta = $"\n\n\n\n[ϵͳ��Ϣ,��ǰģ�ͣ�{input.ApiModel}]";
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

                    #region �������ݽ���
                    var tempData = jsonStr.StrToModel<SendChatCompletionsResponse>();
                    if (string.IsNullOrEmpty(assistantId) &&
                        string.IsNullOrEmpty(tempData.ChatId) == false)
                    {
                        //ֻ��Ҫ��ȡһ�μ���
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
                        _logger.LogWarning("�ǵ�һ������,�û���Ϣ��ʧ��user:{msg}", input.Prompt);
                        return;
                    }

                    await SaveAssistantContentMessage(userMessage, assistantId, currentAnswer.ToString(),
                        response, GPT3Tokenizer.Encode(currentAnswer).Count, duration);

                }

                //��¼
                await _perUseActivationCodeRecordRepository.CreateAsync(new PerUseActivationCodeRecord(
                    _idGenerateExtension.GenerateId(),
                    token,
                    supportModelItem.ModeId,
                    supportModelItem.ModeGroupName
                    ));
            }

        }

        /// <summary>
        /// ��ȡȫ����Դ
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
                //ƥ�䵱ǰ��
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

        #region ˽��

        /// <summary>
        /// �����û��״���Ϣ������ParentId��
        /// </summary>
        /// <param name="userMsg">�û���Ϣ</param>
        /// <param name="assistantContent">�ش�����</param>
        /// <param name="assistantId">�ش�Id</param>
        /// <param name="request">����json</param>
        /// <param name="response">����json todo:��ʱ����¼����ԭʼ����
        /// </param>
        /// <param name="duration">��Ӧʱ��</param>
        /// <param name="systemMsg">ϵͳ��Ϣ������У�</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, long duration, string systemMsg = "")
        {
            var cardNo = GetCurrentAuthCardNo();
            var messages = new List<GptWebMessage>();
            var conversationId = _idGenerateExtension.GenerateId();

            if (string.IsNullOrEmpty(systemMsg) == false)
            {
                //ϵͳ��Ϣ
                var systemMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), systemMsg,
                    MsgType.System, conversationId, cardNo);
                messages.Add(systemMessage);
            }

            //�û���Ϣ
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, conversationId, cardNo)
            {
                GtpRequest = request,
                Tokens = GPT3Tokenizer.Encode(userMsg).Count
            };
            messages.Add(userMessage);

            //�ظ���Ϣ
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
        /// ����Gpt��ϢId������Ϣ������
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


            //��ǰ�Ự��������Ϣ
            var currentConversationMessage =
                await _webMessageRepository.QueryMsgByConversationIdAsync(assistantMessage.ConversationId);
            if (currentConversationMessage.Any() == false)
            {
                //todo: δ��¼�� ���Ҷ�ʧ
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
        /// �����û���Ϣ
        /// </summary>
        /// <param name="parentMsg">����Ϣ</param>
        /// <param name="userMsg">�û���Ϣ</param>
        /// <param name="request">������Ϣ</param>
        /// <param name="tokens">����Tokens</param>
        /// <returns></returns>
        private async Task<GptWebMessage> SaveUserMessage(GptWebMessage parentMsg, string userMsg,
            string request, int tokens)
        {
            var cardNo = GetCurrentAuthCardNo();
            //�û���Ϣ
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
        /// ���淵����Ϣ
        /// </summary>
        /// <returns></returns>
        private async Task SaveAssistantContentMessage(GptWebMessage userMessage,
            string assistantId, string assistantContent, string response, int tokens, long duration)
        {
            var cardNo = GetCurrentAuthCardNo();
            //�ظ���Ϣ
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
        /// ���CardNo
        /// </summary>
        /// <param name="cardNo">����</param>
        /// <param name="modelId">ģ��Id</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo, string modelId)
        {
            var cacheValue = await GetCardInfoByCacheAsync(cardNo);
            if (cacheValue == null)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "��Ч����,��ȷ������"
                };
            }

            if (cacheValue.ActivateTime.HasValue == false)
            {
                cacheValue.ActivateTime = DateTime.Now;
                await _activationCodeRepository.UpdateAsync(cacheValue);
            }

            //������
            var codeType = await GetCodeTypeByCacheAsync(cacheValue.CodyTypeId);
            var expiryTime = cacheValue.ActivateTime.Value.AddDays(codeType.ValidDays);
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "�����ѹ���,�����ѻ����¹���"
                };
            }

            //ģ�ͷ�����Ϣ
            var supportModelItem = codeType.SupportModelItems.FirstOrDefault(a => a.ModeId == modelId);
            if (supportModelItem == null)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"��ǰ���ܲ�֧�֡�{modelId}��,���л�ģ�ͻ��������" +
                              $"����ǰ֧��ģ�ͣ�{string.Join(",", codeType.SupportModelItems.Select(a => a.ModeId))}"
                };
            }

            if (codeType.IsEveryDayResetCount)
            {
                return await CheckTodayCardNoTimesAsync(cardNo, codeType, supportModelItem);
            }

            return await CheckCardNoTimesAsync(cardNo, codeType, supportModelItem);
        }

        /// <summary>
        /// �������Token
        /// </summary>
        /// <param name="modelId">ģ��Id</param>
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
        /// ��ȡ��ǰ��Ȩ����
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
        /// ��ȡ����Ϣ����
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
                    //��ֵ����30������Ч
                    _memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
                }
            }

            return cacheValue;
        }

        /// <summary>
        /// ��ȡ�������ͻ���
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
        /// ��鿨�ܵ����������
        /// </summary>
        /// <param name="cardNo">����</param>
        /// <param name="codeType">��������Ϣ</param>
        /// <param name="supportModelItem">ģ�ͷ�����Ϣ</param>
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
                    Message = "����쳣,today,����ϵ����Ա"
                };
            }

            //��ǰģ�����������
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
            if (modelMax == null)
            {
                //δ���ò�����
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Success
                };
            }

            //���μƷ�
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardNo, supportModelItem.ModeGroupName, DateTime.Today);
            if (count > modelMax.MaxCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"ģ�ͣ�{supportModelItem.ModeGroupName},�������Ѻľ�����������ܡ�\r\n���ܽ�����������{modelMax.MaxCount}"
                };
            }

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
        }

        /// <summary>
        /// ��鿨���������
        /// </summary>
        /// <param name="cardNo">����</param>
        /// <param name="codeType">��������Ϣ</param>
        /// <param name="supportModelItem">ģ�ͷ�����Ϣ</param>
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
                    Message = "����쳣,cardNo times,����ϵ����Ա"
                };
            }

            //��ǰģ�����������
            var modelMax = codeType.GetMaxCountItems()
                .FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);
            if (modelMax == null)
            {
                //δ���ò�����
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Success
                };
            }

            //���μƷ�
            var count = await _perUseActivationCodeRecordRepository
                .CountTimesByGroupNameAsync(cardNo, supportModelItem.ModeGroupName, null);
            if (count > modelMax.MaxCount)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"ģ�ͣ�{supportModelItem.ModeGroupName},����Ѻľ�����������ܡ�\r\n������������{modelMax.MaxCount}"
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