using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Pages.Maintenance;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GDPRSetupPage : FormBasePage
    {
        CompanySettingsClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.GDPRSetup; } }
        public override Type TableType { get { return typeof(CompanySettingsClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanySettingsClient)value; } }

        public GDPRSetupPage(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            InitializeComponent();
            leGDPRjoinCreditor.api= leGDPRjoinDebtor.api = api;
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            LoadType(new[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    this.saveForm();
                    break;
                case "CleanUp":
                    GDPRCleanUp();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void GDPRCleanUp()
        {
            closePageOnSave = false;
            saveForm();
            var cwobj = new CwSimulateGDPR();
            cwobj.Closing += async delegate
             {
                 if (cwobj.DialogResult == true)
                 {
                     var compApi = new CompanyAPI(api);
                     if (cwobj.Simulate)
                     {
                         var res = await compApi.GDPRTextCleanup();
                         if (res == null || res.Length == 0)
                             UtilDisplay.ShowErrorCode(res == null ? compApi.LastError : ErrorCodes.NoLinesFound);
                         else
                             AddDockItem(TabControls.DebtorCreditorGDPRTextCleanUp, new object[] { api, res }, string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("CleanUp"), Uniconta.ClientTools.Localization.lookup("Account")));
                     }
                     else
                     {
                         EraseYearWindow delDialog = new EraseYearWindow("", false);
                         delDialog.Closed += async delegate
                         {
                             if (delDialog.DialogResult == true)
                             {
                                 var res = await compApi.GDPRCleanup();
                                 UtilDisplay.ShowErrorCode(res);
                             }
                         };
                         delDialog.Show();
                     }
                 }
             };
            cwobj.Show();
        }
    }
}
