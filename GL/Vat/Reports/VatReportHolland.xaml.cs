using UnicontaClient.Models;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatReportHollandGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(VatSumOperationReport); }
        }

        public override bool Readonly
        {
            get { return true; }
        }
    }

    public partial class VatReportHolland : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VatReportHolland; }
        }

        #region Member variables
        protected List<VatSumOperationReport> vatSumOperationLst;
        #endregion

        #region Properties
        private List<VatSumOperationReport> VatSumOperationLst
        {
            get
            {
                return vatSumOperationLst;
            }

            set
            {
                vatSumOperationLst = value;
            }
        }

        private DateTime fromDate { get; set; }
        private DateTime toDate { get; set; }
        #endregion
        public VatReportHolland(List<VatSumOperationReport> vatSumOperationLst, DateTime fromDate, DateTime toDate) : base(null)
        {
            this.VatSumOperationLst = vatSumOperationLst;
            this.fromDate = fromDate;
            this.toDate = toDate;

            InitializeComponent();
            this.DataContext = this;

            dgVatReportHolland.api = api;
            dgVatReportHolland.BusyIndicator = busyIndicator;

            bool SumIsneg = false;
            double sum = 0d, sumBase = 0d, sum19 = 0d;
            foreach (var rec in VatSumOperationLst)
            {
                string s, opr;

                var lin = rec._Line;
                if (lin == 17 || lin == 19 || lin == 23)
                    rec._Amount = rec._AmountBase = 0d;

                if (lin >= 16)
                {
                    var d = rec._Amount;
                    if (d != 0)
                    {
                        if (lin <= 18)
                        {
                            if (d >= 0)
                                d += 0.499d;
                            else
                                d -= 0.499d;
                        }
                        rec._Amount = Math.Round(d);
                    }
                    sum += rec._Amount;
                    sumBase += rec._AmountBase;
                }
                else
                {
                    var d = rec._Amount;
                    if (d != 0)
                    {
                        if (d > 0)
                            d -= 0.499d;
                        else
                            d += 0.499d;
                        rec._Amount = Math.Round(d);
                    }
                    sum -= rec._Amount;
                    sumBase -= rec._AmountBase;
                }

                switch (lin)
                {
                    case 1: s = "Prestaties binnenland"; opr = "1"; opr = "1"; break;
                    case 2: s = "Leveringen/diensten belast met hoog tarief"; opr = "1a"; break;
                    case 3: s = "Leveringen/diensten belast met laag tarief"; opr = "1b"; break;
                    case 4: s = "Leveringen/diensten belast met overige tarieven behalve 0%"; opr = "1c"; break;
                    case 5: s = "Prive-gebruik"; opr = "1d"; break;
                    case 6: s = "Leveringen/diensten belast met 0% of niet bij u belast"; opr = "1e"; break;
                    case 7: s = "Verleggingsregelingen binnenland"; opr = "2"; break;
                    case 8: s = "Leveringen/diensten waarbij de heffing van omzetbelasting naar u is verlegd."; opr = "2a"; break;
                    case 9: s = "Prestaties naar/in het buitenland"; opr = "3"; break;
                    case 10: s = "Leveringen naar landen buiten de EU (uitvoer)"; opr = "3a"; break;
                    case 11: s = "Leveringen naar/diensten in landen binnen de EU"; opr = "3b"; break;
                    case 12: s = "Installatie/afstandsverkopen binnen de EU"; opr = "3c"; break;
                    case 13: s = "Prestaties uit het buitenland aan u verricht"; opr = "4"; break;
                    case 14: s = "Leveringen/diensten uit landen buiten de EU"; opr = "4a"; break;
                    case 15: s = "Leveringen/diensten uit landen binnen de EU"; opr = "4b"; break;
                    case 16: s = "Voorbelasting, kleinondernemersregeling, schatting en eindtotaal"; opr = "5"; break;
                    case 17: s = "Verschuldigde omzetbelasting";
                        rec._Amount = sum19 = -sum;
                        opr = "5a"; break;
                    case 18: s = "Voorbelasting"; opr = "5b";
                        sum19 -= rec._Amount;
                        break; 
                    case 19: s = "Subtotal: bereken 5a min 5b.";
                        sum = sum19;
                        rec._Amount = sum;
                        SumIsneg = (sum < 0d);
                        opr = "5c"; break;
                    case 20: s = "Vermindering volges de kleindondernemersregeling"; opr = "5d"; break;
                    case 21: s = "Schatting vorige aangifte(n)"; opr = "5e"; break;
                    case 22: s = "Schatting deze aangifte"; opr = "5f"; break;
                    case 23:
                        if (SumIsneg)
                        {
                            s = "Totaal te vorderen";
                            sum = -sum;
                        }
                        else
                            s = "Totaal te betalen";
                        rec._Amount = sum;
                        opr = "5g"; break;
                    default: s = null; opr = null; break;
                }
                rec._Text = s;
                rec._UnicontaOperation = opr;
            }

            dgVatReportHolland.ItemsSource = VatSumOperationLst;
            dgVatReportHolland.Visibility = Visibility.Visible;
        }
    }
}
 