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
        /// ����
        /// </summary>
        /// <returns></returns>
        [HttpPost("config")]
        public async Task<IActionResult> GetConfigAsync()
        {
            //���Token
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
                    Message = "��ȡʧ��",
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
            var keyItem = _chatGptWebConfig.ApiKeys.RandomList();
            var isFirst = string.IsNullOrEmpty(input.Options.ParentMessageId);
            var reqMessages = new List<SendChatCompletionsMessageItem>()
            {
                new("system",input.SystemMessage),
            };

            GptWebMessage? userMessage = null;
            if (isFirst == false)
            {
                //���״� ��ȡ������
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
                //todo:�ظ���ʧʱ δ����
                userMessage = await SaveUserMessage(assistantMessage, input.Prompt,
                    request.ToJsonStr());
            }

            var result = await _openAiHttpApi.SendChatCompletionsAsync(keyItem.ApiKey, request,
                keyItem.OrgId);
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
                        string.IsNullOrEmpty(tempData?.ChatId) == false)
                    {
                        //ֻ��Ҫ��ȡһ�μ���
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
                        _logger.LogWarning("�ǵ�һ������,�û���Ϣ��ʧ��user:{msg}", input.Prompt);
                        return;
                    }

                    await SaveAssistantContentMessage(userMessage, assistantId, currentAnswer.ToString(),
                        response);

                }

                //��¼
                await _perUseActivationCodeRecordRepository.CreateAsync(new PerUseActivationCodeRecord(
                    _idGenerateExtension.GenerateId(),
                    token,
                    input.ApiModel
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
            var result = new BaseGptWebDto<WebResourceConfig>()
            {
                Data = _webResourceConfig,
                ResultCode = KdyResultCode.Success
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
        /// <param name="systemMsg">ϵͳ��Ϣ������У�</param>
        /// <returns></returns>
        private async Task CreateFirstMsgAsync(string userMsg, string assistantContent, string assistantId,
            string request, string response, string systemMsg = "")
        {
            var messages = new List<GptWebMessage>();
            var conversationId = _idGenerateExtension.GenerateId();

            if (string.IsNullOrEmpty(systemMsg) == false)
            {
                //ϵͳ��Ϣ
                var systemMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), systemMsg,
                    MsgType.System, conversationId);
                messages.Add(systemMessage);
            }

            //�û���Ϣ
            var userMessage = new GptWebMessage(_idGenerateExtension.GenerateId(), userMsg,
                MsgType.User, conversationId)
            {
                GtpRequest = request
            };
            messages.Add(userMessage);

            //�ظ���Ϣ
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
        /// ����Gpt��ϢId������Ϣ������
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


            //��ǰ�Ự��������Ϣ
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
        /// �����û���Ϣ
        /// </summary>
        /// <param name="parentMsg">����Ϣ</param>
        /// <param name="userMsg">�û���Ϣ</param>
        /// <param name="request">������Ϣ</param>
        /// <returns></returns>
        private async Task<GptWebMessage> SaveUserMessage(GptWebMessage parentMsg, string userMsg, string request)
        {
            //�û���Ϣ
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
        /// ���淵����Ϣ
        /// </summary>
        /// <returns></returns>
        private async Task SaveAssistantContentMessage(GptWebMessage userMessage,
            string assistantId, string assistantContent, string response)
        {
            //�ظ���Ϣ
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
        /// ���CardNo
        /// </summary>
        /// <param name="cardNo">����</param>
        /// <param name="modelId">ģ��Id</param>
        /// <returns></returns>
        private async Task<BaseGptWebDto<object>> CheckCardNoAsync(string cardNo, string modelId)
        {
            var cacheValue = await GetCardInfoByCacheAsync(cardNo);
            if (cacheValue.CodeType == default)
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

            var expiryTime = cacheValue.ActivateTime.Value.AddDays(cacheValue.CodeType.GetValidDaysByCodeType());
            if (DateTime.Now > expiryTime)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = "�����ѹ���,�����ѻ����¹���"
                };
            }

            var modelArray = cacheValue.ModelStr.Split(',');
            if (modelArray.Any(modelId.StartsWith) == false)
            {
                return new BaseGptWebDto<object>()
                {
                    ResultCode = KdyResultCode.Error,
                    Message = $"��ǰ���ܲ�֧�֡�{modelId}��,���л�ģ�ͻ�������ܣ���ǰ֧��ģ�ͣ�{cacheValue.ModelStr}"
                };
            }

            #region ���μ���
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
                    Message = $"������Ѷ��,�����ꡣ��Ѵ�����{limitCount}"
                };
            }
            #endregion

            return new BaseGptWebDto<object>()
            {
                ResultCode = KdyResultCode.Success
            };
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
        /// ��ȡ����Ϣ����
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
                    //��ֵ����30������Ч
                    _memoryCache.Set(cacheKey, cacheValue, TimeSpan.FromMinutes(30));
                }
            }

            return cacheValue;
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
        #endregion

    }
}