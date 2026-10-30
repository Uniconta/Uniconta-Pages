using System;
using Org.BouncyCastle.Math.EC;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public partial class PostingTraceDiagramView : System.Windows.Controls.UserControl
    {
        private PostingTraceDiagramViewModel _viewModel;

        public PostingTraceDiagramView()
        {
            InitializeComponent();
        }

        public void Initialize(CrudAPI api)
        {
            _viewModel = new PostingTraceDiagramViewModel(api.CompanyEntity);
            DataContext = _viewModel;
        }

        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register(
                nameof(Results),
                typeof(IEnumerable<PostingTraceResult>),
                typeof(PostingTraceDiagramView),
                new PropertyMetadata(null, OnResultsChanged));

        public IEnumerable<PostingTraceResult> Results
        {
            get { return (IEnumerable<PostingTraceResult>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }

        private static void OnResultsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as PostingTraceDiagramView;

            if (control == null)
                return;

            control._viewModel?.Load(e.NewValue as IEnumerable<PostingTraceResult>);
        }

        internal static void JumpToMainTable(LookUpTable lookupTable, string title)
        {
            if (lookupTable == null
            ||  lookupTable.TableType == null)
                return;

            var controlName = LookupReferences.References(lookupTable.TableType);
            if (string.IsNullOrEmpty(controlName))
                return;

            BasePage.LookUpTable(lookupTable, title ?? string.Empty, controlName, currentPage: null);
        }
    }
}
