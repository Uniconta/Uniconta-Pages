using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections;
using Uniconta.API.Service;
using System.Globalization;
using Uniconta.ClientTools;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransLogGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransLogClient); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new GLTransLogSort(); } }
    }
    /// <summary>
    /// Interaction logic for GLTransLogGridPage.xaml
    /// </summary>
    public partial class GLTransLogPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransLogPage; } }

        public GLTransLogPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public GLTransLogPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgGLTranLogGridClient.api = api;
            dgGLTranLogGridClient.BusyIndicator = busyIndicator;
            dgGLTranLogGridClient.UpdateMaster(master);
            SetRibbonControl(localMenu, dgGLTranLogGridClient);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLTranLogGridClient.RowDoubleClick += DgGLTranLogGridClient_RowDoubleClick;
        }

        private void DgGLTranLogGridClient_RowDoubleClick()
        {
            localMenu_OnItemClicked("DeletedTransactions");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DgGLTranLogGridClient_RowDoubleClick();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLTranLogGridClient.SelectedItem as GLTransLogClient;
            switch (ActionType)
            {
                case "DeletedTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLTransDeletedReport, dgGLTranLogGridClient.syncEntity, string.Format("{0}: {1}/{2}", Uniconta.ClientTools.Localization.lookup("DeletedTransactions"), selectedItem._JournalPostedId, selectedItem._Voucher));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
    public class UnderlineConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string action = Convert.ToString(value);
            var val = (TransLogActions)AppEnums.TransLogActions.IndexOf(action);
            if (val == TransLogActions.DeleteJournal || val == TransLogActions.DeleteVoucher)
                return TextDecorations.Underline;
            else
                return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
