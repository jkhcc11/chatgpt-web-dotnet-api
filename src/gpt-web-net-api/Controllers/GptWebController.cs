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
    /// ChatGpt-web api����
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
        private readonly IConfiguration _configuration;

        public GptWebController(IOpenAiHttpApi openAiHttpApi, IGptWebMessageRepository webMessageRepository,
            ILogger<GptWebController> logger, IdGenerateExtension idGenerateExtension, IOptions<ChatGptWebConfig> options,
            IPerUseActivationCodeRecordRepository perUseActivationCodeRecordRepository, IActivationCodeService activationCodeService,
            IWebConfigService webConfigService,
            IConfiguration configuration)
        {
            _openAiHttpApi = openAiHttpApi;
            _webMessageRepository = webMessageRepository;
            _logger = logger;
            _idGenerateExtension = idGenerateExtension;
            _perUseActivationCodeRecordRepository = perUseActivationCodeRecordRepository;
            _activationCodeService = activationCodeService;
            _webConfigService = webConfigService;
            _configuration = configuration;
            _chatGptWebConfig = options.Value;
        }

        /// <summary>
        /// ����Ƿ�����֤
        /// </summary>
        /// <returns></returns>
        [HttpPost("session")]
        [AllowAnonymous]
        public async Task<IActionResult> Session()
        {
            //auth �Ƿ���Ҫ��֤
            var data = new
            {
                auth = true,
                model = "ChatGPTAPI",
            };
            var result = new BaseGptWebDto<object>(null)
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
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAsync(VerifyInput input)
        {
            var check = await _activationCodeService.CheckCardNoIsValidWithFirstAsync(input.Token);
            if (check.IsSuccess == false)
            {
                return new JsonResult(new BaseGptWebDto<string?>(check.Msg)
                {
                    ResultCode = KdyResultCode.Error
                });
            }

            await Task.CompletedTask;
            var result = new BaseGptWebDto<string>(null)
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
            var cardNo = User.GetUserId();
            if (cardNo.StartsWith(ChatGptWebConfig.CustomKeyPrefix))
            {
                return new JsonResult(new BaseGptWebDto<object>(null)
                {
                    Data = new
                    {
                        expiryTime = "ǰ����תվ���ȡ",
                        activedTime = "ǰ����תվ���ȡ",
                        canModelStr = "ǰ����תվ���ȡ"
                    },
                    ResultCode = KdyResultCode.Success
                });
            }

            //����Ϣ
            var cardInfo = await _activationCodeService.GetCardInfoByCacheAsync(cardNo);
            if (cardInfo == null ||
                cardInfo.ActivateTime.HasValue == false)
            {
                return new JsonResult(new BaseGptWebDto<object>("��ȡʧ��")
                {
                    ResultCode = KdyResultCode.Error
                });
            }

            //������
            var cardType = await _activationCodeService.GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);
            var result = new BaseGptWebDto<object>(null)
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

            var token = User.GetUserId();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            #region ����Ϣ��ȡ
            CheckCardNoResult checkResult;
            if (token.StartsWith(ChatGptWebConfig.CustomKeyPrefix))
            {
                checkResult = await CheckWithCustomHostAsync(token);
            }
            else
            {
                checkResult = await CheckWithActivationCodeAsync(token, input.ApiModel);
            }

            if (checkResult.IsSuccess == false)
            {
                await writer.WriteLineAsync(new BaseGptWebDto<string?>(checkResult.Msg)
                {
                    ResultCode = KdyResultCode.Forbidden
                }.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            var maxCountItem = checkResult.MaxCountItem;
            var keyItem = checkResult.KeyItem;
            var supportModelItem = checkResult.SupportModeItem;

            if (keyItem == null ||
                supportModelItem == null)
            {
                await writer.WriteLineAsync(new BaseGptWebDto<string?>("ϵͳ�쳣,code:keyItem is null")
                {
                    ResultCode = KdyResultCode.Forbidden
                }.ToJsonStr());
                await writer.FlushAsync();
                return;
            }
            #endregion

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

            var maxResponseToken = maxCountItem?.MaxResponseToken;
            var request = new SendChatCompletionsRequest(reqMessages)
            {
                Stream = true,
                Model = input.ApiModel,
                TopP = input.TopP,
                Temperature = input.Temperature,
            };
            if (maxResponseToken is > 0)
            {
                request.MaxTokens = maxResponseToken.Value;
            }

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
                    var errorResult = new BaseGptWebDto<string>($"ϵͳ��ʾ����ǰ���󳬳���������ַ�{maxCountItem?.MaxRequestToken}tokens����ǰ��{requestTokenizer.Count} tokens. ����������£�\n" +
                                                                "1�����Թص������Ŀ��ػ�����������\n" +
                                                                "2���½����촰��\n" +
                                                                "3��ǰ������ҳ���л�ģ�ͣ�gpt3����ģ�Ͳ����ƣ�\n" +
                                                                "4�������ܣ��ſ�����\n" +
                                                                "��ܰ��ʾ��һ��������2tokens,һ��������1tokens");

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
                var errorResult = new BaseGptWebDto<string>($"OpenAi����\n\n\n{result.Msg}");

                await writer.WriteLineAsync(errorResult.ToJsonStr());
                await writer.FlushAsync();
                return;
            }

            if (result.Data.ResponseStream == null)
            {
                _logger.LogError("��ȡ��Ӧ��ʧЧ��{reqMsg},{responseMsg}",
                    request.ToJsonStr(),
                    result.ToJsonStr());
                var errorResult = new BaseGptWebDto<string>("�����쳣, response stream is null");

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
        [AllowAnonymous]
        public async Task<IActionResult> GetResourceAsync()
        {
            var refUrl = HttpContext.Request.Headers.Referer + "";
            string? host = null;
            if (string.IsNullOrEmpty(refUrl) == false)
            {
                //ƥ�䵱ǰ��
                host = new Uri(refUrl).Host;
            }

            var resultDto = await _webConfigService.GetResourceByHostAsync(host);
            var result = new BaseGptWebDto<GetResourceByHostDto>(null)
            {
                Data = resultDto,
                ResultCode = KdyResultCode.Success,
            };
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
            var cardNo = User.GetUserId();
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
            var cardNo = User.GetUserId();
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
            var cardNo = User.GetUserId();
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
        /// ���п���У��
        /// </summary>
        /// <returns></returns>
        private async Task<CheckCardNoResult> CheckWithActivationCodeAsync(string cardNo, string apiModel)
        {
            var cardInfo = await _activationCodeService.GetCardInfoByCacheAsync(cardNo);
            if (cardInfo == null)
            {
                return CheckCardNoResult.Error("card is null");
            }

            var codeType = await _activationCodeService.GetCodeTypeByCacheAsync(cardInfo.CodyTypeId);

            #region Ȩ��У��
            //Ȩ��
            var isAccess = await _activationCodeService.CheckCardNoIsAccessAsync(cardInfo, codeType, apiModel);
            if (isAccess.IsSuccess == false)
            {
                return CheckCardNoResult.Error(isAccess.Msg);
            }

            //Ȩ��У����� ��ȡ��Ϣ
            var supportModelItem = codeType.SupportModelItems.First(a => a.ModeId == apiModel);
            var maxCountItem = codeType.GetMaxCountItems().FirstOrDefault(a => a.ModeGroupName == supportModelItem.ModeGroupName);

            //����
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
                return CheckCardNoResult.Error(checkTimes.Msg);
            }
            #endregion

            #region ����ģ����ѡ���Ӧ��api key
            var keyItems = _chatGptWebConfig.ApiKeys
                .Where(a => a.ModelGroupName == supportModelItem.ModeGroupName)
                .ToList();
            if (keyItems.Any() == false)
            {
                keyItems = _chatGptWebConfig.ApiKeys;
            }

            var keyItem = keyItems.RandomList();
            #endregion

            return CheckCardNoResult.Success(maxCountItem, keyItem, supportModelItem);
        }

        /// <summary>
        /// �Զ��忨��У��
        /// </summary>
        /// <returns></returns>
        private async Task<CheckCardNoResult> CheckWithCustomHostAsync(string cardNo)
        {
            await Task.CompletedTask;
            return CheckCardNoResult.Success(new MaxCountItem("custom", ChatGptWebConfig.CustomApiHostMax)
            {
                MaxHistoryCount = ChatGptWebConfig.CustomApiHostMax,
                MaxRequestToken = ChatGptWebConfig.CustomApiHostMax
            },
                new ApiKeyItem()
                {
                    ApiKey = cardNo,
                    OpenAiBaseHost = _configuration.GetValue<string>(ChatGptWebConfig.CustomApiHostKey)
                },
                new SupportModeItem("custom", "custom"));
        }
        #endregion

    }
}