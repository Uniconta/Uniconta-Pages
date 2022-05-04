using UnicontaClient.Utilities;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
#if !SILVERLIGHT
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.ChartOfAccount.Reports
{
    public partial class CWCommentsDialogBox : ChildWindow
    {
        [InputFieldData]
        public string Comments { get; set; }
        [InputFieldData]
        public DateTime Date { get; set; }

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CWCommentsDialogBox(string title)
        {
            Init(title, Uniconta.ClientTools.Localization.lookup("Note"));
        }

        public CWCommentsDialogBox(string title, bool enableDateField, DateTime dateField) : this(title)
        {
            if (enableDateField)
            {
                txtBlockDate.Text = Uniconta.ClientTools.Localization.lookup("Date");
                txtBlockDate.Visibility = Visibility.Visible;
                dtDefaultDate.SelectedText = dateField.ToShortDateString();
                dtDefaultDate.Visibility = Visibility.Visible;
            }
        }

        private void Init(string title, string label)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = title;
            this.txtBlockComments.Text = label;
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.txtEditor.EditValueChanging += txtEditor_EditValueChanging;
            this.Loaded += CW_Loaded;
            OKButton.IsEnabled = false; ;
        }

        public CWCommentsDialogBox(string title, string label)
        {
            Init(title, label);
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtEditor.Focus(); }));
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }
        void txtEditor_EditValueChanging(object sender, DevExpress.Xpf.Editors.EditValueChangingEventArgs e)
        {
            if (string.IsNullOrEmpty(Convert.ToString(e.NewValue)) || string.IsNullOrWhiteSpace(Convert.ToString(e.NewValue)))
            {
                OKButton.IsEnabled = false;
                return;
            }
            else
                OKButton.IsEnabled = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Comments = txtEditor.Text;
            this.Date = dtDefaultDate.DateTime;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}


