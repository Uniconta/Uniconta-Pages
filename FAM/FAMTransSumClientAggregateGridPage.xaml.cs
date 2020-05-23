using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
using Uniconta.Common;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FAMTransSumClientAggregateGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(FAMTransSumClientAggregate); } }
        public override bool Readonly { get { return true; } }
    }
        
    public partial class FAMTransSumClientAggregateGridPage : GridBasePage
    {
        DateTime fromDate, toDate;
        public override string NameOfControl { get { return TabControls.FAMTransSumClientAggregateGridPage; } }
        public FAMTransSumClientAggregateGridPage(BaseAPI Api, FAMTransSumClient[] fAMTransSumClients, DateTime fromDate, DateTime toDate) :
            base(Api, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgFAMTransSumClientAggregateGrid);
            dgFAMTransSumClientAggregateGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            this.fromDate = fromDate;
            this.toDate = toDate;

            var lst = new List<FAMTransSumClientAggregate>(fAMTransSumClients.Length / 4);
            FAMTransSumClientAggregate cur = null;
            string lastAsset = "\r";
            foreach (var rec in fAMTransSumClients)
            {
                if (rec._Asset != lastAsset)
                {
                    lastAsset = rec._Asset;
                    cur = new FAMTransSumClientAggregate() { _CompanyId = api.CompanyId, _Asset = lastAsset };
                    lst.Add(cur);
                }

                switch ((byte)rec._AssetPostType)
                {
                    case (byte)FAMTransCodes.Depreciation:
                        cur._Depreciation = rec._Amount;
                        cur._DepreciationPrimo += rec._Primo;
                        break;
                    case (byte)FAMTransCodes.WriteDown:
                        cur._WriteDown = rec._Amount;
                        cur._DepreciationPrimo += rec._Primo;
                        break;
                    case (byte)FAMTransCodes.WriteOff:
                        cur._WriteOff = rec._Amount;
                        cur._DepreciationPrimo += rec._Primo;
                        break;
                    case (byte)FAMTransCodes.WriteUp:
                        cur._WriteUp = rec._Amount;
                        cur._Primo += rec._Primo;
                        break;
                    case (byte)FAMTransCodes.Acquisition:
                        cur._Receipt = rec._Amount;
                        cur._Primo += rec._Primo;
                        break;
                    case (byte)FAMTransCodes.Sale:
                        cur._Issue = rec._Amount;
                        cur._Primo += rec._Primo;
                        break;
                }
            }

            dgFAMTransSumClientAggregateGrid.ItemsSource = lst;
            dgFAMTransSumClientAggregateGrid.ShowTotalSummary();
            dgFAMTransSumClientAggregateGrid.Visibility = Visibility.Visible;
        }

        public override Task InitQuery() { return null; }

        private void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }

        public override object GetPrintParameter()
        {
            var fromDate = this.fromDate == DateTime.MinValue ? string.Empty : this.fromDate.ToShortDateString();
            var toDate = this.toDate == DateTime.MinValue ? string.Empty : this.toDate.ToShortDateString();

            var printData = new PageReportHeader()
            {
                CurDateTime = DateTime.Now.ToString("g"),
                CompanyName = api.CompanyEntity._Name,
                ReportName = Uniconta.ClientTools.Localization.lookup("Statement"),
                Header = string.Format("({0} - {1})", fromDate, toDate),
            };
            return printData;
        }

    }

    public class FamSumText
    {
        public static string Receipt { get { return Uniconta.ClientTools.Localization.lookup("ReceiptStock"); } }
        public static string Issue { get { return Uniconta.ClientTools.Localization.lookup("LeaveStock"); } }
        public static string DepreciationBase { get { return Uniconta.ClientTools.Localization.lookup("DepreciationBase"); } }
        public static string DepreciationPrimo { get { return Uniconta.ClientTools.Localization.lookup("DepreciationPrimo"); } }
        public static string DepreciationUltimo { get { return Uniconta.ClientTools.Localization.lookup("DepreciationUltimo"); } }
    }

    public class FAMTransSumClientAggregate : UnicontaBaseEntity
    {
        public int _CompanyId;
        public string _Asset;

        public long _Primo, _Receipt, _Issue, _DepreciationPrimo, _Depreciation, _WriteUp, _WriteOff, _WriteDown;

        [Uniconta.Common.ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.FAM))]
        [Display(Name = "Asset", ResourceType = typeof(FamText))]
        public string Asset { get { return _Asset; } }

        [Display(Name = "AssetName", ResourceType = typeof(FamText))]
        public string AssetName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.FAM), _Asset); } }

        [Display(Name = "Primo", ResourceType = typeof(GLDailyJournalText))]
        public double Primo { get { return _Primo / 100d; } }

        [Display(Name = "Receipt", ResourceType = typeof(FamSumText))]
        public double Receipt { get { return _Receipt / 100d; } }

        [Display(Name = "Issue", ResourceType = typeof(FamSumText))]
        public double Issue { get { return _Issue / 100d; } }

        [Display(Name = "DepreciationBase", ResourceType = typeof(FamSumText))]
        public double DepreciationBase { get { return (_Primo + _Receipt + _Issue + _WriteUp) / 100d; } }

        [Display(Name = "DepreciationPrimo", ResourceType = typeof(FamSumText))]
        public double DepreciationPrimo { get { return _DepreciationPrimo / 100d; } }

        [Display(Name = "Depreciation", ResourceType = typeof(FamText))]
        public double Depreciation { get { return _Depreciation / 100d; } }

        [Display(Name = "WriteUp", ResourceType = typeof(FamText))]
        public double WriteUp { get { return _WriteUp / 100d; } }

        [Display(Name = "WriteDown", ResourceType = typeof(FamText))]
        public double WriteDown { get { return _WriteDown / 100d; } }

        [Display(Name = "WriteOff", ResourceType = typeof(FamText))]
        public double WriteOff { get { return _WriteOff / 100d; } }

        [Display(Name = "DepreciationUltimo", ResourceType = typeof(FamSumText))]
        public double DepreciationUltimo { get { return (_DepreciationPrimo + _Depreciation + _WriteOff + _WriteDown) / 100d; } }

        [Display(Name = "BookedValue", ResourceType = typeof(FamText))]
        public double BookedValue { get { return (_Primo + _Receipt + _Issue + _DepreciationPrimo + _Depreciation + _WriteUp + _WriteOff + _WriteDown) / 100d; } }

        [ReportingAttribute]
        public FamClient AssetRef
        {
            get
            {
                return ClientHelper.GetRefClient<FamClient>(_CompanyId, typeof(Uniconta.DataModel.FAM), _Asset);
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(_CompanyId); } }

        public int CompanyId { get { return _CompanyId; } set { _CompanyId = value; } }
        public Type BaseEntityType() { return GetType(); }
        public void loadFields(CustomReader r, int SavedWithVersion) { }
        public void saveFields(CustomWriter w, int SaveVersion) { }
        public int Version(int ClientAPIVersion){ return 1; }
        public int ClassId() { return 1234; }
    }
}
