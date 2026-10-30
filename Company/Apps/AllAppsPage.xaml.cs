using DevExpress.XtraReports.UI;
using DevExpress.XtraSpreadsheet.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppSort : IComparer<AppClientLocal>, IComparer
    {
        public int Compare(object x, object y) { return Compare((AppClientLocal)x, (AppClientLocal)y); }

        public int Compare(AppClientLocal x, AppClientLocal y)
        {
            if (x.IsSubscribed)
            {
                if (!y.IsSubscribed)
                    return -1;
                return x.AppId - y.AppId;
            }
            if (y.IsSubscribed)
                return 1;
            return y.AppId - x.AppId;
        }
    }

    public class AppClientLocal : AppClient
    {
        bool isSubsribed;
        [Display(Name = nameof(AppText.Subscribe), ResourceType = typeof(AppText))]
        public bool IsSubscribed
        {
            get { return isSubsribed; }
            set { isSubsribed = value; NotifyPropertyChanged("IsSubscribed"); }
        }
    }
    public class AllAppsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppClientLocal); } }
        public override bool IsAutoSave => false;
        public override IComparer GridSorting => new AppSort();
    }

    public partial class AllAppsPage : GridBasePage
    {
        public override string NameOfControl => TabControls.AllAppsPage;

        public AllAppsPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgGrid.api = api;
            dgGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        AppShare[] subscribedApps;
        public async override Task InitQuery()
        {
            var apps = (await api.Query<AppClientLocal>(new [] {PropValuePair.GenereteParameter("Subscribe", typeof(bool), "true")}));
            subscribedApps = await api.Query<Uniconta.DataModel.AppShare>();
            foreach (var s in subscribedApps)
            {
                if (s._AppId != 0)
                {
                    var app = apps.Where(a => a.AppId == s._AppId).FirstOrDefault();
                    if (app != null)
                        app.IsSubscribed = true;
                }
            }
            dgGrid.ItemsSource = apps;
            dgGrid.Visibility = System.Windows.Visibility.Visible;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGrid.SelectedItem as AppClientLocal;
            switch (ActionType)
            {
                case "Subscribe":
                    if (selectedItem?.IsSubscribed == false)
                        Subscribe(selectedItem);
                    break;
                case "ViewURL":
                    if (selectedItem == null)
                        return;
                    ViewAppURL(selectedItem);
                    break;
                case "Unsubscribe":
                    if (selectedItem?.IsSubscribed == true)
                        Unsubscribe(selectedItem);
                    break;
                //case "AppLog":
                //    if (selectedItem != null)
                //        AddDockItem(TabControls.AppLogPage, selectedItem, $"{Uniconta.ClientTools.Localization.lookup("Log")}: {selectedItem.Name}");
                //    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        public async void Unsubscribe(AppClientLocal app)
        {
            var confirmLabel = Uniconta.ClientTools.Localization.lookup("Unsubscribe");
            var promptLabel = string.Concat(Uniconta.ClientTools.Localization.lookup("WarningDataLost"), "\n", string.Format(Uniconta.ClientTools.Localization.lookup("TypeToStart"), confirmLabel));
            var cwConfirmationBox = new CWTypeConfirmationBox(promptLabel, confirmLabel);
            cwConfirmationBox.Closed += async delegate
            {
                if (cwConfirmationBox.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    var appShare = subscribedApps.Where(s => s._AppId == app.AppId).FirstOrDefault();
                    if (appShare != null)
                    {
                        var err = await api.Delete(appShare);
                        if (err == ErrorCodes.Succes)
                        {
                            app.IsSubscribed = false;
                            globalEvents.OnRefresh(NameOfControl, api.CompanyEntity.CompanyId);/*Setup Company again in mainpage */
                        }
                        else
                            UtilDisplay.ShowErrorCode(err);
                    }
                    busyIndicator.IsBusy = false;

                }
            };
            cwConfirmationBox.Show();
        }
        public async void Subscribe(AppClientLocal app)
        {
            if (!app.Free)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DisclaimerInConfiguration"), Uniconta.ClientTools.Localization.lookup("Information"));

            var appShare = new Uniconta.DataModel.AppShare
            {
                _AppId = app.AppId,
                _CompanyId = api.CompanyId,
            };
            var err = await api.Insert(appShare);
            if (err == ErrorCodes.Succes)
                app.IsSubscribed = true;
            else
                UtilDisplay.ShowErrorCode(err);
        }

        private void ViewAppURL(AppClient app)
        {
            var text = app._URL;
            if (string.IsNullOrEmpty(text))
                text = GetAppStoreURL(app.AppId);
            if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                text = "https://" + text;
            UnicontaClient.Utilities.Utility.OpenWebSite(text);
        }

        string GetAppStoreURL(int appid)
        {
            var countrycode = api.CompanyEntity._CountryId;
            Language language = Country2Language.Convert(countrycode);
            var storeSupportedLanguage = new[] { "da", "de", "nl", "nb", "is" };
            string lang = language.ToString().Substring(0, 2);
            if (!storeSupportedLanguage.Contains(lang))
                lang = "da";
            var url = $"https://partner.uniconta.com/{lang}/addons/?appid={appid}";
            return url;
        }
    }
}
