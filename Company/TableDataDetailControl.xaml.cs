using UnicontaClient.Utilities;
using Uniconta.ClientTools.Util;
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
using Uniconta.API.System;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TableDataDetailControl : System.Windows.Controls.UserControl
    {
        public TableDataDetailControl()
        {
            InitializeComponent();
            layoutItems.Tag = this;
        }

        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }

        public void Refresh(object argument, object dataContext)
        {
            var argumentParams = argument as object[];
            if (argumentParams != null)
            {
                var Operation = Convert.ToInt32(argumentParams[0]);
                if (Operation != 3)
                {
                    layoutItems.DataContext = null;
                    layoutItems.DataContext = dataContext;
                }
            }
        }
        public void CreateUserField(TableField[] UserFieldDef, bool hasKeyFields, CrudAPI api,string pkPromptHeaderKey)
        {
            if (hasKeyFields)
                UserFieldControl.CreateKeyFieldsGroupOnPage2(layoutItems, pkPromptHeaderKey, true);
            if (UserFieldDef != null)
                UserFieldControl.CreateUserFieldOnPage2(layoutItems, UserFieldDef, (RowIndexConverter)this.Resources["RowIndexConverter"], api, this, true);
        }
    }
}
