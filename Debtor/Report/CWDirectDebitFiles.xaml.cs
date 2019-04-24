using Corasau.Client.Models;
using Corasau.Client.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;


namespace Corasau.Client.Pages
{
  
   
    public class DebtorPaymentFileGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorPaymentFileClient); } }

     //   public override bool Readonly { get { return true; } }
   //     public override IComparer GridSorting { get { return new LogTimeSort(); } }

    }

    public partial class CWDirectDebitFiles : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.DirectDebitFiles.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public CWDirectDebitFiles(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public CWDirectDebitFiles()
            : base(null)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            //localMenu.dataGrid = dgDirectDebitFilesGrid;
            //SetRibbonControl(localMenu, dgDirectDebitFilesGrid);
            dgDirectDebitFilesGrid.api = api;
            dgDirectDebitFilesGrid.BusyIndicator = busyIndicator;
            dgDirectDebitFilesGrid.api = api;
            //Utility.Refresh += Utility_Refresh;
            //localMenu.OnItemClicked += gridRibbon_BaseActions; 
            //this.BeforeClose += DebtorAccount_BeforeClose;
        }

      
    }

    


    //public partial class CWDirectDebitFiles : ChildWindow
    //{
    //    public string Account { get; set; }

    //    public CWDirectDebitFiles(CrudAPI api)
    //    {
    //        this.DataContext = this;
    //        InitializeComponent();
    //        this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice"));
    //        this.Loaded += CW_Loaded;
    //        dgCWDirectDebitFiles.BusyIndicator = busyIndicator;
    //        dgCWDirectDebitFiles.api = api;
    //        localMenu.dataGrid = dgCWDirectDebitFiles;
    //       // this.Account = account;
    //        //if (!this.dgCWDirectDebitFiles.ReuseCache(typeof(Uniconta.DataModel.DCInvoice)))
    //        //    BindGrid();
    //    }

    //    void CW_Loaded(object sender, RoutedEventArgs e)
    //    {
    //        Dispatcher.BeginInvoke(new Action(() => { CreateButton.Focus(); }));
    //    }

    //    private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
    //    {
    //        if (e.Key == Key.Escape)
    //        {
    //            this.DialogResult = false;
    //        }
    //        else if (e.Key == Key.Enter)
    //        {
    //            if (CreateButton.IsFocused)
    //                CreateButton_Click(null, null);
    //            else if (CancelButton.IsFocused)
    //                this.DialogResult = false;
    //        }
    //    }

    //    private void CreateButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        if (dgCWDirectDebitFiles.SelectedItem == null)
    //        {
    //            MessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"));
    //            return;
    //        }
    //        else
    //            this.DialogResult = true;
    //    }

    //    private void CancelButton_Click(object sender, RoutedEventArgs e)
    //    {
    //        this.DialogResult = false;
    //    }

    //    private Task Filter(IEnumerable<PropValuePair> propValuePair)
    //    {
    //        if(!string.IsNullOrWhiteSpace(Account))
    //            propValuePair = new List<PropValuePair>(){ PropValuePair.GenereteWhereElements("Account", Account, CompareOperator.Equal) };

    //        return dgCWDirectDebitFiles.Filter(propValuePair);
    //    }

    //    private void BindGrid()
    //    {
    //        var t = Filter(null);
    //    }
}
