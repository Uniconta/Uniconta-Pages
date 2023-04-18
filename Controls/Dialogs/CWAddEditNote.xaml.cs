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
using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWAddEditNote.xaml
    /// </summary>
    public partial class CWAddEditNote : ChildWindow
    {
        CrudAPI API;
        public ErrorCodes result;
        public InvTransClient invTransClient;
        public GLTransClient glTransClient;
        public string Note;
        public GLTransNote glTransNote;

        ProductionPostedTransClient prodPostedTransClient;

        public CWAddEditNote(CrudAPI api, InvTransClient invTransClient, GLTransClient glTransClient, bool IsNote = false)
        {
            InitializeComponent();
            this.DataContext = this;
            API = api;
            this.invTransClient = invTransClient;
            this.glTransClient = glTransClient;
            if (!IsNote)
                this.Title = Uniconta.ClientTools.Localization.lookup("AddEditNote");
            else
                this.Title = Uniconta.ClientTools.Localization.lookup("Note");
            this.Loaded += CWAddEditNote_Loaded;

            if (IsNote)
            {
                OKButton.Visibility = Visibility.Collapsed;
                txtNote.IsReadOnly = true;
            }
        }

        public CWAddEditNote(CrudAPI api, ProductionPostedTransClient productionPostedClient, bool IsNote = false) : this(api, null, null, IsNote)
        {
            prodPostedTransClient = productionPostedClient;
        }

        private async void CWAddEditNote_Loaded(object sender, RoutedEventArgs e)
        {
            if (invTransClient != null)
            {
                Note = invTransClient._Note;
            }
            else if (glTransClient != null)
            {
                busyIndicator.IsBusy = true;
                var notes = await API.Query<GLTransNote>(new UnicontaBaseEntity[] { glTransClient }, null);
                glTransNote = notes?.FirstOrDefault();
                Note = glTransNote?._Note;
                busyIndicator.IsBusy = false;
            }
            else if(prodPostedTransClient!=null)
            {
                Note = prodPostedTransClient.Note;
            }
            if (!string.IsNullOrEmpty(Note))
                txtNote.Text = Note;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtNote.Text))
                    txtNote.Focus();
                else
                    OKButton.Focus();
            }));
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

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            result = ErrorCodes.NoSucces;
            if (invTransClient != null)
            {
                invTransClient._Note = txtNote.Text != string.Empty ? txtNote.Text : null;
                var invApi = new InvoiceAPI(API);
                result = await invApi.UpdateNote(invTransClient);
                busyIndicator.IsBusy = false;
            }
            else if (glTransClient != null)
            {
                if (glTransNote == null)
                {
                    glTransNote = new GLTransNote();
                    glTransNote._Note = txtNote.Text;
                    glTransNote.SetMaster(glTransClient);
                    result = await API.Insert(glTransNote);
                    busyIndicator.IsBusy = false;
                }
                else
                {
                    if (string.IsNullOrEmpty(txtNote.Text) || string.IsNullOrWhiteSpace(txtNote.Text))
                    {
                        result = await API.Delete(glTransNote);
                        busyIndicator.IsBusy = false;
                    }
                    else
                    {
                        glTransNote._Note = txtNote.Text;
                        result = await API.Update(glTransNote);
                        busyIndicator.IsBusy = false;
                    }
                }
            }
            else if(prodPostedTransClient!=null)
            {
                prodPostedTransClient._Note = txtNote.Text != string.Empty ? txtNote.Text : null;
                var invoieAPI = new InvoiceAPI(API);
                result = await invoieAPI.UpdateNote(prodPostedTransClient);
                busyIndicator.IsBusy = false;
            }

            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
