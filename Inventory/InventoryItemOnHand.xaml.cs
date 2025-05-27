using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;


using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InventoryItemOnHand : ControlBasePage
    {

        InvItemClient item;
        public InventoryItemOnHand(UnicontaBaseEntity sourcedata)
           : base(sourcedata)
        {
            InitializeComponent();
            Init(sourcedata);
        }
        public InventoryItemOnHand(SynchronizeEntity sourcedata)
           : base(true, sourcedata)
        {
            InitializeComponent();
            Init(sourcedata.Row);
        }
        void Init(UnicontaBaseEntity sourcedata)
        {
            item = sourcedata as InvItemClient;
            this.DataContext = item;
            this.Loaded += InventoryItemOnHand_Loaded;
        }

        private void InventoryItemOnHand_Loaded(object sender, RoutedEventArgs e)
        {
            var curpanel = this.ParentControl;
            curpanel.AllowDock = false;
            if (curpanel?.IsFloating == true)
            {
                curpanel.Parent.FloatSize = new System.Windows.Size(370, 340);
            }
            curpanel.UpdateLayout();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            item = args as InvItemClient;
            this.DataContext = item;
            base.SyncEntityMasterRowChanged(args);
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            var cache = api.CompanyEntity.GetCache(typeof(InvItem));
            var entity = argument as UnicontaBaseEntity;
            var prop = CorasauDataGrid.GetItemProperty(entity);
            var oldItemNo = item._Item;
            var itemNo = prop?.GetValue(entity, null) as string;
            if (itemNo != null && oldItemNo != itemNo)
            {
                var currentItem = cache.Get(itemNo);
                if (currentItem != null)
                    await api.Read(currentItem as UnicontaBaseEntity);
                item = currentItem as InvItemClient;
                this.DataContext = item;
                base.Utility_Refresh(screenName, argument);
            }
        }
    }
}
