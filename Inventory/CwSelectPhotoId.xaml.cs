using DevExpress.Charts.Native;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSelectPhotoId : ChildWindow
    {
        public int Photo { get; set; }
        InvVariantDetailClient row;
        CrudAPI api;
        public CwSelectPhotoId(CrudAPI _api, InvVariantDetailClient _row)
        {
            row = _row;
            Photo = row._Photo;
            api = _api;
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("SetOBJ"),Uniconta.ClientTools.Localization.lookup("Photo"));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        bool lookupIsSet = false;
        private void liPhoto_LookupButtonClicked(object sender)
        {
            var lookupEditor = sender as LookupEditor;
            if (!lookupIsSet)
            {
                lookupEditor.PopupContentTemplate = (Application.Current).Resources["LookUpDocumentClientPopupContent"] as ControlTemplate;
                lookupEditor.ValueMember = "RowId";
                lookupEditor.SelectedIndexChanged += LookupEditor_SelectedIndexChanged;
                lookupIsSet = true;
                lookupEditor.ItemsSource = api.Query<UserDocsClient>(row).GetAwaiter().GetResult();
            }
        }

        private void LookupEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookupEditor = sender as LookupEditor;
            var docsClient = lookupEditor.SelectedItem as UserDocsClient;
            txtPhoto.EditValue = Photo = docsClient?._RowId ?? 0;
        }
    }
}
