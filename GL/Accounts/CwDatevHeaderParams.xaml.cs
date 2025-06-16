using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwDatevHeaderParams : ChildWindow
    {
        public string Consultant { get; set; }
        public string Client { get; set; }
        private string _Path;
        public string Path { get { return this._Path; } set { this._Path = value; } }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        public string DefaultAccount { get; set; }
        public int Kost1 { get; set; }
        public int Kost2 { get; set; }
        public string LanguageId { get; set; }
        public DateTime FiscalYearBegin { get; set; }
        public Boolean Active { get; set; }

        public CwDatevHeaderParams(string consultant, string client, string path, string defaultAcc, string language, DateTime fiscalYearBegin, Boolean active, int kost1, int kost2, CrudAPI api)
        {

            this.DataContext = this;
            this.Consultant = consultant;
            this.Client = client;
            this._Path = path;
            this.DefaultAccount = defaultAcc;
            this.LanguageId = language;
            this.FiscalYearBegin = fiscalYearBegin;
            this.Active = active;
            this.InitializeComponent();
            this.leGlAccount.api = api;

            var company = api.CompanyEntity;
            this.Kost1 = kost1 > company.NumberOfDimensions ? -1 : kost1;
            this.Kost2 = kost2 > company.NumberOfDimensions ? -1 : kost2;
            var dimensions = new List<string>();
            dimensions.Add("(Leer)"); // value -1, index 0: keep empty on export
            dimensions.Add($"Standard ({company._Dim1 ?? "leer"})"); // index 1, value 0
            dimensions.Add(company._Dim1);
            dimensions.Add(company._Dim2);
            dimensions.Add(company._Dim3);
            dimensions.Add(company._Dim4);
            dimensions.Add(company._Dim5);
            dimensions.RemoveRange(company.NumberOfDimensions + 2, 5 - company.NumberOfDimensions);
            this.cmbKost1.ItemsSource = dimensions;
            var dimensions2 = new List<string>(dimensions);
            dimensions2[1] = $"Standard ({company._Dim2 ?? "leer"})";
            this.cmbKost2.ItemsSource = dimensions2;
            this.cmbKost1.SelectedIndex = this.Kost1 + 1; // default Dim1
            this.cmbKost2.SelectedIndex = this.Kost2 + 1; // default Dim2

            this.Title = Uniconta.ClientTools.Localization.lookup("DatevHeader");
            this.Loaded += this.CW_Loaded;
            this.txtDatev.Text = "Ich bestätige, dass bei der DATEV das Produkt DATEV";
            this.txtDatev1.Text = "DATEV-Online erfordert die Aktivierung von DATEV Buchungsdatenservice";
            this.txtDatev2.Text = "Mehr Informationen dazu finden Sie";
            this.txturl.Inlines.Add("hier");
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtConsultant.Focus();
            }));
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetDialogResult(false);
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void CmbKost1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cmbKost1.SelectedItem != null)
            {
                this.Kost1 = this.cmbKost1.SelectedIndex - 1;
            }
        }

        private void CmbKost2_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cmbKost2.SelectedItem != null)
            {
                this.Kost2 = this.cmbKost2.SelectedIndex - 1;
            }
        }
    }
}