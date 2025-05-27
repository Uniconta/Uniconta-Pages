using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransExportedPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransExportedClient); } }
        public override System.Collections.IComparer GridSorting => new GLTransExportedClient.Sort();
    }

    public partial class GLTransExportedPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransExportedPage; } }

        public GLTransExportedPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgGLTransExported;
            SetRibbonControl(localMenu, dgGLTransExported);
            dgGLTransExported.api = api;
            dgGLTransExported.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (!api.CompanyEntity.DATEV)
            {
                ribbonControl.DisableButtons("DatevVerbinden");
                ribbonControl.DisableButtons("Upload");
                ribbonControl.DisableButtons("ConnectedApplications");
                ribbonControl.DisableButtons("ShowLog");

            }
            if (api.CompanyEntity._Country != (byte)CountryCode.Germany)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                if (rb != null)
                    UtilDisplay.RemoveMenuCommand(rb, new string[] { "Upload", "DatevVerbinden", "ConnectedApplications", "ShowLog" });
            }
            if (DatevDetails.AccessToken != null && DatevDetails.Tokenvalidto > DateTime.Now)
            {
                setShowHideGreen(true);
                GetUser(null, Convert.ToString(API.CompanyId), DatevDetails.AccessToken);
                string clientId = UtilDisplay.GetKey("DatevClientId");
                SetStatusText();
            }
            else
            {
                Revoke(null, Convert.ToString(API.CompanyId));
                setShowHideGreen(false);
                DatevDetails.AccessToken = null;
                DatevDetails.IdentityToken = null;
                DatevDetails.RefreshToken = null;
                DatevDetails.Tokenvalidto = DateTime.MinValue;
                DatevDetails.ToTime = null;
                DatevDetails.ToService = null;
                DatevDetails.Send = false;
                SetStatusText();
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.GLTransPage)
                dgGLTransExported.UpdateItemSource(argument);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLTransExported.SelectedItem as GLTransExportedClient;
            switch (ActionType)
            {
                case "NewExport":
                    var today = GetSystemDefaultDate();
                    var lst = (ICollection<GLTransExportedClient>)dgGLTransExported.ItemsSource;
                    var maxDate = (lst != null && lst.Count > 0) ? (from rec in lst select rec._ToDate).Max() : today.AddYears(-1).AddDays(-today.Day);

                    var start = maxDate.AddDays(1);
                    var end = start.AddMonths(1).AddDays(-1);
                    CWInterval winInterval = new CWInterval(start, end);
                    winInterval.Closed += delegate
                    {
                        if (winInterval.DialogResult == true)
                        {
                            var glTransExported = new GLTransExportedClient();
                            glTransExported.SetMaster(api.CompanyEntity);
                            glTransExported._FromDate = winInterval.FromDate;
                            glTransExported._ToDate = winInterval.ToDate;
                            AddDockItem(TabControls.GLTransPage, new object[] { glTransExported, "NewExport" }, Uniconta.ClientTools.Localization.lookup("AccountsTransaction"));
                        }
                    };
                    winInterval.Show();
                    break;
                case "NewSupplement":
                    if (selectedItem != null)
                    {
                        var glTransExported = new GLTransExportedClient();
                        glTransExported.SetMaster(api.CompanyEntity);
                        glTransExported._FromDate = selectedItem._FromDate;
                        glTransExported._ToDate = selectedItem._ToDate;
                        glTransExported._MinJournalPostedId = selectedItem._MaxJournalPostedId + 1;
                        AddDockItem(TabControls.GLTransPage, new object[] { glTransExported, "NewExport" }, Uniconta.ClientTools.Localization.lookup("SupplementaryTransactions"));
                    }
                    break;
                case "DeleteExport":
                    if (selectedItem != null)
                        DeleteRecord(selectedItem);
                    break;
                case "ShowExport":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransPage, new object[] { selectedItem, "ShowExport" }, Uniconta.ClientTools.Localization.lookup("AccountsTransaction"));
                    break;
                case "ShowSupplement":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransPage, new object[] { selectedItem, "ShowSupplement" }, Uniconta.ClientTools.Localization.lookup("SupplementaryTransactions"));
                    break;
                case "DatevHeader":
                    SaveDatev();
                    break;
                case "DatevVerbinden":
                    if (selectedItem != null)
                        ConnectToDatev(selectedItem);
                    break;
                case "Upload":
                    if (!string.IsNullOrEmpty(DatevDetails.AccessToken) && DatevDetails.Tokenvalidto > DateTime.Now)
                    {
                        if (UnicontaMessageBox.Show(String.Format("Möchten Sie {0:d} - {1:d} Version {3} ({2}) zu DATEV hochladen?", selectedItem._FromDate, selectedItem._ToDate, selectedItem.Comment, selectedItem._SuppVersion), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                        {
                            if ((!string.IsNullOrEmpty(selectedItem._SendToDatevDOC)) || (!string.IsNullOrEmpty(selectedItem.SendToDatevEXTF)))
                                if (UnicontaMessageBox.Show(String.Format("Sie haben {0:d} - {1:d} Version {3} ({2}) bereits zu DATEV hochgeladen. Möchten Sie noch einmal hochladen?", selectedItem._FromDate, selectedItem._ToDate, selectedItem.Comment, selectedItem._SuppVersion), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                {
                                    FilUpload(selectedItem);
                                    break;
                                }
                                else
                                    break;
                            FilUpload(selectedItem);
                            break;
                        }
                        else
                            break;
                    }
                    else
                        UnicontaMessageBox.Show("Der DATEV Zugangstoken ist ungültig oder abgelaufen", "Fehler", MessageBoxButton.OK);
                    break;
                case "ConnectedApplications":
                    Process.Start("https://apps.datev.de/tokrevui");
                    break;
                case "ShowLog":
                    AddDockItem(TabControls.DatevLogPage, selectedItem, Uniconta.ClientTools.Localization.lookup("DatevLog"));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveDatev()
        {
            var datev = await UnicontaClient.Pages.GLTransPage.CreateDatevHeader(api);
            var cw = new CwDatevHeaderParams(datev.Consultant, datev.Client, datev.Path, datev.DefaultAccount, datev.LanguageId, datev.FiscalYearBegin, datev.Active, api);
            cw.Closed += delegate
            {
                if (cw.DialogResult == true)
                {
                    datev.Consultant = cw.Consultant;
                    datev.Client = cw.Client;
                    datev.Path = cw.Path;
                    datev.DefaultAccount = cw.DefaultAccount;
                    datev.LanguageId = cw.LanguageId;
                    datev.FiscalYearBegin = cw.FiscalYearBegin;
                    datev.Active = cw.Active; //Per
                    UnicontaClient.Pages.GLTransPage.SaveDatevHeader(datev, api);
                }
            };
            cw.Show();
        }
        public async void GetUser(GLTransExportedClient selectedItem, string CompID, string token)
        {
            byte status = 0;
            var clientu = new HttpClient();
            var requestu = new HttpRequestMessage(HttpMethod.Get, "https://api.datev.de/userinfo");
            requestu.Headers.Add("X-DATEV-Client-Id", clientId);
            requestu.Headers.Add("Authorization", "Bearer " + token);
            requestu.Headers.Add("Cookie", "DATEV_SECURE=1");
            var responseu = await clientu.SendAsync(requestu);

            if (responseu.IsSuccessStatusCode == false)
            {
                setShowHideGreen(false);
                DatevDetails.ToTime = "";
                DatevDetails.ToService = "";
                DatevDetails.User = "";
                DatevDetails.Send = false;
                SetStatusText();
                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show("Sie sind nun von DATEV abgemeldet", "DATEV", MessageBoxButton.OK);
                return;

            }
            else
            {
                var Output = responseu.Content.ReadAsStringAsync().Result;
                var User = JsonConvert.DeserializeObject<Root>(Output);
                DatevDetails.User = User.name;
                status = 1;
                SetStatusText();
                InsDatevLog(selectedItem, DateTime.Now, "https://api.datev.de/userinfo HttpMethod.Get", "X-DATEV-Client-Id" + clientId, "", status, Output);
            }

        }

        public async void ConnectToDatev(GLTransExportedClient selectedItem)
        {
            var datev = await CreateDatevHeader();
            if (datev.Active != true)
            {
                UnicontaMessageBox.Show("Sie haben in DATEV-Informationen nicht bestätigt, dass bei DATEV das Produkt DATEV Buchungsdatenservice aktiviert ist", "Fehler", MessageBoxButton.OK);
                return;
            }
            busyIndicator.IsBusy = true;
            byte status = 0;
            if (!string.IsNullOrEmpty(DatevDetails.AccessToken) && DatevDetails.Tokenvalidto > DateTime.Now)
            {
                var UCService = selectedItem.DatevService;
                if ((UCService == "Belege" || UCService == "Buchungen & Belege") && (DatevDetails.Send == true) && (selectedItem.SendToDatevDOC == null))
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show("Sie können sich nicht von DATEV abmelden, solange noch Dokumente hochgeladen werden", "Fehler", MessageBoxButton.OK);
                    return;
                }
                else
                {
                    Revoke(selectedItem, Convert.ToString(selectedItem.CompanyId));
                    DatevDetails.Send = false;
                    SetStatusText();
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show("Sie sind nun von DATEV abgemeldet", "DATEV", MessageBoxButton.OK);
                    return;
                }
            }

            try
            {
                var loginResult = await LoginToDatev(api);
                if (loginResult == null)
                {
                    setShowHideGreen(false);
                    UnicontaMessageBox.Show("Fehler beim Anmelden an DATEV", "Fehler", MessageBoxButton.OK);
                    status = 1;
                }
                else if (!loginResult.IsError)
                {
                    DatevDetails.AccessToken = loginResult.AccessToken;
                    DatevDetails.IdentityToken = loginResult.IdentityToken; // ???
                    DatevDetails.RefreshToken = loginResult.RefreshToken;
                    DatevDetails.SendToDatevStatus = "";
                    DatevDetails.Tokenvalidto = loginResult.AccessTokenExpiration.DateTime.AddMinutes(-1);
                    status = 1;
                }
                else
                {
                    setShowHideGreen(false);
                    UnicontaMessageBox.Show("Fehler beim Anmelden an DATEV " + loginResult.Error, "Fehler",
                        MessageBoxButton.OK);
                    status = 1;
                }
            }
            catch (Exception e)
            {
                setShowHideGreen(false);
                UnicontaMessageBox.Show("Fehler beim Anmelden an DATEV " + e.Message, "Fehler", MessageBoxButton.OK);
                status = 1;

            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
            if (string.IsNullOrEmpty(DatevDetails.AccessToken))
            {
                setShowHideGreen(false);
                UnicontaMessageBox.Show("Der DATEV Zugangstoken ist ungültig oder abgelaufen", "Fehler", MessageBoxButton.OK);
                status = 1;
            }
            else
                TestUpload(selectedItem);
            InsDatevLog(selectedItem, DateTime.Now, "Anmeldung an https://login.datev.de/openid", "", "", status, "");

        }

        private string clientId = "";
        private string clientSecret = "";

        public async void Revoke(GLTransExportedClient selectedItem, string CompID)
        {

            string revokeEndpoint = "https://api.datev.de/revoke";
            clientId = UtilDisplay.GetKey("DatevClientId");
            clientSecret = UtilDisplay.GetKey("DatevClientSecret");
            string requestBody = $"token={DatevDetails.AccessToken}&token_type_hint=access_token";

            //Now, let's revoke the token
            RevokeToken(selectedItem, requestBody, revokeEndpoint);
            //Now, let's revoke the refresh token
            requestBody = $"token={DatevDetails.RefreshToken}&token_type_hint=refresh_token";
            RevokeToken(selectedItem, requestBody, revokeEndpoint);
            setShowHideGreen(false);
            DatevDetails.ToTime = "";
            DatevDetails.ToService = "";
            DatevDetails.User = "";
            DatevDetails.Send = false;
            SetStatusText();
            DatevDetails.AccessToken = null;
            DatevDetails.IdentityToken = null;
            DatevDetails.RefreshToken = null;
            DatevDetails.Tokenvalidto = DateTime.MinValue;
        }
        public async void RevokeToken(GLTransExportedClient selectedItem, string requestBody, string revokeEndpoint)
        {
            byte status = 0;
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")));
                try
                {
                    var content = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");
                    HttpResponseMessage response = await client.PostAsync(revokeEndpoint, content);

                    var Status = Convert.ToString(response.StatusCode);
                    if (!response.IsSuccessStatusCode)
                    {
                        status = 3;
                        UnicontaMessageBox.Show($"Fehler beim Widerruf des Zugangstokens: {response.StatusCode} - {response.ReasonPhrase}", "Fehler", MessageBoxButton.OK);
                    }
                    else
                        status = 1;
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK);
                    status = 3;
                    busyIndicator.IsBusy = false;
                }
            }
            InsDatevLog(selectedItem, DateTime.Now, revokeEndpoint, "X-DATEV-Client-Id " + clientId, "Encoding.UTF8 application/x-www-form-urlencoded", status, "");
        }
        private void setShowHideGreen(bool hideGreen)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "DatevVerbinden");
            if (ibase == null)
                return;
            if (hideGreen)
            {
                ibase.Caption = string.Concat(Uniconta.ClientTools.Localization.lookup("UnRegister"), " ", Uniconta.ClientTools.Localization.lookup("von DATEV"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("datevconnectg");
            }
            else
            {
                ibase.Caption = string.Concat(Uniconta.ClientTools.Localization.lookup("Register"), " ", Uniconta.ClientTools.Localization.lookup("an DATEV"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("datevconnectb");
            }
        }


        public async Task<LoginResult> LoginToDatev(BaseAPI api)
        {
            var Sandbox = false;
            string authority;
            string clientId = "";
            string clientSecret = "";
            var datevEndpointBaseAddresses = new List<string>(2);

            if (Sandbox)
            {
                authority = "https://login.datev.de/openidsandbox";
                clientId = UtilDisplay.GetKey("DatevClientIdTest");
                clientSecret = UtilDisplay.GetKey("DatevClientSecretTest");
                datevEndpointBaseAddresses.Add("https://login.datev.de/");
                datevEndpointBaseAddresses.Add("https://sandbox-api.datev.de");
            }
            else
            {
                authority = "https://login.datev.de/openid";
                clientId = UtilDisplay.GetKey("DatevClientId");
                clientSecret = UtilDisplay.GetKey("DatevClientSecret");
                datevEndpointBaseAddresses.Add("https://login.datev.de/");
                datevEndpointBaseAddresses.Add("https://api.datev.de");
            }

            string clientScopes = "openid profile email accounting:documents datev:accounting:clients datev:accounting:extf-files-import";

            int GetRandomUnusedPort()
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                return port;
            }
            var clientRedirectPort = GetRandomUnusedPort();

            var options = new OidcClientOptions()
            {
                Authority = authority,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = clientScopes, // Scope for Online API accounting:clients
                RedirectUri = $"http://localhost:{clientRedirectPort}/redirect/{Guid.NewGuid()}/", // Redirect URI with random path to avoid collisions on multi user environments like WTS
                Policy = new Policy()
                {
                    Discovery = new DiscoveryPolicy()
                    {
                        AdditionalEndpointBaseAddresses = datevEndpointBaseAddresses
                    }
                },
                TokenClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader, // credentials not in body
                RefreshTokenInnerHttpHandler = new HttpClientHandler() // usage of own httpclienthandler possible (i. e. for http-proxys)
            };

            string GenerateNonce()
            {
                var rnd = new CryptoRandom(typeof(GLTransExportedPage).GetHashCode());
                return $"{rnd.Next(100_000_000, 1_000_000_000):D9}{rnd.Next(100_000_000, 1_000_000_000):D9}{rnd.Next(100_000, 1_000_000):D6}";
            }

            var tokenSource = new CancellationTokenSource();
            CWBrowserDialog dialog = null;
            try
            {
                short timeoutInSeconds = 100; // after this timeout, login will be canceled
                tokenSource.CancelAfter(TimeSpan.FromSeconds(timeoutInSeconds));
                var client = new OidcClient(options);
                var nonce = GenerateNonce();
                var state = await client.PrepareLoginAsync(new Parameters()
                {
                    {OidcConstants.AuthorizeRequest.Nonce, nonce}, // add missing mandatory parameter
                    {OidcConstants.AuthorizeRequest.ResponseMode, "query"}, // add missing parameter, because otherwise we need to parse request body, so we get the auth details as query parameter
                }, cancellationToken: tokenSource.Token);
                if (state.IsError)
                {
                    throw new Exception($"Cannot preprocess login: {state.Error} - {state.ErrorDescription}");
                }

                using (var http = new HttpListener())
                {
                    // automatically stop the http listener after timeout
                    Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds), tokenSource.Token)
                        .ContinueWith(_ =>
                        {
                            if (http?.IsListening == true)
                                http.Stop();
                        });

                    http.Prefixes.Add(client.Options.RedirectUri);
                    http.Start();
                    Console.WriteLine("Listening for Browser Redirect...");
                    string startUrlWithSuffix = state.StartUrl
                        // replace reponse_type, because otherwise it gets appended and we have two times this parameter
                        .Replace($"{OidcConstants.AuthorizeRequest.ResponseType}=code", $"{OidcConstants.AuthorizeRequest.ResponseType}=code%20id_token")
                        // Parameter enableWindowsSso for login.datev.de:
                        // if you have DATEV-software with Kommunikationsserver installed, you can login with DATEV-Benutzer (DID).
                        + "&enableWindowsSso=true";
                    //var startInfo = new ProcessStartInfo(startUrlWithSuffix);
                    //startInfo.UseShellExecute = true;
                    Console.WriteLine($"Start browser with URL '{startUrlWithSuffix}'");
                    dialog = new CWBrowserDialog(startUrlWithSuffix, "DATEV Login");

                    Dispatcher.BeginInvoke((Action)(() => dialog.Show()));
                    //Process.Start(startInfo);

                    var context = await http.GetContextAsync();
                    // handle OPTIONS request (PNA / CORS preflight request)
                    if (context.Request.HttpMethod == "OPTIONS")
                    {
                        //Console.WriteLine($"Request with {context.Request.HttpMethod} instead of expected GET");
                        var optionsResponse = context.Response;
                        var requestHeaders = context.Request.Headers;
                        var origin = requestHeaders["Origin"];
                        if (origin != null && origin.EndsWith(".datev.de"))
                        {
                            optionsResponse.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
                            optionsResponse.AppendHeader("Access-Control-Allow-Methods", "GET, OPTIONS");
                            optionsResponse.AppendHeader("Access-Control-Max-Age", "86400");
                            optionsResponse.AppendHeader("Access-Control-Allow-Origin", origin);
                            if (requestHeaders["Access-Control-Request-Private-Network"] != null)
                            {
                                optionsResponse.AppendHeader("Access-Control-Allow-Private-Network", "true");
                            }
                        }

                        optionsResponse.StatusCode = 204;
                        optionsResponse.Close();
                        context = await http.GetContextAsync();
                    }

                    // sends an HTTP response to the browser.
                    var response = context.Response;
                    var redirectServerUrl = "https://web.uniconta.com/datev.html";
                    response.RedirectLocation = redirectServerUrl;
                    response.StatusCode = 307; // temporary redirect
                    string responseString = $"<html lang=\"en\"><head><title>DATEV Login finished</title><meta http-equiv=\"refresh\" content=\"0; url={redirectServerUrl}\" /><script type=\"text/javascript\">window.location.href = \"{redirectServerUrl}\"</script></head><body>Please return to Uniconta.</body></html>";
                    var buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    var responseOutput = response.OutputStream;
                    await responseOutput.WriteAsync(buffer, 0, buffer.Length, tokenSource.Token);
                    responseOutput.Close();
                    var query = context.Request.Url?.Query;
                    Console.WriteLine($"Query: {query}");
                    var result = await client.ProcessResponseAsync(query, state, cancellationToken: tokenSource.Token);
                    if (result.IsError)
                    {
                        throw new Exception($"Error: {result.Error} - {result.ErrorDescription}");
                    }

                    // check nonce
                    var jwtTokenHandler = new JwtSecurityTokenHandler();
                    var jwt = jwtTokenHandler.ReadJwtToken(result.IdentityToken);
                    if (!nonce.Equals(jwt.Payload.Nonce))
                    {
                        throw new ApplicationException("Nonce is wrong, could not login");
                    }

                    Console.WriteLine($"Access token:\n{result.AccessToken}, expires {result.AccessTokenExpiration}");
                    if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                    {
                        Console.WriteLine($"Refresh token:\n{result.RefreshToken}");
                    }

                    // we are finished, so we can stop our http listener
                    http.Stop();

                    // Close the browser dialog automatically after 5 seconds (only on success)
                    await Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                        Dispatcher.BeginInvoke((Action)(() => dialog?.Close()))
                    );

                    return result;
                }

            }
            catch (Exception ex)
            {
                // try to close dialog on any error
                try
                {
                    Dispatcher.BeginInvoke((Action)(() => dialog?.Close()));
                }
                catch (Exception) { }

                if (ex is ApplicationException || ex is TaskCanceledException || ex is HttpListenerException)
                    throw new ApplicationException("Anmeldevorgang abgebrochen", ex);
                api.ReportException(ex, "Fehler bei der Verbindung mit login.datev.de");
            }
            finally
            {
                tokenSource.Cancel();
            }
            return null;
        }
        async Task<GLTransPage.DatevHeader> CreateDatevHeader()
        {
            return await UnicontaClient.Pages.GLTransPage.CreateDatevHeader(api);
        }

        public async void TestUpload(GLTransExportedClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            byte status = 0;
            var statusText = "";
            var datev = await CreateDatevHeader();
            var url = "https://accounting-clients.api.datev.de/platform/v2/clients/" + datev.Consultant + "-" + datev.Client;

            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-DATEV-Client-Id", clientId);
            request.Headers.Add("Authorization", "Bearer " + DatevDetails.AccessToken);
            request.Headers.Add("Cookie", "DATEV_SECURE=1");
            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                DatevDetails.AccessToken = DatevDetails.IdentityToken = DatevDetails.RefreshToken = null;
                DatevDetails.Tokenvalidto = DateTime.MinValue;
                status = 1;
                UnicontaMessageBox.Show("Sie sind erfolgreich von DATEV abgemeldet", "DATEV", MessageBoxButton.OK);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                status = 3;
                statusText = String.Format("Die Kombination Mandant {0} und Berater {1} ist bei DATEV nicht vorhanden ", datev.Client, datev.Consultant);
                UnicontaMessageBox.Show(statusText, "Fehler", MessageBoxButton.OK);
                return;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                status = 3;
                UnicontaMessageBox.Show("Der DATEV Zugriffstoken ist ungültig oder abgelaufen", "Fehler", MessageBoxButton.OK);
                return;
            }
            var Output = response.Content.ReadAsStringAsync().Result;

            var myclient = JsonConvert.DeserializeObject<Root>(Output);

            if (myclient.consultant_number != Convert.ToInt32(datev.Consultant) || (myclient.id != Convert.ToInt32(datev.Consultant) + "-" + datev.Client))
            {
                DatevDetails.AccessToken = DatevDetails.IdentityToken = DatevDetails.RefreshToken = null;
                DatevDetails.Tokenvalidto = DateTime.MinValue;
                status = 3;
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                statusText = String.Format("Die Kombination Mandant {0} und Berater {1} ist bei DATEV nicht vorhanden ", datev.Client, datev.Consultant);
                UnicontaMessageBox.Show(statusText, "Fehler", MessageBoxButton.OK);
                return;
            }
            var UCServiceText = "";

            var numofservice = myclient.services.Count;
            for (var i = 0; i < numofservice; i++)
                if (myclient.services[i] != null)
                {
                    var serso = myclient.services[i].scopes;
                    var sertxt = serso[0];
                    if (sertxt == "accounting:documents")
                        DatevDetails.UCService = 2;
                    if (sertxt == "datev:accounting:extf-files-import")
                        DatevDetails.UCService = DatevDetails.UCService + 1;
                }

            if (DatevDetails.UCService == 0)
                UCServiceText = Uniconta.ClientTools.Localization.lookup("NoOptions");
            if (DatevDetails.UCService == 1)
                UCServiceText = Uniconta.ClientTools.Localization.lookup("PostedTransactions");
            if (DatevDetails.UCService == 2)
                UCServiceText = Uniconta.ClientTools.Localization.lookup("Vouchers");
            if (DatevDetails.UCService == 3)
                UCServiceText = Uniconta.ClientTools.Localization.lookup("PostedTransactions") + " & " + Uniconta.ClientTools.Localization.lookup("Vouchers");
            DatevDetails.DatevServiceText = UCServiceText;

            var clientu = new HttpClient();
            var requestu = new HttpRequestMessage(HttpMethod.Get, "https://api.datev.de/userinfo");
            requestu.Headers.Add("X-DATEV-Client-Id", clientId);
            requestu.Headers.Add("Authorization", "Bearer " + DatevDetails.AccessToken);
            requestu.Headers.Add("Cookie", "DATEV_SECURE=1");
            var responseu = await clientu.SendAsync(requestu);
            responseu.EnsureSuccessStatusCode();
            Output = responseu.Content.ReadAsStringAsync().Result;
            var User = JsonConvert.DeserializeObject<Root>(Output);
            DatevDetails.ToTime = DatevDetails.Tokenvalidto.ToString("dd.MM.yy HH:mm:ss");
            DatevDetails.ToService = UCServiceText;
            DatevDetails.User = User.name;
            SetStatusText();
            setShowHideGreen(true);
            busyIndicator.IsBusy = false;
            UnicontaMessageBox.Show("Anmelden an DATEV erfolgreich", "DATEV", MessageBoxButton.OK);
            status = 1;
            InsDatevLog(selectedItem, DateTime.Now, "https://api.datev.de/userinfo" + " Method.Get", "X-DATEV-Client-Id " + clientId + "Cookie", "DATEV_SECURE=1", status, Output);

        }
        async void DeleteRecord(GLTransExportedClient selectedItem)
        {
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                busyIndicator.IsBusy = true;
                var err = await api.Delete(selectedItem);
                busyIndicator.IsBusy = false;
                if (err != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(err);
                else
                    InitQuery();
            }
        }
        int MaxJournalPostId;
        public async void FilUpload(GLTransExportedClient selectedItem)
        {
            byte status = 0;
            var statusText = "";
            if (DatevDetails.AccessToken != null)
            {
                DatevDetails.Send = true;
                SetStatusText();
                if (selectedItem._SuppJournalPostedId != 0)
                    MaxJournalPostId = selectedItem._SuppJournalPostedId;
                else
                    MaxJournalPostId = selectedItem._MaxJournalPostedId;
                var UCServiceText = DatevDetails.DatevServiceText;
                var services = new List<string>();
                if (UCServiceText == Uniconta.ClientTools.Localization.lookup("NoOptions"))
                    DatevDetails.UCService = 0;
                if (UCServiceText == Uniconta.ClientTools.Localization.lookup("PostedTransactions") || (UCServiceText == Uniconta.ClientTools.Localization.lookup("PostedTransactions") + " & " + Uniconta.ClientTools.Localization.lookup("Vouchers")))
                {
                    services.Add(Uniconta.ClientTools.Localization.lookup("PostedTransactions"));
                    DatevDetails.UCService = 1;
                }
                if (UCServiceText == Uniconta.ClientTools.Localization.lookup("Vouchers") || (UCServiceText == Uniconta.ClientTools.Localization.lookup("PostedTransactions") + " & " + Uniconta.ClientTools.Localization.lookup("Vouchers")))
                {
                    services.Add(Uniconta.ClientTools.Localization.lookup("Vouchers"));
                    DatevDetails.UCService = 2;
                }
                if (UCServiceText == Uniconta.ClientTools.Localization.lookup("PostedTransactions") + " & " + Uniconta.ClientTools.Localization.lookup("Vouchers"))
                {
                    services.Add(UCServiceText);

                    DatevDetails.UCService = 3;
                }

                if (DatevDetails.UCService > 0)
                {
                    var cwComboBox = new CWComboBoxSelector(Uniconta.ClientTools.Localization.lookup("Upload"), services.ToArray());
                    cwComboBox.Closed += async delegate
                    {
                        if (cwComboBox.DialogResult == true && DatevDetails.UCService == 3)
                            DatevDetails.UCService = cwComboBox.SelectedItemIndex + 1;
                        if (cwComboBox.DialogResult == false)
                            return;
                        var datev = await CreateDatevHeader();
                        var url = "Https://accounting-extf-files.api.datev.de/platform/v3/clients/" + Convert.ToInt32(datev.Consultant) + "-" + datev.Client + "/extf-files/import";
                        var partname = "";
                        var anz = 0;
                        var err = "";
                        string[] loc = new string[3];
                        busyIndicator.IsBusy = true;
                        var client = new HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(300);
                        if (DatevDetails.UCService == 1 || DatevDetails.UCService == 3)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                if (i == 0)
                                    partname = "EXTF_Buchungstextkonstanten_" + selectedItem._ToDate.ToString("ddMMyyyy") + "_" + MaxJournalPostId;
                                if (i == 1)
                                    partname = "EXTF_DebitorenKreditoren_" + selectedItem._ToDate.ToString("ddMMyyyy") + "_" + MaxJournalPostId;
                                if (i == 2)
                                    partname = "EXTF_Buchungsstapel_" + selectedItem._ToDate.ToString("ddMMyyyy") + "_" + MaxJournalPostId;

                                if (!File.Exists(datev.Path + "\\" + partname + ".csv"))
                                {
                                    status = 3;
                                    InsDatevLog(selectedItem, DateTime.Now, url + "/extf-files/ import " + " Method.Post", "extf-file " + datev.Path + "\\" + partname + ".csv" + " Content-Type " + "application/octet-stream", "", status, "No files available");
                                    busyIndicator.IsBusy = false;
                                    UnicontaMessageBox.Show("Keine Buchungsdateien gefunden", "Error", MessageBoxButton.OK);
                                    return;
                                }

                                try
                                {
                                    var request1 = new HttpRequestMessage(HttpMethod.Post, url);
                                    request1.Headers.Add("X-DATEV-Client-Id", clientId);
                                    request1.Headers.Add("Filename", partname + ".csv");
                                    request1.Headers.Add("Authorization", "Bearer " + DatevDetails.AccessToken);
                                    request1.Headers.Add("Cookie", "DATEV_SECURE=1");
                                    request1.Content = new StreamContent(File.OpenRead(datev.Path + "\\" + partname + "." + "csv"));
                                    var apiresult = await client.SendAsync(request1);

                                    var Status = Convert.ToString(apiresult.StatusCode);
                                    if (Status == "Accepted")
                                    {
                                        status = 2;
                                        statusText = Convert.ToString(apiresult.Headers.Location);
                                        var locID = Convert.ToString(apiresult.Headers.Location);
                                        var len = locID.Length;
                                        loc[anz] = locID.Substring(len - 36, 36);
                                        anz = anz + 1;
                                    }
                                    else
                                    {
                                        status = 3;
                                        statusText = Convert.ToString(apiresult.Content);
                                        setShowHideGreen(false);
                                        DatevDetails.ToTime = "";
                                        DatevDetails.ToService = "";
                                        DatevDetails.User = "";
                                        DatevDetails.Send = false;
                                        SetStatusText();

                                        DatevDetails.AccessToken = "";
                                        DatevDetails.IdentityToken = "";
                                        DatevDetails.RefreshToken = "";
                                        DatevDetails.Tokenvalidto = DateTime.MinValue;
                                        busyIndicator.IsBusy = false;
                                        UnicontaMessageBox.Show("Der DATEV Zugriffstoken ist ungültig oder abgelaufen", "Fehler", MessageBoxButton.OK);
                                        return;
                                    }
                                    InsDatevLog(selectedItem, DateTime.Now, url + "/extf-files/import" + " Method.Post", "/extf-files/ import" + "X-DATEV-Client-Id " + clientId + " Filename " + partname + ".csv", "extf-file " + datev.Path + "\\" + partname + ".csv" + " Content-Type " + "application/octet-stream", status, statusText);
                                }
                                catch (Exception e)
                                {
                                    UnicontaMessageBox.Show(String.Format("{0} Fehler ", e), "DATEV", MessageBoxButton.OK);
                                }
                            }

                            TestextF(selectedItem, loc, Convert.ToString(DatevDetails.AccessToken), datev.Consultant, datev.Client, clientId);
                        }

                        var UpText = "";
                        var UpTextEXTF = "";
                        if (DatevDetails.UCService == 2 || DatevDetails.UCService == 3)
                        {
                            UpText = "Die Belege werden vom Server hochgeladen. Sie können diese Seite verlassen, mit Uniconta an anderer Stelle weiterarbeiten. ";
                            selectedItem._SendToDatevDOC = "";
                        }
                        if (DatevDetails.UCService == 1 || DatevDetails.UCService == 3)
                        {
                            UpTextEXTF = String.Format("{0} Dateien (Stammdaten und Buchungen) gesendet. {1} ohne Fehler. {2} - {3}", anz, DatevDetails.SendOK, err, DateTime.Now);
                        }
                        selectedItem._SendToDatevEXTF = UpTextEXTF;
                        selectedItem._DatevService = Convert.ToByte(DatevDetails.UCService);
                        await api.Update(selectedItem);
                        busyIndicator.IsBusy = false;
                        gridRibbon_BaseActions("RefreshGrid");
                        busyIndicator.IsBusy = false;
                        UnicontaMessageBox.Show(UpTextEXTF + " " + UpText, "DATEV", MessageBoxButton.OK);
                        if (DatevDetails.UCService == 2 || DatevDetails.UCService == 3)
                        {
                            //Server Call UploadDatevDoc(selectedItem (or selectedItem._ToDate, selectedItem._SuppVersion) , DatevDetails.AccessToken, DatevDetails.RefreshToken); //Aunrag0 02.06
                            //PG Call to test pdf upload                                
                            var Cmp = api.CompanyEntity;
                            Cmp.SetUserField("AccessToken", DatevDetails.AccessToken);
                            Cmp.SetUserField("RefreshToken", DatevDetails.RefreshToken);
                            Cmp.SetUserField("MaxJournalPostedId", selectedItem._MaxJournalPostedId);
                            Cmp.SetUserField("SuppVersion", selectedItem.SuppVersion);
                            Cmp.SetUserField("ToDate", selectedItem._ToDate);
                            api.Update(Cmp);
                        }

                    };
                    cwComboBox.Show();
                }
            }
            else
            {
                setShowHideGreen(false);
                DatevDetails.ToTime = "";
                DatevDetails.ToService = "";
                DatevDetails.User = "";
                DatevDetails.Send = false;
                SetStatusText();
                UnicontaMessageBox.Show("Der DATEV Zugriffstoken ist ungültig oder abgelaufen", "Fehler", MessageBoxButton.OK);
            }
        }
        public async void InsDatevLog(GLTransExportedClient selectedItem, DateTime lDate, String conMet, String head, String body, byte resp, string respb)
        {
            if (selectedItem != null)
            {
                DatevLogClient log = new DatevLogClient();
                log.SetMaster(selectedItem);
                log._Time = DateTime.Now;
                log._HTTPConnMethod = conMet;
                log._HTTPHeader = head;
                log._HTTPBody = body;
                log._HTTPRESPONSECode = resp;
                log._HTTPRESPONSEBody = respb;
                await api.Insert(log);
            }

        }

        public void TestextF(GLTransExportedClient selectedItem, string[] loc, string token, string Consultant, string Client, string clientId)
        {
            byte status = 0;
            var statusText = "";
            System.Threading.Thread.Sleep(60000);
            DatevDetails.SendOK = 0;
            for (int i = 0; i < 3; i++)
            {
                var urlc = "https://accounting-extf-files.api.datev.de/platform/v3/clients/" + Convert.ToInt32(Consultant) + "-" + Client;
                var clientc = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, urlc + "/extf-files/jobs/" + loc[i]);
                request.Headers.Add("X-DATEV-Client-Id", clientId);
                request.Headers.Add("Authorization", "Bearer " + token);
                request.Headers.Add("Cookie", "DATEV_SECURE=1");
                var response = clientc.SendAsync(request).Result;
                var contentStream = response.Content.ReadAsStringAsync().Result;

                var respfiles = JsonConvert.DeserializeObject<Extf>(contentStream);

                var Status = Convert.ToString(response.StatusCode);

                if (respfiles.result == "succeeded")
                {
                    status = 1;
                    statusText = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.date_from + " " + respfiles.date_to + " " + respfiles.number_of_accounting_records;
                    DatevDetails.SendOK = DatevDetails.SendOK + 1;
                }
                else
                {
                    status = 3;
                    statusText = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.validation_details.detail;
                }
                InsDatevLog(selectedItem, DateTime.Now, urlc + "/extf-files/job/" + loc[i] + " Method.Get", "X-DATEV-Client-Id " + clientId, contentStream, status, statusText);
            }
        }
        void SetStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            foreach (var grp in groups)
            {
                if (grp.StatusValue == "a" || grp.Caption == Uniconta.ClientTools.Localization.lookup("ToTime"))
                {
                    grp.StatusValue = DatevDetails.ToTime;
                    continue;
                }
                else
                if (grp.StatusValue == "b" || grp.Caption == Uniconta.ClientTools.Localization.lookup("Service"))
                {
                    grp.StatusValue = DatevDetails.ToService;
                    continue;

                }
                else
                if (grp.StatusValue == "c" || grp.Caption == Uniconta.ClientTools.Localization.lookup("User"))
                {
                    grp.StatusValue = DatevDetails.User;
                    continue;
                }
                else
                if (grp.StatusValue == "d" || grp.Caption == Uniconta.ClientTools.Localization.lookup("DocSent"))
                {
                    if (DatevDetails.Send == true)
                        grp.StatusValue = "Ja";
                    else
                        grp.StatusValue = "Nein";
                }
            }
        }

    }

    public class Root
    {
        public int client_number { get; set; }
        public int consultant_number { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public List<Service> services { get; set; }
    }
    public class Extf
    {
        public string id { get; set; }
        public string filename { get; set; }
        public string client_application_display_name { get; set; }
        public string client_application_vendor { get; set; }
        public string data_category_id { get; set; }
        public string date_from { get; set; }
        public string date_to { get; set; }
        public string label { get; set; }
        public string number_of_accounting_records { get; set; }
        public string result { get; set; }
        public DateTime timestamp { get; set; }
        public ValidationDetails validation_details { get; set; }
    }
    public class Service
    {
        public string name { get; set; }
        public List<string> scopes { get; set; }
    }
    public class AffectedElement
    {
        public string name { get; set; }
        public string reason { get; set; }
    }
    public class ValidationDetails
    {
        public string type { get; set; }
        public string title { get; set; }
        public string detail { get; set; }
        public List<AffectedElement> affected_elements { get; set; }
    }

    public static class DatevDetails
    {
        public static string AccessToken;
        public static string IdentityToken;
        public static string RefreshToken;
        public static DateTime Tokenvalidto;
        public static string SendToDatevStatus;
        public static String ToTime;
        public static String ToService;
        public static String User;
        public static bool Send;
        public static int SendOK;
        public static int UCService;
        public static string DatevServiceText;
    }
}
