using DevExpress.Utils.About;
using DevExpress.Xpf.Grid;
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
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ItemVariantAddPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ItemVariantLocal); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ItemVariantAddPage : GridBasePage
    {
        DCOrder master;
        InvJournal journalMaster;
        InvItem invItem;
        InvStandardVariant stdVariant;
        DCOrderLineClient line;
        InvJournalLineClient journalLine;
        SQLCache variants1, variants2, variants3, variants4, variants5;
        public ItemVariantAddPage(DCOrderLineClient line, DCOrder master) : base(null)
        {
            this.master = master;
            this.line = line;
            this.invItem = line.InvItem;
            InitPage();
        }

        bool IsInvJrnLine = false;
        public ItemVariantAddPage(InvJournalLineClient line, InvJournal master) : base(null)
        {
            this.journalMaster = master;
            this.journalLine = line;
            this.invItem = journalLine.InvItem;
            IsInvJrnLine = true;
            InitPage();
        }

        void InitPage()
        {
            DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgItemVariant;
            dgItemVariant.api = api;
            SetRibbonControl(localMenu, dgItemVariant);
            dgItemVariant.BusyIndicator = busyIndicator;
            var standardVariants = api.GetCache(typeof(Uniconta.DataModel.InvStandardVariant));
            stdVariant = (InvStandardVariant)standardVariants?.Get(invItem._StandardVariant);
            if (stdVariant != null)
                VariantSetup(colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, stdVariant);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var master = stdVariant;
            if (master == null)
                return;
            var Combinations = master.Combinations ?? await master.LoadCombinations(api);
            if (Combinations == null)
                return;
            int desc = invItem._Decimals;
            var lst = new List<ItemVariantLocal>();
            if (master._AllowAllCombinations)
            {
                var t1 = api.LoadCache(typeof(Uniconta.DataModel.InvVariant1));
                var t2 = api.LoadCache(typeof(Uniconta.DataModel.InvVariant2));
                var t3 = api.LoadCache(typeof(Uniconta.DataModel.InvVariant3));
                var t4 = api.LoadCache(typeof(Uniconta.DataModel.InvVariant4));
                var t5 = api.LoadCache(typeof(Uniconta.DataModel.InvVariant5));
                var tasks = Task.WhenAll(t1, t2, t3, t4, t5);
                variants1 = t1.Result;
                variants2 = t2.Result;
                variants3 = t3.Result;
                variants4 = t4.Result;
                variants5 = t5.Result;
                var v1Source = variants1.GetKeyStrRecords as IEnumerable<InvVariant1>;
                var v2Source = variants2.GetKeyStrRecords as IEnumerable<InvVariant2>;
                var v3Source = variants3.GetKeyStrRecords as IEnumerable<InvVariant3>;
                var v4Source = variants4.GetKeyStrRecords as IEnumerable<InvVariant4>;
                var v5Source = variants5.GetKeyStrRecords as IEnumerable<InvVariant5>;

                var comp = api.CompanyEntity;
                int n = stdVariant._Nvariants != 0 ? stdVariant._Nvariants : comp.NumberOfVariants;

                if (n >= 1)
                {
                    foreach (var v1 in v1Source)
                    {
                        var variant1 = v1.KeyStr;
                        if (n == 1)
                        {
                            var rec1 = new ItemVariantLocal() { _Variant1 = variant1 };
                            lst.Add(rec1);
                        }
                        if (n >= 2)
                        {
                            foreach (var v2 in v2Source)
                            {
                                var variant2 = v2.KeyStr;
                                if (n == 2)
                                {
                                    var rec2 = new ItemVariantLocal() { _Variant1 = variant1, _Variant2 = variant2 };
                                    lst.Add(rec2);
                                }
                                if (n >= 3)
                                {
                                    foreach (var v3 in v3Source)
                                    {
                                        var variant3 = v3.KeyStr;
                                        if (n == 3)
                                        {
                                            var rec3 = new ItemVariantLocal() { _Variant1 = variant1, _Variant2 = variant2, _Variant3 = variant3 };
                                            lst.Add(rec3);
                                        }
                                        if (n >= 4)
                                        {
                                            foreach (var v4 in v4Source)
                                            {
                                                var variant4 = v4.KeyStr;
                                                if (n == 4)
                                                {
                                                    var rec4 = new ItemVariantLocal() { _Variant1 = variant1, _Variant2 = variant2, _Variant3 = variant3, _Variant4 = variant4 };
                                                    lst.Add(rec4);
                                                }
                                                if (n >= 5)
                                                {
                                                    foreach (var v5 in v5Source)
                                                    {
                                                        var variant5 = v5.KeyStr;
                                                        var rec5 = new ItemVariantLocal() { _Variant1 = variant1, _Variant2 = variant2, _Variant3 = variant3, _Variant4 = variant4, _Variant5 = variant5 };
                                                        lst.Add(rec5);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var rec in Combinations)
                {
                    var r2 = new ItemVariantLocal(desc);
                    StreamingManager.Copy(rec, r2);
                    lst.Add(r2);
                }
                lst.Sort(new ItemVariantLocalSort());
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                dgItemVariant.ItemsSource = lst;
                dgItemVariant.Visibility = Visibility.Visible;
            }));
            busyIndicator.IsBusy = false;

        }

        public override bool IsDataChaged { get { return false; } }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Generate":
                    Generate();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Generate()
        {
            Type recordType = !IsInvJrnLine ? line.GetType() : journalLine.GetType();
            var itemStr = invItem.KeyStr;
            var InvItems = new List<UnicontaBaseEntity>();
            bool first = !IsInvJrnLine ? (line._Variant1 == null && line._Variant2 == null) : (journalLine._Variant1 == null && journalLine._Variant2 == null);  // the line we pasted has no variant, so we insert the first variant in that.
            foreach (var rec in (IEnumerable<ItemVariantLocal>)dgItemVariant.ItemsSource)
            {
                if (rec._Quantity != 0d && !IsInvJrnLine)
                {
                    DCOrderLineClient lin;
                    if (!first)
                        lin = Activator.CreateInstance(recordType) as DCOrderLineClient;
                    else
                        lin = this.line; // we want to edit the first.
                    lin.SetMaster((UnicontaBaseEntity)master);
                    lin._Item = itemStr;
                    lin.Qty = rec._Quantity;
                    lin.Variant1 = rec._Variant1;
                    lin.Variant2 = rec._Variant2;
                    lin.Variant3 = rec._Variant3;
                    lin.Variant4 = rec._Variant4;
                    lin.Variant5 = rec._Variant5;
                    if (!first)
                        InvItems.Add((UnicontaBaseEntity)lin);
                    else
                        first = false;
                }
                else if(rec._Quantity != 0d && IsInvJrnLine)
                {
                    InvJournalLineClient lin;
                    if (!first)
                        lin = Activator.CreateInstance(recordType) as InvJournalLineClient;
                    else
                        lin = this.journalLine; // we want to edit the first.
                    lin.SetMaster((UnicontaBaseEntity)journalMaster);
                    lin._Item = itemStr;
                    lin.Qty = rec._Quantity;
                    lin.Variant1 = rec._Variant1;
                    lin.Variant2 = rec._Variant2;
                    lin.Variant3 = rec._Variant3;
                    lin.Variant4 = rec._Variant4;
                    lin.Variant5 = rec._Variant5;
                    if (!first)
                        InvItems.Add((UnicontaBaseEntity)lin);
                    else
                        first = false;
                }
            }
            var param = !IsInvJrnLine ? new object[2] { InvItems, master._OrderNumber } : new object[2] { InvItems, journalMaster._Journal };
            globalEvents.OnRefresh(TabControls.ItemVariantAddPage, param);
            CloseDockItem();
        }

        public void VariantSetup(GridColumn Variant1, GridColumn Variant2, GridColumn Variant3, GridColumn Variant4, GridColumn Variant5, InvStandardVariant stdVariant)
        {
            var comp = api.CompanyEntity;
            int n = stdVariant._Nvariants != 0 ? stdVariant._Nvariants : comp.NumberOfVariants;

            if (n >= 1)
                Variant1.Header = stdVariant._Variant1Name ?? comp._Variant1;
            else
                Variant1.ShowInColumnChooser = Variant1.Visible = false;
            if (n >= 2)
                Variant2.Header = stdVariant._Variant2Name ?? comp._Variant2;
            else
                Variant2.ShowInColumnChooser = Variant2.Visible = false;
            if (n >= 3)
                Variant3.Header = stdVariant._Variant3Name ?? comp._Variant3;
            else
                Variant3.ShowInColumnChooser = Variant3.Visible = false;
            if (n >= 4)
                Variant4.Header = stdVariant._Variant4Name ?? comp._Variant4;
            else
                Variant4.ShowInColumnChooser = Variant4.Visible = false;
            if (n >= 5)
                Variant5.Header = stdVariant._Variant5Name ?? comp._Variant5;
            else
                Variant5.ShowInColumnChooser = Variant5.Visible = false;
        }
    }

    public class ItemVariantLocalSort : IComparer<ItemVariantLocal>
    {
        public int Compare(ItemVariantLocal x, ItemVariantLocal y)
        {
            if (x._LineNumber1 > y._LineNumber1)
                return 1;
            if (x._LineNumber1 < y._LineNumber1)
                return -1;
            if (x._LineNumber2 > y._LineNumber2)
                return 1;
            if (x._LineNumber2 < y._LineNumber2)
                return -1;
            if (x._LineNumber3 > y._LineNumber3)
                return 1;
            if (x._LineNumber3 < y._LineNumber3)
                return -1;
            if (x._LineNumber4 > y._LineNumber4)
                return 1;
            if (x._LineNumber4 < y._LineNumber4)
                return -1;
            if (x._LineNumber5 > y._LineNumber5)
                return 1;
            if (x._LineNumber5 < y._LineNumber5)
                return -1;
            return 0;
        }
    }

    public class ItemVariantLocal : InvStandardVariantCombiClient
    {
        public ItemVariantLocal() { }
        int desc;
        public ItemVariantLocal(int desc) { this.desc = desc; }

        public double _Quantity;
        [Display(Name = "Qty", ResourceType = typeof(DCOrderText))]
        public double Quantity { get { return _Quantity; } set { _Quantity = value; NotifyPropertyChanged("Quantity"); } }

        public int Decimals { get { return desc; } }
    }
}
