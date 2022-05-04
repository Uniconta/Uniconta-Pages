using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWFrontPage : ChildWindow
    {
        public string RichEditText { get; set; }
        public string FrontPageTemplate { get; set; }

        private CrudAPI Api;
        public CWFrontPage(CrudAPI api, string title, string text, string template)
        {
            RichEditText = text;
            FrontPageTemplate = template;
            this.DataContext = this;
            InitializeComponent();
            Api = api;
            this.Title = title;
            this.txtCoverPageTemplate.Text = string.Format("{0} {1}:", Uniconta.ClientTools.Localization.lookup("FrontPage"), Uniconta.ClientTools.Localization.lookup("Templates"));
#if SILVERLIGHT
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
            LoadTemplates();
        }

        async private void LoadTemplates()
        {
            var instance = Activator.CreateInstance(typeof(StandardBalanceFrontPageClient)) as UnicontaBaseEntity;
            var source = await Api.Query(instance, null, null) as UserReportDevExpressClient[];
            if (source != null && source.Length > 0)
            {
                var templates = source.Select(p => p.Name).ToList();
                templates.Insert(0, string.Empty);
                cmbCoverPageTemplate.ItemsSource = templates.ToArray();
            }
            else
                cmbCoverPageTemplate.ItemsSource = null;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtEditor.Text))
                    txtEditor.Focus();
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
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

