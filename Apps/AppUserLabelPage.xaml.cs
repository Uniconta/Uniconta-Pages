using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppUserLabelGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppUserLabelClient); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new AppLabelSort(); } }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var newRow = dataEntity as AppUserLabelClient;
            if (newRow != null)
                newRow._Language = Uniconta.ClientTools.Localization.LanguageCode;
        }
    }

    public partial class AppUserLabelPage : GridBasePage
    {
        public override string NameOfControl { get { return UnicontaTabs.AppUserLabelPage; } }

        public AppUserLabelPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgUserLabel);
            dgUserLabel.api = api;
            dgUserLabel.UpdateMaster(App.MainPageObj.ActiveApp);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgUserLabel.AddRow();
                    break;
                case "CopyRow":
                    dgUserLabel.CopyRow();
                    break;
                case "DeleteRow":
                    dgUserLabel.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    //UpdateLocalDict();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        //void UpdateLocalDict()
        //{
        //    Uniconta.ClientTools.Localization.ResetAllUserLabels();
        //    Language _lan = (Language)255;
        //    Uniconta.ClientTools.Localization loc = null;
        //    var lst = dgUserLabel.ItemsSource as IEnumerable<AppUserLabelClient>;
        //    var cnt = lst.Count();
        //    foreach (var row in lst)
        //    {
        //        if (row._Language != _lan)
        //        {
        //            _lan = row._Language;
        //            loc = Uniconta.ClientTools.Localization.GetLocalization((Language)_lan);
        //        }
        //        if (row._Label != null && row._Text != null)
        //            loc.AddUserLabel(row._Label, row._Text, cnt);
        //    }
        //}
    }
}
