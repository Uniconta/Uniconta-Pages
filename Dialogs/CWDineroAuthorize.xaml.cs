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
        const string CLIENT_ID = "isv_7N_Marketing_App";
        const string CLIENT_SECRET_CODE = "2MPnKTv6HkGMTIV99gTP01IGCMYeHAyWq8d6TmFalBQ8S0EqaFahE8aLv76xIKU3";

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
                    MessageBox.Show(Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"));
                    return;
                }
            }
        }

        private void CWDineroAuthorize_Loaded(object sender, RoutedEventArgs e)
        {
            var consentUrl = $"https://connect.visma.com/consent?returnUrl=" + $"{HttpUtility.UrlEncode("/connect/authorize/callback?")}" +
                $"{HttpUtility.UrlEncode("response_type=code&")}" + $"{HttpUtility.UrlEncode($"client_id={CLIENT_ID}&")}" +
                $"{HttpUtility.UrlEncode("scope=dineropublicapi:read dineropublicapi:write offline_access&")}" +
                $"{HttpUtility.UrlEncode($"redirect_uri={REDIRECT_URI}")}";

            webViewer.UriSource = new Uri(consentUrl);
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
                }
                else
                    MessageBox.Show(Uniconta.ClientTools.Localization.lookup("Invalid"));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                MessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Company")));
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

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
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
