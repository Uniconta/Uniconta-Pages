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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProdCatalogPage2 : FormBasePage
    {
        ProdCatalogClient editRow;
        public override void OnClosePage(object[] RefreshParams){ globalEvents.OnRefresh(NameOfControl, RefreshParams);}
        public override string NameOfControl { get { return TabControls.ProdCatalogPage2; } }
        public override Type TableType { get { return typeof(ProdCatalogClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editRow; } set { editRow = (ProdCatalogClient)value; } }

        public ProdCatalogPage2(UnicontaBaseEntity sourceData, bool isEdit = true) : base(sourceData, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
                editRow = (ProdCatalogClient)StreamingManager.Clone(sourceData);
            InitPage();
        }

        public ProdCatalogPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage();
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }

        void InitPage()
        {
            layoutControl = layoutItems;
            if (LoadedRow == null && editRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editRow = CreateNew() as ProdCatalogClient;
            }
            layoutItems.DataContext = editRow;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            frmRibbon.OnItemClicked += frmRibbon_BaseActions;
        }
    }
}
