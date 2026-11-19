using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.DirectDebitPayment;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    // Iceland claims on the Direct Debit page: send the open invoices as claims online, open the claims register (Kröfuskrá),
    // and the claim's dates per row: Payment date is its due date (gjalddagi), Eindagi its final due date
    public partial class DebtorDirectDebit
    {
        static readonly string[] IcelandClaimsActions = { "IcelandSendClaims", "IcelandClaims" };

        void RemoveIcelandClaimsMenu()
        {
            if (api.CompanyEntity._CountryId != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, IcelandClaimsActions);
            else
                dgDebtorTranOpenGrid.tableView.CellValueChanged += IcelandCellValueChanged;
            SetIcelandClaimColumns();
        }

        bool icelandColumnsSet;

        // Eindagi is a column where the company has an Icelandic claims format; elsewhere the page is as it was
        void SetIcelandClaimColumns()
        {
            if (icelandColumnsSet)
                return;
            var claims = false;
            if (api.CompanyEntity._CountryId == CountryCode.Iceland)
            {
                if (PaymentFormatCache == null)
                    return; // decided when the formats are loaded
                foreach (var rec in PaymentFormatCache.GetNotNullArray)
                    if (IcelandClaimsFormat(rec as DebtorPaymentFormat) != null)
                    {
                        claims = true;
                        break;
                    }
            }
            icelandColumnsSet = true;
            if (!claims)
                GraceDate.Visible = GraceDate.ShowInColumnChooser = false;
        }

        // a new payment date moves the row's Eindagi with it
        void IcelandCellValueChanged(object sender, DevExpress.Xpf.Grid.CellValueChangedEventArgs e)
        {
            var rec = e.Row as DebtorTransDirectDebit;
            if (rec == null || e.Column.FieldName != "PaymentDate" || rec.PaymentFormat == null || PaymentFormatCache == null)
                return;
            IcelandEindagi(rec, IcelandClaimsFormat(PaymentFormatCache.Get(rec.PaymentFormat) as DebtorPaymentFormat));
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

        // A claim is due on the row's payment date, and the invoice's grace date is its final due date (eindagi), not the day
        // to collect: without a payment date the row starts from the due date
        static DateTime IcelandPayDate(DCTransOpen rec)
        {
            return rec._PaymentDate != DateTime.MinValue ? rec._PaymentDate : rec._DueDate;
        }

        // The row's Eindagi as the claim gets it: the format's days after the payment date when the format has that rule, else
        // the invoice's own; never before the payment date. Only until the row is sent
        static void IcelandEindagi(DebtorTransDirectDebit rec, DebtorPaymentFormatClientIceland format)
        {
            if (format == null || rec._PaymentStatus != PaymentStatusLevel.None || rec._PaymentDate == DateTime.MinValue)
                return;
            var eindagi = format.FinalDueDateDays != 0 ? rec._PaymentDate.AddDays(format.FinalDueDateDays) : rec._GraceDate;
            if (eindagi < rec._PaymentDate)
                eindagi = rec._PaymentDate;
            if (eindagi != rec._GraceDate)
                rec.GraceDate = eindagi;
        }

        readonly Dictionary<string, DebtorPaymentFormatClientIceland> icelandFormats = new Dictionary<string, DebtorPaymentFormatClientIceland>();

        // the payment format as an Icelandic claims format, with its settings; null when it is not one
        DebtorPaymentFormatClientIceland IcelandClaimsFormat(DebtorPaymentFormat format)
        {
            if (format == null || format._Format == null || format._ExportFormat != (byte)DebtorPaymFormatType.Iceland)
                return null;
            DebtorPaymentFormatClientIceland iceland;
            if (!icelandFormats.TryGetValue(format._Format, out iceland))
            {
                iceland = new DebtorPaymentFormatClientIceland();
                StreamingManager.Copy(format, iceland);
                icelandFormats[format._Format] = iceland;
            }
            return iceland;
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

                // only invoices without a claim; the server skips any that already have a live claim.
                // Merge payments on: the rows of a merge group go on one claim, and the rows the merge left out are not sent
                var merge = doMergePaym;
                var excluded = Uniconta.ClientTools.Localization.lookup("Excluded");
                var invoices = new List<DCTransOpen>();
                if (rows != null)
                    foreach (var rec in rows)
                        if (rec.Trans != null && (rec._PaymentRefId == 0 || rec._PaymentStatus == PaymentStatusLevel.None) &&
                            (string.IsNullOrEmpty(rec._PaymentFormat) || rec._PaymentFormat == format._Format) &&
                            !(merge && rec.MergeDataId == excluded))
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
                    // as the rows show: due on the payment date, final due date the row's Eindagi
                    result = await capi.CreateAsShown(format, invoices, t =>
                    {
                        var id = merge ? ((DebtorTransDirectDebit)t).MergeDataId : null;
                        return id == Common.MERGEID_SINGLEPAYMENT ? null : id;
                    });
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
