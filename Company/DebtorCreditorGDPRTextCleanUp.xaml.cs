using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorCreditorGDPRTextCleanUpGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorCreditorLocal); } }
        public override bool CanInsert { get { return false; } }
        public override bool Readonly { get { return true; } }  
    }
    public partial class DebtorCreditorGDPRTextCleanUp : GridBasePage
    {
        public DebtorCreditorGDPRTextCleanUp(BaseAPI API, int[] rowIds) : base(API, string.Empty)
        {
            InitializeComponent();
            dgDebCredGDPRClnUp.BusyIndicator = busyIndicator;
            this.BeforeClose += DebtorCreditorGDPRTextCleanUp_BeforeClose;
            this.PreviewKeyDown += DebtorCreditorGDPRTextCleanUp_PreviewKeyDown;
            LoadDataGrid(rowIds);
        }

        private void DebtorCreditorGDPRTextCleanUp_BeforeClose()
        {
            this.PreviewKeyDown -= DebtorCreditorGDPRTextCleanUp_PreviewKeyDown;
        }

        void LoadDataGrid(int[] rowIds)
        {
            busyIndicator.IsBusy = true;
            var DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            var CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            var lst = new List<DebtorCreditorLocal>(rowIds.Length);
            foreach(int rowId in rowIds)
            {
                var dc = (DCAccount)DebtorCache.Get(rowId) ?? (DCAccount)CreditorCache.Get(rowId);
                if (dc != null)
                    lst.Add(new DebtorCreditorLocal { _DCType = dc.__DCType(), Account = dc._Account, Name = dc._Name } );
            }
            dgDebCredGDPRClnUp.ItemsSource = lst;
            dgDebCredGDPRClnUp.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }
        private void DebtorCreditorGDPRTextCleanUp_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F6 )
            {
                var currentRow = dgDebCredGDPRClnUp.SelectedItem as DebtorCreditorLocal;
                if (currentRow != null)
                {
                    var lookupTable = new LookUpTable();
                    lookupTable.api = this.api;
                    lookupTable.KeyStr = currentRow.Account;
                    if (currentRow._DCType == 1)
                    {
                        lookupTable.TableType = typeof(Uniconta.DataModel.Debtor);
                        this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.DebtorAccount);
                    }
                    if (currentRow._DCType == 2)
                    {
                        lookupTable.TableType = typeof(Uniconta.DataModel.Creditor);
                        this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.CreditorAccount);
                    }
                }
            }
        }

        public override Task InitQuery()
        {
            return null;
        }
    }

    public class DebtorCreditorLocal: INotifyPropertyChanged
    {
        public int _DCType;
        [Display(Name = "AccountType", ResourceType = typeof(GLDailyJournalText))]
        public string DCType { get { return AppEnums.DebtorCreditorProspect.ToString(_DCType); } }

        [Display(Name = "DAccount", ResourceType = typeof(GLTableText)), Key]
        public string Account { get; set; }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
