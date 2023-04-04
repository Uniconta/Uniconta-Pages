using DevExpress.DashboardWpf.Internal;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWSetFeeAmount : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Value", ResourceType = typeof(InputFieldDataText))]
        public double value { get { return _value; } set { _value = value; } }
        static double _value;

        public string CWName { get; set; }
        public string Prompt { get; set; }

        [InputFieldData]
        [Display(Name = "PerTransaction", ResourceType = typeof(InputFieldDataText))]
        public bool PerTransaction { get { return !_PerAccount; } set { _PerAccount = !value; } }
        static bool _PerAccount;
        public string SelectedType { get { return _SelectedType; } set { _SelectedType = value; } }
        static string _SelectedType;

        [InputFieldData]
        [Display(Name = "CollectionLetter", ResourceType = typeof(InputFieldDataText))]
        [AppEnumAttribute(EnumName = "DebtorEmailType", ValidIndexes = new[] { 3, 4, 5, 6, 13 })]
        public string CollectionType { get { return _CollectionType; } set { _CollectionType = value; } }
        static string _CollectionType;

        [InputFieldData]
        [AppEnumAttribute(EnumName = "Currencies", Enumtype = typeof(Currencies))]
        [Display(Name = "Currency", ResourceType = typeof(InputFieldDataText))]
        public string FeeCurrency { get { return _FeeCurrency; } set { _FeeCurrency = value; } }
        static string _FeeCurrency;

        [InputFieldData]
        [Display(Name = "Charge", ResourceType = typeof(InputFieldDataText))]
        public double Charge { get { return _Charge; } set { _Charge = value; } }
        static double _Charge;

        [InputFieldData]
        [AppEnumAttribute(EnumName = "Currencies", Enumtype = typeof(Currencies))]
        [Display(Name = "Currency", ResourceType = typeof(InputFieldDataText))]
        public string ChargeCurrency { get { return _ChargeCurrency; } set { _ChargeCurrency = value; } }
        static string _ChargeCurrency;

        public DateTime PrDate { get { return _PrDate; } set { _PrDate = value; } }
        static public DateTime _PrDate;

        [InputFieldData]
        [Display(Name = "NumberOfDays", ResourceType = typeof(InputFieldDataText))]
        public string NoOfDays { get { return _NoOfDays; } set { _NoOfDays = value; } }
        static string _NoOfDays;

        [InputFieldData]
        [Display(Name = "FeeOnReminder", ResourceType = typeof(InputFieldDataText))]
        public bool FeeOnReminder { get { return _FeeOnReminder; } set { _FeeOnReminder = value; } }
        static bool _FeeOnReminder;

        [InputFieldData]
        [AppEnumAttribute(EnumName = "DebtorEmailType", ValidIndexes = new[] { 3, 4, 5, 6, 13 })]
        [Display(Name = "FirstCollection", ResourceType = typeof(InputFieldDataText))]
        public string FirstCollectionType { get { return _firstCollectionType; } set { _firstCollectionType = value; } }
        static string _firstCollectionType;

        public int CollectionLetterTypes, FirstCollectionIndex;
        bool _addInterest;
        protected override int DialogId => DialogTableId;
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton => true;

        public CWSetFeeAmount(bool AddInterest)
        {
            InitializeComponent();
            _addInterest = AddInterest;
            this.DataContext = this;
            lblPerDate.Text = String.Format(Uniconta.ClientTools.Localization.lookup("PerOBJ"), Uniconta.ClientTools.Localization.lookup("Date"));
            if (AddInterest)
            {
                CWName = Uniconta.ClientTools.Localization.lookup("Interest");
                Prompt = Uniconta.ClientTools.Localization.lookup("Interest") + " % (" + Uniconta.ClientTools.Localization.lookup("Month") + ")";
                cmbNoOfDays.ItemsSource = new string[] { "30", Uniconta.ClientTools.Localization.lookup("PerDay") };
                if (_NoOfDays == null)
                    _NoOfDays = "30";
                if (cmbNoOfDays.SelectedIndex < 0)
                    cmbNoOfDays.SelectedIndex = 0;
                rowFeeTransaction.Height = new GridLength(0);
                rowCollectionLetter.Height = new GridLength(0);
                colFeeCurrency.Width = new GridLength(0);
                colMarginFeeCurrency.Width = new GridLength(0);
                rowFeeCharge.Height = new GridLength(0);
                rowFeeOnReminder.Height = new GridLength(0);
                rowFirstCollection.Height = new GridLength(0);
            }
            else
            {
                Prompt = CWName = Uniconta.ClientTools.Localization.lookup("AddCollection");
                lblFee.Text = Uniconta.ClientTools.Localization.lookup("Per");
                cmbtypeValue.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Transaction"), Uniconta.ClientTools.Localization.lookup("Account") };
                cmbCurrency.ItemsSource = AppEnums.Currencies.GetLabels();
                cmbFirstCollections.ItemsSource = cmbCollections.ItemsSource = Utility.GetDebtorCollectionLetters().ToList();
                rowNoOfDays.Height = new GridLength(0);
            }
            if (_PrDate == DateTime.MinValue)
                _PrDate = Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();

            dePrDate.DateTime = _PrDate;

            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrEmpty(_firstCollectionType))
                    cmbFirstCollections.SelectedIndex = 0;
                cmbtypeValue.SelectedIndex = PerTransaction ? 0 : 1;

                if (string.IsNullOrWhiteSpace(txtValue.Text))
                    txtValue.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_addInterest)
            {
                if (string.IsNullOrEmpty(CollectionType))
                {
                    Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup("CollectionLetter")), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                var coltype = CollectionType.Split(';');
                CollectionLetterTypes = 0;
                for (int i = 0; i < coltype.Length; i++)
                {
                    switch (AppEnums.DebtorEmailType.IndexOf(coltype[i]))
                    {
                        case 13: CollectionLetterTypes |= 0x01; break;
                        case 3: CollectionLetterTypes |= 0x02; break;
                        case 4: CollectionLetterTypes |= 0x04; break;
                        case 5: CollectionLetterTypes |= 0x08; break;
                        case 6: CollectionLetterTypes |= 0x10; break;
                    }
                }

                FirstCollectionIndex = cmbFirstCollections.SelectedIndex;
            }
            _PrDate = dePrDate.DateTime;
            PerTransaction = cmbtypeValue.SelectedIndex == 0;
            SetDialogResult(true);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
               if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
    }
}
