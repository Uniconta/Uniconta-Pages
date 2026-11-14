using System;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    // "Senda til RSK" for an Icelandic company: the server files lines 1-7 of the saved VAT settlement through api.uniconta.is
    // (VatAPI.Upload, method 148) with the RSK password given here, and keeps RSK's receipt on the settlement (GetReciept).
    static class IcelandVatFiling
    {
        static string Caption { get { return Uniconta.ClientTools.Localization.lookup("ReportTo"); } }

        /// <summary>Files the settlement; when RSK already has the period, asks whether to correct it. The receipt, or null.</summary>
        static internal async Task<byte[]> File(CrudAPI api, GLVatReported settlement, string password)
        {
            var keys = new IcelandRskKeysAPI(api);
            var state = await keys.State();
            var saved = state != null && state.VatSaved;
            if (string.IsNullOrEmpty(password) && !saved)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandEnterRskPassword"), Caption);
                return null;
            }
            var vapi = new VatAPI(api);
            if (!await ConfirmPeriod(vapi, settlement, password))
                return null;
            var numbers = new long[20]; // the server takes the lines from the saved settlement
            var err = await vapi.Upload(settlement, numbers, password, false);
            if (err == ErrorCodes.RecordExists)
            {
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandVatCorrectQuestion"), Caption, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return null;
                err = await vapi.Upload(settlement, numbers, password, true);
            }
            if (err != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(err);
                return null;
            }
            var receipt = await vapi.GetReciept(settlement);
            if (receipt == null)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("IcelandVatNoReceipt"), Caption);
            if (!saved && !string.IsNullOrEmpty(password) && UnicontaMessageBox.Show(
                    "Vista lykilorð RSK fyrir fyrirtækið? Það er geymt dulkóðað á þjóninum, og þá þarf ekki að slá það inn næst.", Caption, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var s = await keys.Save(IcelandRskKeyKind.Vat, password);
                if (s == null || s.Err != ErrorCodes.Succes)
                    UnicontaMessageBox.Show(s?.Message ?? Uniconta.ClientTools.Localization.lookup("CouldNotSave"), Caption);
            }
            return receipt;
        }

        /// <summary>The VAT report of an Icelandic company opens on the period RSK expects next, when a VSK password is saved for the
        /// company (method 519 without a settlement). The dates stay the user's to change; they are only set while still the
        /// defaults, and nothing is shown when the lookup fails. "Senda til RSK" still checks the period before sending.</summary>
        static internal async void StartFromRskPeriod(CrudAPI api, DateEditor from, DateEditor to)
        {
            try
            {
                DateTime f0 = from.DateTime, t0 = to.DateTime;
                var next = await ExpectedPeriod(api);
                if (next != null && from.DateTime == f0 && to.DateTime == t0)
                {
                    from.DateTime = next.From;
                    to.DateTime = next.To;
                }
            }
            catch
            {
                // the user picks the dates
            }
        }

        /// <summary>RSK's next period with the saved VSK password; null without one or when the lookup fails</summary>
        static internal async Task<IcelandVatNextPeriod> ExpectedPeriod(CrudAPI api)
        {
            var state = await new IcelandRskKeysAPI(api).State();
            if (state == null || !state.VatSaved)
                return null;
            var next = await new VatAPI(api).IcelandNextPeriod(null, null);
            return next != null && next.Err == ErrorCodes.Succes && next.From != DateTime.MinValue && next.To != DateTime.MinValue ? next : null;
        }

        /// <summary>Shows the period RSK expects next (method 519) and asks before sending. A settlement whose dates are not that
        /// period is not sent, nor is anything when the lookup fails. A settlement filed before is sent by RSK's code from its
        /// receipt, so the server then offers the correction instead.</summary>
        static async Task<bool> ConfirmPeriod(VatAPI vapi, GLVatReported settlement, string password)
        {
            var next = await vapi.IcelandNextPeriod(settlement, password);
            if (next == null)
            {
                UtilDisplay.ShowErrorCode(vapi.LastError);
                return false;
            }
            if (next.Err != ErrorCodes.Succes)
            {
                UnicontaMessageBox.Show(string.IsNullOrEmpty(next.Message) ? next.Err.ToString() : next.Message, Caption);
                return false;
            }
            if (next.FiledBefore)
                return true;
            var expected = string.Format(Uniconta.ClientTools.Localization.lookup("IcelandRskPeriodWithDueDate"), next.From.ToString("d.M."), next.To.ToString("d.M.yyyy"), next.DueDate.ToString("d.M.yyyy"));
            if (!next.Matches)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandVatPeriodMismatch"),
                    expected, settlement._FromDate.ToString("d.M."), settlement._ToDate.ToString("d.M.yyyy")), Caption);
                return false;
            }
            return UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandVatPeriodConfirm"), expected), Caption,
                MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }
    }
}
