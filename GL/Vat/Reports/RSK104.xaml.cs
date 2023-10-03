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
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;
using Uniconta.ClientTools.Page;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.Globalization;
using DevExpress.Xpf.Editors;
using Bilagscan;
using System.IO;
using System.Windows.Forms;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for RSK104.xaml
    /// </summary>
    /// 

    class RSK104Reitur
    {
        public string reitur { get; set; }
        public double amount { get; set; }

    }

    public class RSK104Grid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountClient); } }
    }

    public partial class RSK104 : GridBasePage
    {
        List<RSK104Reitur> RSK104Reitir = new List<RSK104Reitur>();
        int filterYear;

        public RSK104(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            this.DataContext = this;
            SetRibbonControl(localMenu, dgGLTable2);
            dgGLTable2.api = (CrudAPI)api;
            //filterYear = BasePage.GetSystemDefaultDate().Year;
            var accountingYears = dgGLTable2.api.QuerySync<CompanyFinanceYearClient>();
            var accountYear = accountingYears.Where(y => y.Current == true).FirstOrDefault();
            filterYear = accountYear.FromDate.Year;

            txtYearValue.EditValue = DateTime.Parse($"{filterYear}-01-01", CultureInfo.InvariantCulture);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            txtYearValue.IsReadOnly = true;
            dgGLTable2.BusyIndicator = busyIndicator;
        }
        private void btnTXT_OnClick(object sender, RoutedEventArgs e)
        {
            if (CombineTransactions())
            {
                CreateRSK104File();
            }
        }

        private void CreateRSK104File()
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            try
            {
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.FileName = "RSK104_" + api.CompanyEntity.Name + "_" + filterYear.ToString();
                saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 2;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if ((myStream = saveFileDialog1.OpenFile()) != null)
                    {
                        List<RSK104Reitur> RSK104Radadir = RSK104Reitir.OrderBy(o => o.reitur).ToList();
                        using (var writer = new StreamWriter(myStream))
                        {
                            writer.Write("[RSK 1.04 - " + filterYear.ToString() + "]" + "\r\n");
                            writer.Write("0020 = " + api.CompanyEntity._Id + "\r\n");
                            writer.Write("0220 = " + api.CompanyEntity._VatNumber + "\r\n");
                            foreach (var grouping in RSK104Radadir.GroupBy(r => r.reitur))
                            {
                                writer.Write(grouping.Key + "=" + String.Format("{0:0}", Math.Abs(grouping.Sum(o => o.amount))) + "\r\n");
                            }
                            writer.Flush();
                        }
                        myStream.Close();
                    }
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e.Message, "Error");
            }
        }

        public GLTransClient[] gltrans { get; set; }

        private bool DigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        private bool CombineTransactions()
        {
            var res = true;
            SetBusy();
            try
            {
                gltrans = api.QuerySync<GLTransClient>();
                var items = dgGLTable2.VisibleItems.Cast<GLAccountClient>();

                foreach (var item in items)
                {
                    if (!String.IsNullOrEmpty(item.ExternalNo))
                    {
                        if (item.ExternalNo.Length == 4 && DigitsOnly(item.ExternalNo))
                        {
                            RSK104Reitur rsk104 = new RSK104Reitur()
                            {
                                reitur = item.ExternalNo,
                                amount = SummerizeTransactions(item)
                            };
                            RSK104Reitir.Add(rsk104);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e.Message, "Error");
                res = false;
            }
            ClearBusy();
            return res;
        }

        private double SummerizeTransactions(GLAccountClient item)
        {
            double total = 0;
            var matchingTransactions = gltrans.Where(o => o.Account != null && o.Account.StartsWith(item.Account));
            foreach (var m in matchingTransactions)
            {
                if ((filterYear - 1).ToString() == m.Date.ToString("yyyy"))
                {
                    total += m.Amount;
                }
            }
            return total;
        }

        private void TxtYearValue_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var filterDate = (DateTime)e.NewValue;
            filterYear = filterDate.Year;
            this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
            this.baseRibbon?.FilterGrid.Refresh();
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "TXT":
                    if (CombineTransactions())
                    {
                        CreateRSK104File();
                    }
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override Filter[] DefaultFilters()
        {
            Filter acctypeFilter = new Filter() { name = "AccountType", value = $"Aðrar tekjur;Afskriftir;Banki;Birgðir;Eigið fé;Fastafjármunir;Kostnaður;Lánardrottnar;Rekstur;Skuldir;Tekjur;Útgjöld;Veltufjármunir;Viðskiptavinir;Vörunotkun" };
            return new Filter[] { acctypeFilter };
        }
    }
}
