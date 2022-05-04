using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwGenerateDepreciation : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "PostingDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime PostingDate { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [Display(Name = "TransType", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CwGenerateDepreciation(CrudAPI crudApi)
        {
            PostingDate = PostingDate != DateTime.MinValue ? PostingDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("GenerateOBJ"), Uniconta.ClientTools.Localization.lookup("Depreciation"));
            postingDate.DateTime = PostingDate;
            leGlDailyJr.api = leTransType.api = crudApi;

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
