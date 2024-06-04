using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using Datev.Cloud.Authentication;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
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
            if (api.CompanyEntity._Country != (byte)CountryCode.Germany)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                if (rb != null)
                    UtilDisplay.RemoveMenuCommand(rb, new string[] { "Upload", "DatevVerbinden", "ConnectedApplications", "ShowLog" });  //PG 10.05
            }
            if (DatevDetails.AccessToken != null && DatevDetails.Tokenvalidto > DateTime.Now)
            {
                setShowHideGreen(true);
                DatevDetails.ToTime = DatevDetails.Tokenvalidto.ToString("dd.mm.yy HH:mm:ss");
                GetUser(null, Convert.ToString(API.CompanyId), DatevDetails.AccessToken); // PG 10.05  //GetUser(DatevDetails.AccessToken, Convert.ToString(API.CompanyId));              
                clientId = UtilDisplay.GetKey("DatevClientId"); //PG 10.05
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
                DatevDetails.Send = false; //Anurag 17.05
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
                case "Upload": //PG                  
                    FilUpload(selectedItem);
                    break;
                case "ConnectedApplications": //PG                                      
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
            var cw = new CwDatevHeaderParams(datev.Consultant, datev.Client, datev.Path, datev.DefaultAccount, datev.LanguageId, datev.FiscalYearBegin, api);
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
                    UnicontaClient.Pages.GLTransPage.SaveDatevHeader(datev, api);
                }
            };
            cw.Show();
        }
        public async void GetUser(GLTransExportedClient selectedItem, string CompID, string token) //PG
        {
            byte status = 0; // Anurag 26.05
            var clientu = new HttpClient();
            var requestu = new HttpRequestMessage(HttpMethod.Get, "https://api.datev.de/userinfo");
            requestu.Headers.Add("X-DATEV-Client-Id", clientId);
            requestu.Headers.Add("Authorization", "Bearer " + token);
            requestu.Headers.Add("Cookie", "DATEV_SECURE=1");
            var responseu = await clientu.SendAsync(requestu);
            responseu.EnsureSuccessStatusCode();
            var Output = responseu.Content.ReadAsStringAsync().Result;
            var serializeru = new JavaScriptSerializer();

            var User = serializeru.Deserialize<Root>(Output);
            DatevDetails.User = User.name;
            SetStatusText();
            InsDatevLog(selectedItem, DateTime.Now, "https://api.datev.de/userinfo HttpMethod.Get", "X-DATEV-Client-Id" + clientId, "", 1, Output);

        }

        public async void ConnectToDatev(GLTransExportedClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            byte status = 0;
            if (!string.IsNullOrEmpty(DatevDetails.AccessToken) && DatevDetails.Tokenvalidto > DateTime.Now)
            {
                var UCServiceText = DatevDetails.DatevServiceText;
                if (((UCServiceText == "Voucher Image Service") || (UCServiceText == "Booking Data Service and Voucher Image Service")) && (DatevDetails.Send == true) && (selectedItem.SendToDatevDOC == ""))
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show("You cannot log out of Datev before the server has uploaded the document!", "Error", MessageBoxButton.OK);
                    return;
                }
                else
                {
                    Revoke(selectedItem, Convert.ToString(selectedItem.CompanyId));
                    DatevDetails.Send = false;
                    SetStatusText();
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show("You is now logged out of Datev", "Datev", MessageBoxButton.OK);
                    return;
                }
            }

            try
            {
                string connectionResult = await Connect();
                if (connectionResult == null)
                    return;
                var array = connectionResult.Split(',');
                if (array[4] == "True")
                {
                    DatevDetails.AccessToken = array[0];
                    DatevDetails.IdentityToken = array[1];
                    DatevDetails.RefreshToken = array[2];
                    DatevDetails.SendToDatevStatus = "";
                    DateTime valdTok = Convert.ToDateTime(array[3]);
                    valdTok = valdTok.AddMinutes(11);
                    DatevDetails.Tokenvalidto = valdTok;
                    status = 1;

                }
                else
                {
                    setShowHideGreen(false);
                    UnicontaMessageBox.Show("Error when logging in to Datev " + array[5], "Error", MessageBoxButton.OK);
                    status = 1;
                }
            }
            catch (Exception e)
            {
                setShowHideGreen(false);
                UnicontaMessageBox.Show("Error when logging in to Datev " + e.Message, "Error", MessageBoxButton.OK);
                status = 1;
                busyIndicator.IsBusy = false;

            }
            if (string.IsNullOrEmpty(DatevDetails.AccessToken))
            {
                setShowHideGreen(false);
                UnicontaMessageBox.Show("The Datev token is invalid or expired", "Error", MessageBoxButton.OK);
                status = 1;
            }
            else
                TestUpload(selectedItem);
            InsDatevLog(selectedItem, DateTime.Now, "Login : to https://login.datev.de/openid", "", "", status, "");  //Anurag 25.05

        }
        public async void Revoke(GLTransExportedClient selectedItem, string CompID)
        {

            string revokeEndpoint = "https://api.datev.de/revoke";
            clientId = UtilDisplay.GetKey("DatevClientId"); //PG 10.05
            clientSecret = UtilDisplay.GetKey("DatevClientSecret"); //PG 10.05
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
        public async void RevokeToken(GLTransExportedClient selectedItem, string requestBody, string revokeEndpoint) //Anurag 25.05
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
                        UnicontaMessageBox.Show($"Error revoking access token: {response.StatusCode} - {response.ReasonPhrase}", "Error", MessageBoxButton.OK);
                    }
                    else
                        status = 1;
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK);
                    status = 3;
                    busyIndicator.IsBusy = false;
                }
            }
            InsDatevLog(selectedItem, DateTime.Now, revokeEndpoint, "X-DATEV-Client-Id " + clientId, requestBody, status, "");  //Anurag 25.05
        }
        private void setShowHideGreen(bool hideGreen)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "DatevVerbinden");
            if (ibase == null)
                return;
            if (hideGreen)
            {
                ibase.Caption = string.Concat(Uniconta.ClientTools.Localization.lookup("UnRegister"), " ", Uniconta.ClientTools.Localization.lookup("von Datev"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("datevconnectg");
            }
            else
            {
                ibase.Caption = string.Concat(Uniconta.ClientTools.Localization.lookup("Register"), " ", Uniconta.ClientTools.Localization.lookup("an Datev"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("datevconnectb");
            }
        }
        string clientId = "";
        string clientSecret = "";

        async Task<string> Connect()
        {
            var Sandbox = false;
            string authority = "";


            if (Sandbox)
            {
                authority = @"https://login.datev.de/openidsandbox";
                clientId = UtilDisplay.GetKey("DatevClientIdTest");
                clientSecret = UtilDisplay.GetKey("DatevClientSecretTest");
            }
            else
            {
                authority = @"https://login.datev.de/openid";
                clientId = UtilDisplay.GetKey("DatevClientId");
                clientSecret = UtilDisplay.GetKey("DatevClientSecret");
            }
            string clientScopes = "openid profile email accounting:documents datev:accounting:clients datev:accounting:extf-files-import";
            ushort clientRedirectPort = 58455;

            var options = new ClientOptions(
                    authority,
                    clientId,
                    clientSecret,
                    clientScopes,
                    clientRedirectPort);

            try
            {
                var client = new Datev.Cloud.Authentication.Client(options);
                var loginResult = await client.LoginAsync();
                var result = loginResult.AccessToken + "," + loginResult.IdentityToken + "," + loginResult.RefreshToken + "," + DateTime.Now + "," + loginResult.Success + "," + loginResult.Error;

                return result;
            }
            catch (InvalidOperationException)
            { }
            catch (Exception ex)
            {
                api.ReportException(ex, "Error while connecting login.datev.de");

            }
            busyIndicator.IsBusy = false;
            return null;
        }
        async Task<GLTransPage.DatevHeader> CreateDatevHeader()
        {
            var dh = await UnicontaClient.Pages.GLTransPage.CreateDatevHeader(api);
            var err = await api.Read(dh);
            return dh;
        }

        public async void TestUpload(GLTransExportedClient selectedItem) //PG
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
                UnicontaMessageBox.Show("You have successfully logged out of Datev", "Datev", MessageBoxButton.OK);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                status = 3;
                statusText = String.Format("Mandant {0} für Berater {1} ist nicht vorhanden beim Datev", datev.Client, datev.Consultant);
                UnicontaMessageBox.Show(statusText, "Fehler", MessageBoxButton.OK);
                return;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                status = 3;
                UnicontaMessageBox.Show("The Datev token is invalid or expired", "Error", MessageBoxButton.OK);
                return;
            }
            var Output = response.Content.ReadAsStringAsync().Result;

            var serializer = new JavaScriptSerializer();

            var myclient = serializer.Deserialize<Root>(Output);

            if (myclient.consultant_number != Convert.ToInt32(datev.Consultant) || (myclient.id != Convert.ToInt32(datev.Consultant) + "-" + datev.Client))
            {
                DatevDetails.AccessToken = DatevDetails.IdentityToken = DatevDetails.RefreshToken = null;
                DatevDetails.Tokenvalidto = DateTime.MinValue;
                status = 3;
                busyIndicator.IsBusy = false;
                setShowHideGreen(false);
                UnicontaMessageBox.Show(String.Format("{0} ", "401 Failed"), "Error", MessageBoxButton.OK);
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
                UCServiceText = "None";
            if (DatevDetails.UCService == 1)
                UCServiceText = "Booking Data Service";
            if (DatevDetails.UCService == 2)
                UCServiceText = "Voucher Image Service";
            if (DatevDetails.UCService == 3)
                UCServiceText = "Booking Data Service and Voucher Image Service";

            DatevDetails.DatevServiceText = UCServiceText;

            var clientu = new HttpClient();
            var requestu = new HttpRequestMessage(HttpMethod.Get, "https://api.datev.de/userinfo");
            requestu.Headers.Add("X-DATEV-Client-Id", clientId);
            requestu.Headers.Add("Authorization", "Bearer " + DatevDetails.AccessToken);
            requestu.Headers.Add("Cookie", "DATEV_SECURE=1");
            var responseu = await clientu.SendAsync(requestu);
            responseu.EnsureSuccessStatusCode();
            Output = responseu.Content.ReadAsStringAsync().Result;
            var serializeru = new JavaScriptSerializer();

            var User = serializeru.Deserialize<Root>(Output);
            DatevDetails.ToTime = DatevDetails.Tokenvalidto.ToString("dd.MM.yy HH:mm:ss");
            DatevDetails.ToService = UCServiceText;
            DatevDetails.User = User.name;
            SetStatusText();
            setShowHideGreen(true);
            busyIndicator.IsBusy = false;
            UnicontaMessageBox.Show("Login zu datev OK", "Datev", MessageBoxButton.OK);
            InsDatevLog(selectedItem, DateTime.Now, url + " Method.Get", "X-DATEV-Client-Id " + clientId, Convert.ToString(Output), status, Output);

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
        public async void FilUpload(GLTransExportedClient selectedItem) //PG
        {
            byte status = 0;
            var statusText = "";
            if (UnicontaMessageBox.Show(String.Format("Möchten Sie {0} zu DATEV Hochladen ?", selectedItem.Comment), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {

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
                    if (UCServiceText == "None")
                        DatevDetails.UCService = 0;
                    if (UCServiceText == "Booking Data Service")
                    {
                        services.Add(UCServiceText);
                        DatevDetails.UCService = 1;
                    }
                    if (UCServiceText == "Voucher Image Service")
                    {
                        services.Add(UCServiceText);
                        DatevDetails.UCService = 2;
                    }
                    if (UCServiceText == "Booking Data Service and Voucher Image Service")
                    {
                        services = new List<string> { "Booking Data Service", "Voucher Image Service", "Booking Data Service and Voucher Image Service" };
                        DatevDetails.UCService = 3;
                    }
                    if (DatevDetails.UCService > 0)
                    {
                        var cwComboBox = new CWComboBoxSelector(Uniconta.ClientTools.Localization.lookup("Upload"), services.ToArray());
                        cwComboBox.Closed += async delegate
                        {
                            if (cwComboBox.DialogResult == true && DatevDetails.UCService == 3)
                                DatevDetails.UCService = cwComboBox.SelectedItemIndex + 1;
                            var datev = await CreateDatevHeader();
                            var url = "Https://accounting-extf-files.api.datev.de/platform/v3/clients/" + Convert.ToInt32(datev.Consultant) + "-" + datev.Client + "/extf-files/import";
                            var partname = "";
                            var anz = 0;
                            var err = "";
                            string[] loc = new string[3];
                            busyIndicator.IsBusy = true; //PG 06.05
                            var client = new HttpClient();
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
                                        UnicontaMessageBox.Show("No files available", "Error", MessageBoxButton.OK);
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

                                        //log.SetMaster(selectedItem);
                                        //log.Date = DateTime.Now;
                                        //log.DateSent = DateTime.Now;
                                        //log.HTTPConnMethod = url + "/extf-files/import" + " Method.Post";
                                        //log.HTTPHeader = "X-DATEV-Client-Id " + clientId + " Filename " + partname + ".csv";
                                        //log.HTTPBody = "extf-file " + datev.Path + "\\" + partname + ".csv" + " Content-Type " + "application/octet-stream";

                                        var Status = Convert.ToString(apiresult.StatusCode);
                                        if (Status == "Accepted")
                                        {
                                            status = 2;
                                            statusText = Convert.ToString(apiresult.Headers.Location);
                                            //log.HTTPRESPONSEHTTPCode = "202 Processed";
                                            //log.HTTPRESPONSEHTTPBody = Convert.ToString(apiresult.Headers.Location);
                                            var locID = Convert.ToString(apiresult.Headers.Location);
                                            var len = locID.Length;
                                            loc[anz] = locID.Substring(len - 36, 36);
                                            anz = anz + 1;

                                        }
                                        else
                                        {
                                            status = 3;
                                            statusText = Convert.ToString(apiresult.Content);
                                            //log.HTTPRESPONSEHTTPCode = "401 Failed";
                                            //log.HTTPRESPONSEHTTPBody = Convert.ToString(apiresult.Content);
                                            //await api.Insert(log);
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
                                            busyIndicator.IsBusy = false; // PG 10.05
                                            UnicontaMessageBox.Show("The Datev token is invalid or expired", "Error", MessageBoxButton.OK);
                                            return;
                                        }
                                        //await api.Insert(log);
                                    }
                                    catch (Exception e)
                                    {
                                        UnicontaMessageBox.Show(String.Format("{0} error ", e), "Datev", MessageBoxButton.OK);
                                    }
                                }
                                InsDatevLog(selectedItem, DateTime.Now, url + "/extf-files/ import " + " Method.Post", "extf-file " + datev.Path + "\\" + partname + ".csv" + " Content-Type " + "application/octet-stream", "", status, statusText);
                                TestextF(selectedItem, loc, Convert.ToString(DatevDetails.AccessToken), datev.Consultant, datev.Client, clientId);  //Anurag 26.05
                            }

                            var UpText = "";
                            var UpTextEXTF = "";
                            if (DatevDetails.UCService == 2 || DatevDetails.UCService == 3)
                            {
                                UpText = "The document will be uploaded from the server";
                            }
                            else
                            {
                                UpTextEXTF = String.Format("{0} send files, and {1} it's ok {2} am {3}", anz, DatevDetails.SendOK, err, DateTime.Now); //Anurag 17.05
                            }
                            selectedItem._SendToDatevEXTF = UpTextEXTF;
                            selectedItem._DatevService = Convert.ToByte(DatevDetails.UCService);
                            await api.Update(selectedItem);
                            gridRibbon_BaseActions("RefreshGrid");
                            busyIndicator.IsBusy = false; // PG 06.05
                            UnicontaMessageBox.Show(UpTextEXTF + " " + UpText, "Datev", MessageBoxButton.OK);
                            if (DatevDetails.UCService == 2 || DatevDetails.UCService == 3)
                            {
                                // Server Call UploadDatevDoc(selectedItem, DatevDetails.AccessToken, DatevDetails.RefreshToken); //Aunrag 15.05
                                //PG Call to test pdf upload
                                var Cmp = api.CompanyEntity;
                                Cmp.SetUserField("AccessToken", DatevDetails.AccessToken);
                                Cmp.SetUserField("RefreshToken", DatevDetails.RefreshToken);
                                Cmp.SetUserField("MaxJournalPostedId", selectedItem._MaxJournalPostedId);
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
                    UnicontaMessageBox.Show("The Datev token is invalid or expired", "Error", MessageBoxButton.OK);
                }
            }
        }
        public async void InsDatevLog(GLTransExportedClient selectedItem, DateTime lDate, String conMet, String head, String body, byte resp, string respb) //PG
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

        public void TestextF(GLTransExportedClient selectedItem, string[] loc, string token, string Consultant, string Client, string clientId) //PG
        {
            byte status = 0;
            var statusText = "";
            System.Threading.Thread.Sleep(60000); //PG 10.05 to give Datev time to procees....
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

                var serializer = new JavaScriptSerializer();

                var respfiles = serializer.Deserialize<Extf>(contentStream); // her skal en Test om Jason ok ist!
                //Datevlog log = new Datevlog();                                             
                //log.Date = DateTime.Now;
                //log.DateSent = DateTime.Now;
                //log.HTTPConnMethod = urlc + "/extf-files/job/" + loc[i] + " Method.Get";
                //log.HTTPHeader = "X-DATEV-Client-Id " + clientId;
                //log.HTTPBody = urlc + "/extf-files/jobs" + " Method.Get";
                var Status = Convert.ToString(response.StatusCode);

                if (respfiles.result == "succeeded")
                {
                    status = 1;
                    statusText = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.date_from + " " + respfiles.date_to + " " + respfiles.number_of_accounting_records;
                    // log.HTTPRESPONSEHTTPCode = "200 Succeeded";
                    // log.HTTPRESPONSEHTTPBody = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.date_from + " " + respfiles.date_to + " " + respfiles.number_of_accounting_records;
                    DatevDetails.SendOK = DatevDetails.SendOK + 1;
                }
                else
                {
                    status = 3;
                    statusText = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.validation_details.detail;
                    // log.HTTPRESPONSEHTTPCode = "401 Failed";
                    // log.HTTPRESPONSEHTTPBody = respfiles.client_application_display_name + " " + respfiles.client_application_vendor + " " + respfiles.filename + " " + respfiles.id + " " + respfiles.validation_details.detail;
                }
                InsDatevLog(selectedItem, DateTime.Now, urlc + "/extf-files/job/" + loc[i] + " Method.Get", "", "", status, statusText);
                //api.Insert(log).GetAwaiter().GetResult();
            }
        }
        void SetStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            foreach (var grp in groups)
            {
                if (grp.StatusValue == "a" || grp.Caption == Uniconta.ClientTools.Localization.lookup("ToTime")) //PG 10.05
                {
                    grp.StatusValue = DatevDetails.ToTime;
                    continue;
                }
                else
                if (grp.StatusValue == "b" || grp.Caption == Uniconta.ClientTools.Localization.lookup("ToService")) //PG 10.05
                {
                    grp.StatusValue = DatevDetails.ToService;
                    continue;

                }
                else
                if (grp.StatusValue == "c" || grp.Caption == Uniconta.ClientTools.Localization.lookup("User")) //PG 10.05
                {
                    grp.StatusValue = DatevDetails.User;
                    continue;
                }
                else
                if (grp.StatusValue == "d" || grp.Caption == Uniconta.ClientTools.Localization.lookup("Send"))
                {
                    grp.StatusValue = Convert.ToString(DatevDetails.Send);
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
