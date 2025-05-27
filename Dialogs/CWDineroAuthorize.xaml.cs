using ImportingTool.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using static ImportingTool.Model.Dinero;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWDineroAuthorize.xaml
    /// </summary>
    public partial class CWDineroAuthorize : ChildWindow
    {
        public HttpClientHelper ClientHelper;
        internal CompanyModel DineroCompany;

        const string REDIRECT_URI = "https://web.uniconta.com/DineroCode.htm";
        string CLIENT_ID;
        string CLIENT_SECRET_CODE;

        public CWDineroAuthorize()
        {
            InitializeComponent();
            Loaded += CWDineroAuthorize_Loaded;
            webViewer.WebViewerPageLoadUrlCompleted += WebViewer_WebViewerPageLoadUrlCompleted;
        }

        private async void WebViewer_WebViewerPageLoadUrlCompleted(object sender, WebViewerPageLoadUrlArgs e)
        {
            if (e.WebViewerUrl != null && e.WebViewerUrl.Contains(REDIRECT_URI))
            {
                var uri = new Uri(e.WebViewerUrl);
                var code = HttpUtility.ParseQueryString(uri.Query).Get("Code");
                if (!string.IsNullOrEmpty(code))
                    await LoadCompanies(code);
                else
                {
                    System.Windows.MessageBox.Show(Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"));
                    return;
                }
            }
        }

        private async void CWDineroAuthorize_Loaded(object sender, RoutedEventArgs e)
        {
            var ses = BasePage.session;
            var s = await ses.GetKeys();
            if (s != null)
            {
                CLIENT_ID = s[0];
                CLIENT_SECRET_CODE = s[1];
                Connect();
            }
        }

        private void Connect()
        {
            var consentUrl = $"https://connect.visma.com/consent?returnUrl=" + $"{HttpUtility.UrlEncode("/connect/authorize/callback?")}" +
                $"{HttpUtility.UrlEncode("response_type=code&")}" + $"{HttpUtility.UrlEncode($"client_id={CLIENT_ID}&")}" +
                $"{HttpUtility.UrlEncode("scope=dineropublicapi:read dineropublicapi:write offline_access&")}" +
                $"{HttpUtility.UrlEncode($"redirect_uri={REDIRECT_URI}")}";

            webViewer.UriSource = new Uri(consentUrl);
            webViewer.Visibility = Visibility.Visible;
        }

        async private Task LoadCompanies(string authorizeCode)
        {
            try
            {
                // Step 4: Exchange Authorization Code for Bearer Token
                HttpClient client = new HttpClient();
                string tokenUrl = "https://connect.visma.com/connect/token";
                var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", authorizeCode), new KeyValuePair<string, string>("redirect_uri", REDIRECT_URI),
                new KeyValuePair<string, string>("client_id", CLIENT_ID), new KeyValuePair<string, string>("client_secret", CLIENT_SECRET_CODE)});

                busyIndicator.IsBusy = true;
                HttpResponseMessage response = await client.PostAsync(tokenUrl, content);
                busyIndicator.IsBusy = false;
                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    var bearerToken = JObject.Parse(responseString)["access_token"].ToString();

                    ClientHelper = new HttpClientHelper();
                    ClientHelper.AddBearerToken(bearerToken);

                    var relativeUrl = $"/v1/organizations?fields=Name,Id,Type,IsPro,IsPayingPro,IsVatFree,Email,Phone,Street,City,ZipCode,AttPerson,IsTaxFreeUnion,VatNumber";
                    string responseData = await ClientHelper.GetAsync(relativeUrl);

                    if (responseData != null)
                    {
                        var companies = JsonConvert.DeserializeObject<List<CompanyModel>>(responseData);
                        lbCompanies.ItemsSource = companies;
                        lbCompanies.Visibility = Visibility.Visible;
                    }
                    //ERIK, do not dispose it
                    //ClientHelper.Dispose();
                }
                else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"), Uniconta.ClientTools.Localization.lookup("Error"));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void lbCompanies_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (lbCompanies.SelectedItem == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Company")),
                    Uniconta.ClientTools.Localization.lookup("Warning"));
                e.Handled = false;
            }

            if (lbCompanies.SelectedItem != null)
            {
                DineroCompany = lbCompanies.SelectedItem as CompanyModel;
                DialogResult = true;
            }
            else
                DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape || (e.Key == Key.Enter && CancelBtn.IsFocused))
                SetDialogResult(false);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            webViewer.CloseUnicontaWebViewer();
            base.OnClosing(e);
        }
    }
}
