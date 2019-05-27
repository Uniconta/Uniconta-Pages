using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWJoinTwoDimensions.xaml
    /// </summary>
    public partial class CWJoinTwoDimensions : ChildWindow
    {
        SQLCache dimensionCache;
        CrudAPI crudApi;
        string dim;
        public Task<ErrorCodes> JoinResult;

        public CWJoinTwoDimensions(CrudAPI api, string dimension)
        {
            DataContext = this;
            InitializeComponent();
            Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Dimensions"));
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            crudApi = api;
            dim = dimension;
            cmbFromDimension.api = cmbToDimension.api = api;
            Loaded += CWJoinTwoDimensions_Loaded;
        }

        private void CWJoinTwoDimensions_Loaded(object sender, RoutedEventArgs e)
        {
            SetItemSource();
            Dispatcher.BeginInvoke(new Action(() => { cmbToDimension.Focus(); }));
        }

        async private void SetItemSource()
        {
            if (string.IsNullOrEmpty(dim)) return;

            Type t;
            var comp = crudApi.CompanyEntity;
            if (dim == comp._Dim1)
                t = typeof(GLDimType1);
            else if (dim == comp._Dim2)
                t = typeof(GLDimType2);
            else if (dim == comp._Dim3)
                t = typeof(GLDimType3);
            else if (dim == comp._Dim4)
                t = typeof(GLDimType4);
            else if (dim == comp._Dim5)
                t = typeof(GLDimType5);
            else
                return;

            dimensionCache = comp.GetCache(t) ?? await comp.LoadCache(t, crudApi);
            cmbFromDimension.ItemsSource = dimensionCache;
            cmbToDimension.ItemsSource = dimensionCache;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var copyFromDim = cmbFromDimension.SelectedItem as GLDimType;
            var copyToDim = cmbToDimension.SelectedItem as GLDimType;

            if (copyFromDim != null && copyToDim != null)
            {
                var mainTableApi = new MaintableAPI(crudApi);
                JoinResult = mainTableApi.JoinTwoDimensions(copyFromDim, copyToDim);
                DialogResult = true;
            }
            else
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Dimension"))), Uniconta.ClientTools.Localization.lookup("Error"));
        }
    }
}
