using UnicontaClient.Models;
using DevExpress.Diagram.Core.Native.Ribbon;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Utilities;
using System.Xml;
using System.ComponentModel;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for OIORASPTrackPage.xaml
    /// </summary>
    public partial class OIORASPTrackPage : FormBasePage
    {
        DebtorInvoiceClient _debInvoice;
        private ImportLogOIORASP _logs;
        public bool Terminate { get; set; }

        public override UnicontaBaseEntity ModifiedRow
        {
            get { return _debInvoice; }
            set { _debInvoice = (DebtorInvoiceClient)value; }
        }

        public override string NameOfControl => TabControls.ImportPhysicalVouchersPage;
        public override Type TableType => null;

        public override void OnClosePage(object[] refreshParams)
        {
        }

        public OIORASPTrackPage(ImportLogOIORASP _log)
        {
            InitializeComponent();
            this.DataContext = this;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "ImportData");
            UtilDisplay.RemoveMenuCommand(rb, "Terminate");

            this._logs = _log;
            this.DataContext = _logs;
            leLog.Visibility = Visibility.Visible;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "CopyData":
                    System.Windows.Forms.Clipboard.SetText(txtLogs.Text);
                    if (api.CompanyEntity._CountryId == CountryCode.Denmark)
                        UnicontaMessageBox.Show("Du kan nu Ctrl+V i dit ønskede textprogram", Uniconta.ClientTools.Localization.lookup("Message"));
                    else
                        UnicontaMessageBox.Show("You can now Ctrl+V in your choosen texteditor", Uniconta.ClientTools.Localization.lookup("Message"));
                    break;
            }
        }
    }

    public class ImportLogOIORASP : INotifyPropertyChanged
    {
        public string LogMsg
        {
            get { return LogMessage.ToString(); }
            set { NotifyPropertyChanged("LogMsg"); }
        }

        StringBuilder LogMessage = new StringBuilder();

        public void AppendLog(string msg)
        {
            LogMessage.Append(msg);
            LogMsg = LogMessage.ToString();
        }

        public void AppendLogLine(string msg)
        {
            LogMessage.AppendLine(msg);
            LogMsg = LogMessage.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
