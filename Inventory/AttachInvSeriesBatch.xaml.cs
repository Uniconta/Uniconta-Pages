using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.API.Inventory;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AttachInvSeriesBatchGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvSerieBatchClient); } }
    }
    public partial class AttachInvSeriesBatch : GridBasePage
    {
        InvTrans invTrans;
        InvItem invItemMaster;
        public AttachInvSeriesBatch(InvTrans trans) : base(trans)
        {
            InitializeComponent();
            this.invTrans = trans;
            var cache = api.CompanyEntity.GetCache(typeof(InvItem));
            invItemMaster = cache.Get(trans._Item) as InvItem;
            dgInvSeriesBatchGrid.api = api;
            dgInvSeriesBatchGrid.UpdateMaster(invItemMaster);
            SetRibbonControl(localMenu, dgInvSeriesBatchGrid);
            dgInvSeriesBatchGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            InvSerieBatchClient selectedItem = dgInvSeriesBatchGrid.SelectedItem as InvSerieBatchClient;
            switch (ActionType)
            {
                case "AttachSerieBatch":
                    if (selectedItem == null)
                        return;
                    AttachSerieBatch(selectedItem as InvSerieBatch);
                    break;
            }
        }
        async void AttachSerieBatch(InvSerieBatch selectedItem)
        {
            var objTransactionsAPI = new TransactionsAPI(api);
            double qty = Uniconta.Common.Utility.NumberConvert.ToDoubleNoThousandSeperator(txtQty.Text);
            var res = await objTransactionsAPI.AttachSerieBatch(invTrans, selectedItem, qty);
            if (res != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(res);
            else
                dockCtrl.CloseDockItem();
        }
    }
}
