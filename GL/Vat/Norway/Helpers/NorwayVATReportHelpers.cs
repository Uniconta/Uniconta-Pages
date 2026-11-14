using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnicontaClient.Pages.GL.Vat.Norway.Models;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Feedback;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Submission;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Helpers
{
    /// <summary>
    /// What an earlier Altinn instance means for a new submission
    /// </summary>
    public enum NorwayVATInstanceState
    {
        /// <summary>Deleted: as if there was none</summary>
        Gone,
        /// <summary>Processed by Skatteetaten (process ended): the return was delivered</summary>
        Delivered,
        /// <summary>Submitted and being processed by Skatteetaten (feedback task)</summary>
        Processing,
        /// <summary>Not submitted (filling in or confirmation task): a draft left by a Send that failed half way</summary>
        Draft,
        /// <summary>Anything else</summary>
        Unknown
    }

    public class NorwayVATReportHelpers
    {
        private string _clientId;
        private string _authority;
        private string _redirectUrl;
        private string _validationUrl;
        private string _altinnTokenExchangeUrl;
        private string _instancesBaseUrl;

        //Tokens of the environment the current operation runs in. Set by InitializeUrls at the start of each operation
        private TokenSet _tokens;

        /// <summary>
        /// ID-porten and Altinn tokens of one environment. Test and production never share tokens
        /// </summary>
        private sealed class TokenSet
        {
            public string IdPortenAccessToken;
            public string IdPortenRefreshToken;
            public DateTimeOffset IdPortenAccessTokenExpiration;

            public string AltinnAccessToken;
            public DateTimeOffset AltinnAccessTokenExpiration;

            public bool HasValidIdPortenToken => !String.IsNullOrEmpty(IdPortenAccessToken) && IdPortenAccessTokenExpiration > DateTimeOffset.Now;
            public bool HasValidAltinnToken => !String.IsNullOrEmpty(AltinnAccessToken) && AltinnAccessTokenExpiration > DateTimeOffset.Now;

            public void Clear()
            {
                IdPortenAccessToken = IdPortenRefreshToken = AltinnAccessToken = null;
                IdPortenAccessTokenExpiration = AltinnAccessTokenExpiration = DateTimeOffset.MinValue;
            }
        }

        private static readonly TokenSet _testTokens = new TokenSet();
        private static readonly TokenSet _prodTokens = new TokenSet();

        //One login at a time, in both environments: all logins share the fixed loopback port
        private static readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);

        //Uniconta user and company the cached tokens belong to. Another user or company in the same process starts with no tokens
        private static string _tokenOwner;
        private static readonly object _tokenOwnerLock = new object();

        //Shared client without default headers. The bearer token is set on each request
        private static readonly HttpClient _httpClient = new HttpClient();

        //Altinn data type of the envelope element that is created with the instance
        private const string EnvelopeDataType = "mvameldinginnsending";

        /// <summary>
        /// Forgets the tokens of the environment, so the next call asks for a new login
        /// </summary>
        public static void SignOut(bool test)
        {
            (test ? _testTokens : _prodTokens).Clear();
        }

        /// <summary>
        /// Ties the cached tokens to a Uniconta user and company. When another user logs in to Uniconta, or the page is opened in another company,
        /// the tokens of the previous owner are dropped, so nobody submits with somebody else's ID-porten login
        /// </summary>
        public static void SetTokenOwner(string owner)
        {
            lock (_tokenOwnerLock)
            {
                if (_tokenOwner == owner)
                    return;

                _testTokens.Clear();
                _prodTokens.Clear();
                _tokenOwner = owner;
            }
        }

        #region References

        /// <summary>
        /// The reference stored on the VAT report: the Altinn instance id ("{partyId}/{guid}"), prefixed for the test environment
        /// </summary>
        public static string ToStoredReference(string instanceId, bool test)
        {
            return test ? NorwayVATReportConsts.TestReferencePrefix + instanceId : instanceId;
        }

        /// <summary>
        /// Splits a stored reference into the Altinn instance id and its environment. False when it is not an Altinn 3 instance id
        /// (empty, or a reference from the earlier Altinn 2 solution)
        /// </summary>
        public static bool TryParseReference(string reference, out string instanceId, out bool test)
        {
            instanceId = null;
            test = false;
            if (String.IsNullOrWhiteSpace(reference))
                return false;

            var id = reference.Trim();
            if (id.StartsWith(NorwayVATReportConsts.TestReferencePrefix, StringComparison.OrdinalIgnoreCase))
            {
                test = true;
                id = id.Substring(NorwayVATReportConsts.TestReferencePrefix.Length);
            }

            var parts = id.Split('/');
            if (parts.Length != 2 || !long.TryParse(parts[0], out _) || !Guid.TryParse(parts[1], out _))
                return false;

            instanceId = id;
            return true;
        }

        /// <summary>
        /// Classifies an existing Altinn instance before a new submission for the same period is created
        /// </summary>
        public static NorwayVATInstanceState ClassifyInstance(InstanceCreationResponse instance)
        {
            if (instance == null || instance.Status?.IsHardDeleted == true || instance.Status?.IsSoftDeleted == true)
                return NorwayVATInstanceState.Gone;

            if (instance.Process?.Ended != null)
                return NorwayVATInstanceState.Delivered;

            var taskType = instance.Process?.CurrentTask?.AltinnTaskType;
            if (String.Equals(taskType, "feedback", StringComparison.OrdinalIgnoreCase))
                return NorwayVATInstanceState.Processing;
            if (String.Equals(taskType, "data", StringComparison.OrdinalIgnoreCase) || String.Equals(taskType, "confirmation", StringComparison.OrdinalIgnoreCase))
                return NorwayVATInstanceState.Draft;

            return NorwayVATInstanceState.Unknown;
        }

        /// <summary>
        /// The envelope data element Altinn creates with the instance: the one with the envelope's data type, or the only element when there is just one
        /// </summary>
        public static InstanceCreationResponseData FindEnvelope(InstanceCreationResponse instance)
        {
            var data = instance?.Data;
            if (data == null || data.Length == 0)
                return null;

            return data.FirstOrDefault(x => String.Equals(x.DataType, EnvelopeDataType, StringComparison.OrdinalIgnoreCase)) ??
                   (data.Length == 1 ? data[0] : null);
        }

        #endregion

        /// <summary>
        /// Sends a validation request to Skatteetaten
        /// </summary>
        public async Task<NorwayVATAPIResponse<Valideringsresultat>> ValidateAsync(MvaMeldingDto mvaMelding, bool test)
        {
            InitializeUrls(test);

            //Authenticate
            var authResult = await AuthenticateAsync(false);
            if (!authResult.IsSuccess)
                return new NorwayVATAPIResponse<Valideringsresultat>(authResult.Error);

            //Serialize to XML
            var serializedXml = SerializeToXml(mvaMelding);

            //Make validation request
            var response = await SendWithRetryAsync(HttpMethod.Post, _validationUrl, serializedXml, "application/xml");
            if (!response.IsSuccess)
                return Fail<Valideringsresultat>(response);

            //Deserialize XML
            var validationResult = DeserializeFromXml<Valideringsresultat>(AsText(response.Result));

            return new NorwayVATAPIResponse<Valideringsresultat>(validationResult);
        }

        /// <summary>
        /// Performs a multi step submission of VAT return
        /// </summary>
        /// <param name="onInstanceCreated">Called as soon as the Altinn instance exists, so the caller can persist its id before the remaining steps run.
        /// Returns an error text to stop the submission (the instance then stays an unsubmitted draft), or null to continue</param>
        public async Task<NorwayVATAPIResponse<InstanceCreationResponse>> SubmitAsync(InstanceCreationRequest instanceCreationRequest, MvaMeldingInnsending mvaMelding, MvaMeldingDto mvaMeldingDto,
                                                                                      bool test, Func<InstanceCreationResponse, Task<string>> onInstanceCreated = null)
        {
            InitializeUrls(test);

            //Authenticate
            var authResult = await AuthenticateAltinnAsync(false);
            if (!authResult.IsSuccess)
                return new NorwayVATAPIResponse<InstanceCreationResponse>(authResult.Error);

            //Create instance. Trailing slash as in the plugin: a redirect would drop the Authorization header
            var instanceCreationResponse = await SendWithRetryAsync(HttpMethod.Post, _instancesBaseUrl + "/", JsonConvert.SerializeObject(instanceCreationRequest), "application/json", true);
            if (!instanceCreationResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(instanceCreationResponse);

            var instanceCreationResponseObj = JsonConvert.DeserializeObject<InstanceCreationResponse>(AsText(instanceCreationResponse.Result));
            var instanceUrl = $"{_instancesBaseUrl}/{instanceCreationResponseObj.Id}";

            //Persist the id first. If a later step fails the draft can still be found in Altinn and in Uniconta
            if (onInstanceCreated != null)
            {
                var persistError = await onInstanceCreated(instanceCreationResponseObj);
                if (persistError != null)
                    return new NorwayVATAPIResponse<InstanceCreationResponse>(persistError);
            }

            //Upload submission into the envelope element Altinn created with the instance
            var envelope = FindEnvelope(instanceCreationResponseObj);
            if (envelope == null)
                return new NorwayVATAPIResponse<InstanceCreationResponse>(Uniconta.ClientTools.Localization.lookup("NorwayVATNoEnvelopeDataElement"));

            var vatReturnSubmissionUrl = $"{instanceUrl}/data/{envelope.Id}";

            var vatReturnSubmissionResponse = await SendWithRetryAsync(HttpMethod.Put, vatReturnSubmissionUrl, SerializeToXml(mvaMelding), "application/xml", true);
            if (!vatReturnSubmissionResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(vatReturnSubmissionResponse);

            //Upload VAT return
            var vatReturnUrl = $"{instanceUrl}/data?datatype=mvamelding";

            var vatReturnResponse = await SendWithRetryAsync(HttpMethod.Post, vatReturnUrl, SerializeToXml(mvaMeldingDto), "text/xml", true, "attachment", NorwayVATReportConsts.VatReturnFileName);
            if (!vatReturnResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(vatReturnResponse);

            //Complete data filling
            var completeDataFillingResponse = await SendWithRetryAsync(HttpMethod.Put, $"{instanceUrl}/process/next", String.Empty, "application/json", true);
            if (!completeDataFillingResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(completeDataFillingResponse);

            //Complete VAT return submission
            var completeVATReturnSubmissionResponse = await SendWithRetryAsync(HttpMethod.Put, $"{instanceUrl}/process/next", String.Empty, "application/json", true);
            if (!completeVATReturnSubmissionResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(completeVATReturnSubmissionResponse);

            return new NorwayVATAPIResponse<InstanceCreationResponse>(instanceCreationResponseObj);
        }

        /// <summary>
        /// Reads an Altinn instance. A failed result has StatusCode NotFound when the instance does not exist (anymore)
        /// </summary>
        public async Task<NorwayVATAPIResponse<InstanceCreationResponse>> GetInstanceAsync(string partyInstanceId, bool test)
        {
            InitializeUrls(test);

            var authResult = await AuthenticateAltinnAsync(false);
            if (!authResult.IsSuccess)
                return new NorwayVATAPIResponse<InstanceCreationResponse>(authResult.Error);

            var instanceResponse = await SendWithRetryAsync(HttpMethod.Get, $"{_instancesBaseUrl}/{partyInstanceId}", null, null, true);
            if (!instanceResponse.IsSuccess)
                return Fail<InstanceCreationResponse>(instanceResponse);

            return new NorwayVATAPIResponse<InstanceCreationResponse>(JsonConvert.DeserializeObject<InstanceCreationResponse>(AsText(instanceResponse.Result)));
        }

        /// <summary>
        /// Deletes an unsubmitted (draft) Altinn instance. Altinn refuses to delete an instance whose process has ended
        /// </summary>
        public async Task<NorwayVATReportAPIResponse> DeleteInstanceAsync(string partyInstanceId, bool test)
        {
            InitializeUrls(test);

            var authResult = await AuthenticateAltinnAsync(false);
            if (!authResult.IsSuccess)
                return authResult;

            var deleteResponse = await SendWithRetryAsync(HttpMethod.Delete, $"{_instancesBaseUrl}/{partyInstanceId}?hard=true", null, null, true);
            return deleteResponse.IsSuccess ? new NorwayVATReportAPIResponse(true) : Fail<byte[]>(deleteResponse);
        }

        /// <summary>
        /// Reads the Altinn instance. Processing is finished when process.ended is set.
        /// kvittering, valideringsresultat and betalingsinformasjon are data elements on the instance. They are only downloaded once
        /// processing has finished, so polling while Skatteetaten works reads the instance and nothing else
        /// </summary>
        public async Task<NorwayVATAPIResponse<NorwayVATReportFeedback>> GetFeedbackAsync(string partyInstanceId, bool test)
        {
            var instanceResult = await GetInstanceAsync(partyInstanceId, test);
            if (!instanceResult.IsSuccess)
                return Fail<NorwayVATReportFeedback>(instanceResult);

            var instance = instanceResult.Result;
            var instanceUrl = $"{_instancesBaseUrl}/{partyInstanceId}";

            var feedbackObj = new NorwayVATReportFeedback()
            {
                IsCompleted = instance?.Process?.Ended != null,
                Ended = instance?.Process?.Ended,
                CurrentTask = instance?.Process?.CurrentTask?.Name
            };

            if (!feedbackObj.IsCompleted)
                return new NorwayVATAPIResponse<NorwayVATReportFeedback>(feedbackObj);

            var data = instance.Data ?? new InstanceCreationResponseData[0];

            //Payment information
            var paymentData = data.FirstOrDefault(x => x.DataType == "betalingsinformasjon");
            if (paymentData != null)
            {
                var paymentResponse = await SendWithRetryAsync(HttpMethod.Get, $"{instanceUrl}/data/{paymentData.Id}", null, null, true);
                if (!paymentResponse.IsSuccess)
                    return Fail<NorwayVATReportFeedback>(paymentResponse);

                feedbackObj.PaymentInformationXml = AsText(paymentResponse.Result);
            }

            //Validation result
            var validationResultData = data.FirstOrDefault(x => x.DataType == "valideringsresultat");
            if (validationResultData != null)
            {
                var validationResultDataResponse = await SendWithRetryAsync(HttpMethod.Get, $"{instanceUrl}/data/{validationResultData.Id}", null, null, true);
                if (!validationResultDataResponse.IsSuccess)
                    return Fail<NorwayVATReportFeedback>(validationResultDataResponse);

                feedbackObj.ValidationResultXml = AsText(validationResultDataResponse.Result);
                feedbackObj.ValidationResult = DeserializeFromXml<Valideringsresultat>(feedbackObj.ValidationResultXml);
            }

            //Receipt PDF, uploaded once Skatteetaten has processed the submission
            var receiptData = data.FirstOrDefault(x => x.DataType == "kvittering");
            if (receiptData != null)
            {
                var receiptDataResponse = await SendWithRetryAsync(HttpMethod.Get, $"{instanceUrl}/data/{receiptData.Id}", null, null, true);
                if (!receiptDataResponse.IsSuccess)
                    return Fail<NorwayVATReportFeedback>(receiptDataResponse);

                feedbackObj.ReceiptPDFData = receiptDataResponse.Result;
            }

            return new NorwayVATAPIResponse<NorwayVATReportFeedback>(feedbackObj);
        }

        #region Helpers

        /// <summary>
        /// Initialize urls and token store for the environment. Called once at the start of each operation with the environment captured by the caller,
        /// so the whole operation stays in one environment
        /// </summary>
        private void InitializeUrls(bool test)
        {
            if (test)
            {
                _clientId = NorwayVATReportConsts.TestClientId;
                _authority = NorwayVATReportConsts.TestAuthority;
                _validationUrl = NorwayVATReportConsts.TestValidationEndPoint;
                _altinnTokenExchangeUrl = NorwayVATReportConsts.TestAltinnTokenExchangeEndPoint;
                _instancesBaseUrl = NorwayVATReportConsts.TestAltinnInstancesBaseEndPoint;
                _tokens = _testTokens;
            }
            else
            {
                _clientId = NorwayVATReportConsts.ProdClientId;
                _authority = NorwayVATReportConsts.ProdAuthority;
                _validationUrl = NorwayVATReportConsts.ProdValidationEndPoint;
                _altinnTokenExchangeUrl = NorwayVATReportConsts.ProdAltinnTokenExchangeEndPoint;
                _instancesBaseUrl = NorwayVATReportConsts.ProdAltinnInstancesBaseEndPoint;
                _tokens = _prodTokens;
            }

            _redirectUrl = NorwayVATReportConsts.RedirectUrl;
        }

        /// <summary>
        /// A failed response of another type, keeping the error text and HTTP status
        /// </summary>
        private static NorwayVATAPIResponse<T> Fail<T>(NorwayVATReportAPIResponse failed)
        {
            return new NorwayVATAPIResponse<T>(failed.Error) { StatusCode = failed.StatusCode };
        }

        /// <summary>
        /// Response body as text. Skatteetaten and Altinn answer in UTF-8. A byte order mark is dropped, as ReadAsStringAsync did
        /// </summary>
        private static string AsText(byte[] body)
        {
            return body == null ? String.Empty : new UTF8Encoding(false).GetString(body).TrimStart('\uFEFF');
        }

        /// <summary>
        /// Sends via http client and retries once if authentication fails. The response is read completely and disposed here
        /// </summary>
        private async Task<NorwayVATAPIResponse<byte[]>> SendWithRetryAsync(HttpMethod method, string requestUri, string content, string contentType, bool altinn = false,
                                                                            string contentDispositionType = "", string contentDispositionFileName = "")
        {
            using (var request = GenerateHttpRequestMessage(method, requestUri, content, contentType, CurrentBearer(altinn), contentDispositionType, contentDispositionFileName))
            using (var response = await _httpClient.SendAsync(request))
            {
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    return await ProcessHttpResponseMessageAsync(response);
            }

            //Renew bearer token and try again
            var authResponse = altinn ? await AuthenticateAltinnAsync(true) : await AuthenticateAsync(true);
            if (!authResponse.IsSuccess)
                return new NorwayVATAPIResponse<byte[]>(authResponse.Error);

            //Retry send request with the new token
            using (var request = GenerateHttpRequestMessage(method, requestUri, content, contentType, CurrentBearer(altinn), contentDispositionType, contentDispositionFileName))
            using (var response = await _httpClient.SendAsync(request))
                return await ProcessHttpResponseMessageAsync(response);
        }

        private string CurrentBearer(bool altinn)
        {
            return altinn ? _tokens.AltinnAccessToken : _tokens.IdPortenAccessToken;
        }

        /// <summary>
        /// Generates http request message for the requested Http Method, url, content and bearer token.
        /// Content is sent as UTF-8 bytes with a bare media type: Altinn rejects "; charset=utf-8" with 415
        /// </summary>
        private static HttpRequestMessage GenerateHttpRequestMessage(HttpMethod method, string requestUri, string content, string contentType, string bearer,
                                                                     string contentDispositionType = "", string contentDispositionFileName = "")
        {
            var httpRequestMessage = new HttpRequestMessage(method, requestUri);

            if (!String.IsNullOrEmpty(bearer))
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);

            //null means no body (GET). An empty string is sent as an empty body with its content type, as process/next expects
            if (content != null)
            {
                httpRequestMessage.Content = new ByteArrayContent(new UTF8Encoding(false).GetBytes(content));
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                if (!String.IsNullOrEmpty(contentDispositionType) &&
                    !String.IsNullOrEmpty(contentDispositionFileName))
                {
                    httpRequestMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(contentDispositionType)
                    {
                        FileName = contentDispositionFileName
                    };
                }
            }

            return httpRequestMessage;
        }

        /// <summary>
        /// Process HTTP response message
        /// </summary>
        private static async Task<NorwayVATAPIResponse<byte[]>> ProcessHttpResponseMessageAsync(HttpResponseMessage responseMessage)
        {
            var body = responseMessage.Content != null ? await responseMessage.Content.ReadAsByteArrayAsync() : new byte[0];

            if (!responseMessage.IsSuccessStatusCode)
            {
                var responseContent = AsText(body);
                string error;

                //The logged in person lacks the Altinn access package for VAT reporting for this organisation
                if (responseMessage.StatusCode == HttpStatusCode.Forbidden)
                    error = $"{Uniconta.ClientTools.Localization.lookup("NorwayVATNoAltinnRights")}{Environment.NewLine}{responseContent}".Trim();
                else if (!String.IsNullOrEmpty(responseContent))
                    error = responseContent;
                else
                    error = $"{responseMessage.StatusCode}:{responseMessage.ReasonPhrase}";

                return new NorwayVATAPIResponse<byte[]>(error) { StatusCode = responseMessage.StatusCode };
            }

            return new NorwayVATAPIResponse<byte[]>(body);
        }

        /// <summary>
        /// Stores ID-porten tokens and drops the Altinn token, which was exchanged from the previous ID-porten token
        /// </summary>
        private void StoreIdPortenTokens(string accessToken, string refreshToken, DateTimeOffset expiration)
        {
            _tokens.IdPortenAccessToken = accessToken;
            _tokens.IdPortenRefreshToken = refreshToken;
            _tokens.IdPortenAccessTokenExpiration = expiration.AddSeconds(-30);

            _tokens.AltinnAccessToken = null;
        }

        /// <summary>
        /// Reads "exp" from a JWT. Falls back to the given lifetime if the token is opaque
        /// </summary>
        private static DateTimeOffset GetTokenExpiration(string jwt, TimeSpan fallback)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2)
                    return DateTimeOffset.Now.Add(fallback);

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var exp = Newtonsoft.Json.Linq.JObject.Parse(json).Value<long?>("exp");

                return exp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(exp.Value).AddSeconds(-30)
                                    : DateTimeOffset.Now.Add(fallback);
            }
            catch
            {
                return DateTimeOffset.Now.Add(fallback);
            }
        }

        /// <summary>
        /// Makes sure there is a valid ID-porten token: cached token, silent refresh, or a new login.
        /// force skips the cached token (it was rejected)
        /// </summary>
        private async Task<NorwayVATReportAPIResponse> AuthenticateAsync(bool force)
        {
            var tokens = _tokens;

            //If there is a valid access token, short-circuit
            if (!force && tokens.HasValidIdPortenToken)
                return new NorwayVATReportAPIResponse(true);

            var rejectedToken = tokens.IdPortenAccessToken;

            await _loginLock.WaitAsync();
            try
            {
                //Another page may have logged in while this one waited
                if (tokens.HasValidIdPortenToken && (!force || tokens.IdPortenAccessToken != rejectedToken))
                    return new NorwayVATReportAPIResponse(true);

                var oidcClient = CreateOidcClient();

                //Silent refresh. Only works when the ID-porten client is registered with the refresh_token grant
                if (!String.IsNullOrEmpty(tokens.IdPortenRefreshToken))
                {
                    var refreshResult = await oidcClient.RefreshTokenAsync(tokens.IdPortenRefreshToken);
                    if (!refreshResult.IsError)
                    {
                        StoreIdPortenTokens(refreshResult.AccessToken, refreshResult.RefreshToken ?? tokens.IdPortenRefreshToken, refreshResult.AccessTokenExpiration);
                        return new NorwayVATReportAPIResponse(true);
                    }

                    tokens.IdPortenRefreshToken = null;
                }

                //prompt=login: always let the user choose who signs in, instead of silently reusing the browser's ID-porten session
                var loginRequest = new LoginRequest()
                {
                    FrontChannelExtraParameters = new Parameters(new Dictionary<string, string>()
                    {
                        { "prompt", "login" },
                        { "acr_values", "idporten-loa-substantial" },
                        { "ui_locales", "nb" }
                    })
                };

                var result = await oidcClient.LoginAsync(loginRequest);
                if (result.IsError)
                    return new NorwayVATReportAPIResponse(false, $"{result.Error} {result.ErrorDescription}".Trim());

                StoreIdPortenTokens(result.AccessToken, result.RefreshToken, result.AccessTokenExpiration);

                return new NorwayVATReportAPIResponse(true);
            }
            finally
            {
                _loginLock.Release();
            }
        }

        /// <summary>
        /// Exchanges the ID-porten token for an Altinn token. force skips the cached Altinn token (it was rejected).
        /// A new ID-porten login is only needed when the ID-porten token itself has expired or is rejected by the exchange
        /// </summary>
        private async Task<NorwayVATReportAPIResponse> AuthenticateAltinnAsync(bool force)
        {
            //If there is a valid access token, short-circuit
            if (!force && _tokens.HasValidAltinnToken)
                return new NorwayVATReportAPIResponse(true);

            var idPortenResponse = await AuthenticateAsync(false);
            if (!idPortenResponse.IsSuccess)
                return idPortenResponse;

            var exchangeResponse = await ExchangeForAltinnTokenAsync();

            //The exchange rejected the ID-porten token: log in again and retry once
            if (exchangeResponse == HttpStatusCode.Unauthorized)
            {
                idPortenResponse = await AuthenticateAsync(true);
                if (!idPortenResponse.IsSuccess)
                    return idPortenResponse;

                exchangeResponse = await ExchangeForAltinnTokenAsync();
            }

            if (exchangeResponse != HttpStatusCode.OK)
                return new NorwayVATReportAPIResponse(false, _lastExchangeError);

            return new NorwayVATReportAPIResponse(true);
        }

        private string _lastExchangeError;

        /// <summary>
        /// Calls Altinn's token exchange with the ID-porten token and stores the Altinn token. Returns the HTTP status
        /// </summary>
        private async Task<HttpStatusCode> ExchangeForAltinnTokenAsync()
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, _altinnTokenExchangeUrl))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.IdPortenAccessToken);

                using (var response = await _httpClient.SendAsync(request))
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _lastExchangeError = !String.IsNullOrEmpty(responseContent) ? responseContent : $"{response.StatusCode}: {response.ReasonPhrase}";
                        return response.StatusCode;
                    }

                    //The token is returned as plain text; some Altinn versions quote it
                    _tokens.AltinnAccessToken = responseContent.Trim().Trim('"');

                    //Altinn documents that the exchanged token lives exactly as long as the ID-porten access token
                    _tokens.AltinnAccessTokenExpiration = GetTokenExpiration(_tokens.AltinnAccessToken, TimeSpan.FromMinutes(2));

                    return HttpStatusCode.OK;
                }
            }
        }

        /// <summary>
        /// Serializes given object to XML (UTF-8, no indentation)
        /// </summary>
        public static string SerializeToXml<T>(T obj) where T : class, new()
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = false,
                OmitXmlDeclaration = false,
                Encoding = new UTF8Encoding(false),
                NewLineHandling = NewLineHandling.Replace,
                NewLineChars = ""
            };

            var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (var memoryStream = new MemoryStream())
            {
                using (var xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSettings))
                {
                    xmlSerializer.Serialize(xmlWriter, obj);
                }

                return new UTF8Encoding(false).GetString(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Deserialize from xml string to object
        /// </summary>
        public static T DeserializeFromXml<T>(string content) where T : class, new()
        {
            var xmlDeserializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var stringReader = new StringReader(content))
            {
                return xmlDeserializer.Deserialize(stringReader) as T;
            }
        }

        /// <summary>
        /// Builds the OIDC client for ID-porten. The login opens in the system browser and returns on the loopback redirect URI
        /// </summary>
        private OidcClient CreateOidcClient()
        {
            var options = new OidcClientOptions
            {
                Authority = _authority,
                ClientId = _clientId,
                Scope = NorwayVATReportConsts.Scope,
                RedirectUri = _redirectUrl,
                Browser = new NorwayVATLoopbackBrowser()
            };

            //ID-porten serves its endpoints from other hosts than the issuer, so endpoint validation must stay off
            options.Policy.Discovery.ValidateEndpoints = false;

            return new OidcClient(options);
        }

        #endregion
    }
}
