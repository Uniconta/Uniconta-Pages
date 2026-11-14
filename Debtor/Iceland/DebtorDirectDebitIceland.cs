using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.DirectDebitPayment;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    // Iceland online claims on the Direct Debit page: send the open invoices as claims, and open the claims register (Kröfuskrá)
    public partial class DebtorDirectDebit
    {
        static readonly string[] IcelandClaimsActions = { "IcelandSendClaims", "IcelandClaims" };

        void RemoveIcelandClaimsMenu()
        {
            if (api.CompanyEntity._CountryId != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, IcelandClaimsActions);
        }

        bool IcelandClaimsAction(string ActionType)
        {
            switch (ActionType)
            {
                case "IcelandSendClaims":
                    if (dgDebtorTranOpenGrid.ItemsSource != null)
                        SendClaimsOnline();
                    return true;
                case "IcelandClaims":
                    AddDockItem(TabControls.IcelandClaimsPage, null, Uniconta.ClientTools.Localization.lookup("ClaimsRegister"));
                    return true;
            }
            return false;
        }

        async void SendClaimsOnline()
        {
            // the server refuses online claims without the AutoBanking add-in; say so before the format and the rows are chosen
            if (!api.CompanyEntity._DirectDebit)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AutoBankingNotActivated"), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }
            var savetask = saveGrid();
            if (savetask != null)
                await savetask;

            var cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("OnlineClaims"));
            cwwin.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult != true)
                    return;

                var format = cwwin.PaymentFormat;
                if (format == null || format._ExportFormat != (byte)DebtorPaymFormatType.Iceland)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandNoClaimsFormat"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                // several rows selected: those; otherwise every visible row
                IEnumerable<DebtorTransDirectDebit> rows;
                var selected = dgDebtorTranOpenGrid.SelectedItems?.OfType<DebtorTransDirectDebit>().ToList();
                if (selected != null && selected.Count > 1)
                    rows = selected;
                else
                {
                    ExpandCollapseAllGroups();
                    rows = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransDirectDebit>;
                }

                // only invoices without a claim; the server skips any that already have a live claim
                var invoices = new List<DCTransOpen>();
                if (rows != null)
                    foreach (var rec in rows)
                        if (rec.Trans != null && (rec._PaymentRefId == 0 || rec._PaymentStatus == PaymentStatusLevel.None) &&
                            (string.IsNullOrEmpty(rec._PaymentFormat) || rec._PaymentFormat == format._Format))
                            invoices.Add(rec);

                if (invoices.Count == 0)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                var capi = new IcelandClaimsAPI(api);
                IcelandClaimsResult result;
                busyIndicator.IsBusy = true;
                try
                {
                    result = await capi.Create(format, invoices);
                }
                catch (Exception ex)
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show(ex);
                    return;
                }
                busyIndicator.IsBusy = false;
                IcelandClaimsPage.ShowResult(result, capi);
                btnSearch();
            };
            cwwin.Show();
        }
    }
}
