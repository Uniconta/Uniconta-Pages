using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwPostWIP :  ChildWindow
    {
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public static bool Simulate { get; set; }

        [Display(Name = "PostingDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime PostingDate { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.NumberSerie))]
        [Display(Name = "NumberSerie", ResourceType = typeof(InputFieldDataText))]
        public static string NumberSerie { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [Display(Name = "TransType", ResourceType = typeof(InputFieldDataText))]
        public static string TransType { get; set; }

        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public static string Comment { get; set; }

         protected override bool ShowTableValueButton { get { return false; } }

        public CwPostWIP(CrudAPI crudApi)
        {
            PostingDate = PostingDate != DateTime.MinValue ? PostingDate : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("PostWIP");
            postingDate.DateTime = PostingDate;
            leNumberSerie.api= leTransType.api = crudApi;
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
