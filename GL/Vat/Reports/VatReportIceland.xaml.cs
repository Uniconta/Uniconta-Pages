using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Security;
using System.Threading.Tasks;
using System.Windows;
using RSK;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using RSK.Vsk;
using Uniconta.Common;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
  public class VatReportIcelandGrid : CorasauDataGridClient
  {
    public override Type TableType
    {
      get { return typeof(VatSumOperationReport); }
    }

    public override bool Readonly
    {
      get { return true; }
    }
  }

  public partial class VatReportIceland : GridBasePage
  {
#region Member Constants

#endregion

#region Member variables

    protected List<VatSumOperationReport> vatSumOperationLst;

#endregion

#region Properties

    private List<VatSumOperationReport> VatSumOperationLst
    {
      get { return vatSumOperationLst; }

      set { vatSumOperationLst = value; }
    }

    private DateTime FromDate { get; }
    private DateTime ToDate { get; }

#endregion

    public override string NameOfControl
    {
      get { return "VatReportIceland"; }
    }

        public VatReportIceland(List<VatSumOperationReport> VatSumOperationLst, DateTime fromDate, DateTime toDate) : base(null)
        {
            this.VatSumOperationLst = VatSumOperationLst;
            this.FromDate = fromDate;
            this.ToDate = toDate;
            InitializeComponent();
            dgVatReportIceland.BusyIndicator = busyIndicator;
            dgVatReportIceland.api = api;
            localMenu.dataGrid = dgVatReportIceland;
            SetRibbonControl(localMenu, dgVatReportIceland);
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            double sum = 0d;
            foreach (var rec in VatSumOperationLst)
            {
                string s;
                switch (rec._Line)
                {
                    case 1:
                        s = "Skattskyld velta efra þrep";
                        break;
                    case 2:
                        s = "Skattskyld velta neðra þrep?";
                        break;
                    case 3:
                        s = "Undanþegin velta (12. gr.)";
                        break;
                    case 4:
                        s = "Útskattur efra þrep";
                        break;
                    case 5:
                        s = "Útskattur neðra þrep";
                        break;
                    case 6:
                        s = "Innskattur efra þrep";
                        break;
                    case 7:
                        s = "Innskattur neðra þrep";
                        break;
                    default:
                        s = null;
                        break;
                }

                rec._Text = s;
                if (rec._Line >= 6)
                    sum += rec._Amount;
                else if (rec._Line >= 4) sum -= rec._Amount;
            }
            dgVatReportIceland.ItemsSource = VatSumOperationLst;
            txtSystemInfo.Text = sum < 0 ? $"Áætluð álagning: {-sum:C0}" : $"Áætluð endurgreiðsla: {sum:C0}";
            dgVatReportIceland.Visibility = Visibility.Visible;
#if !SILVERLIGHT
            txtFromDate.DateTime = fromDate;
            txtToDate.DateTime = toDate;
            txtKennitala.Text = api.CompanyEntity._Id;
            txtVatNo.Text = api.CompanyEntity._VatNumber;
            // Can add if necessary
            txtPassword.Text = (string)(api.CompanyEntity.GetUserField("RskPassword") ?? string.Empty);
#endif
        }

        private async void localMenu_OnItemClicked(string ActionType)
        {
            var previousContent = busyIndicator.BusyContent;

            //MessageBox.Show(ActionType);
            switch (ActionType)
            {
#if !SILVERLIGHT
                case "ResetTest": //Only for debug purposes. Not active for live web service
                    busyIndicator.BusyContent = "Endurstilli prófun";
                    busyIndicator.IsBusy = true;
                    //MessageBox.Show("resetting");
                    await ResetTestVSK(api, VatSumOperationLst);
                    break;

                case "SendToRSK":
                    busyIndicator.BusyContent = "Sendi til RSK";
                    busyIndicator.IsBusy = true;
                    //MessageBox.Show("sending to rsk");
                    var sendingResult = await SendToRSK(VatSumOperationLst);
                    if (sendingResult != null
                     && sendingResult.status.code == 0)
                    {
                        var pdfBytes = sendingResult.Svar.PDFKvittun;
                        var savingResult = await SavePdfFileToVouchers(api, sendingResult.Svar);
                        if (savingResult == ErrorCodes.Succes)
                        {
                            Dispatcher?.Invoke(() => DisplaySavedPdf(pdfBytes));
                            UnicontaMessageBox.Show("Kvittun vistuð í innhólf", "Aðgerð tókst"); // Success saving
                        }
                        else
                        {
                            UnicontaMessageBox.Show("Villa við vistun á skjali í innhólf", "Villa"); // Error saving
                        }
                    }

                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = previousContent;
        }
#if !SILVERLIGHT
    private void DisplaySavedPdf(
      byte[] pdfBytes)
    {
      var ms = new MemoryStream(pdfBytes);
      pdfViewer.DocumentSource = ms;
      var controlWidth = gridColumn.Width;
      gridColumn.Width = pdfColumn.Width;
      pdfColumn.Width = controlWidth;
    }

    private async Task<ErrorCodes> SavePdfFileToVouchers(
      CrudAPI crudApi,
      docType_ns_VSKSkyrslaSvar reply)
    {
      var errorCode = ErrorCodes.NoSucces;
      byte[] docBytes = reply.PDFKvittun;
      var vc = new VouchersClient
      {
        _Data = docBytes,
        Fileextension = FileextensionsTypes.PDF,
        Text = "VSK skil",
        _Amount = reply.NidurstadaSkila.Fjarhaedir.TilGreidslu ?? 0,
        _Content = ContentTypes.Documents,
      };
      var success = false;
      while (!success)
      {
        errorCode = await crudApi.Insert(vc);
        success = errorCode == ErrorCodes.Succes;
        if (!success)
          UnicontaMessageBox.Show(
            Localization.GetLocalization(Uniconta.Common.Language.Is).Lookup(errorCode.ToString()) + "\nReyna aftur?", // Try again?
            "Villa við vistun", // Error while saving
            messageBoxImage: MessageBoxImage.Error,
            messageBoxButton: MessageBoxButton.YesNo
          );
      }
      return errorCode;
    }

    private async Task ResetTestVSK(
      CrudAPI crudApi,
      List<VatSumOperationReport> vatSumOperationReports) // Only for test service URI
    {
      try
      {
        var client = new VskClient(txtKennitala.Text, txtPassword.Text);
        var result = await client.ResetTestAsync(txtKennitala.Text, ToDate, api.CompanyEntity._VatNumber);
        if (result.EydaSkyrsluIProfunSvar != null)
          UnicontaMessageBox.Show(result.EydaSkyrsluIProfunSvar.status.message, "Villa " + result.EydaSkyrsluIProfunSvar.status.code);
      } catch (MessageSecurityException)
      {
        UnicontaMessageBox.Show("Rangt notendanafn eða lykilorð", "Villa"); //Wrong UserName or Password
      } catch (Exception e)
      {
        UnicontaMessageBox.Show($"{e.Message} \n\n {e.StackTrace}" , "Villa");
      }
    }

    private async Task<SkilaVSKSkyrsluSvar> SendToRSK(
      List<VatSumOperationReport> vatSumOperationReports)
    {
      SkilaVSKSkyrsluResponse response = null;
      try
      {
        var client = new VskClient(txtKennitala.Text, txtPassword.Text);
        long[] longsOrdered = vatSumOperationReports.OrderBy(o => o._Line)
                                                    .Select(i => (long)Math.Round(i._Amount != 0 ? i._Amount : i._AmountBase != 0 ? i._AmountBase : 0))
                                                    .ToArray();
        response = await client.SkilaVSKSkyrsluAsync(
          kennitala: txtKennitala.Text,
          vskNumer: txtVatNo.Text,
          toDate: ToDate,
          velta24: longsOrdered[0],
          velta11: longsOrdered[1],
          velta0: longsOrdered[2],
          ut24: longsOrdered[3],
          ut11: longsOrdered[4],
          inn24: longsOrdered[5],
          inn11: longsOrdered[6]
        );
      } catch (MessageSecurityException)
      {
        UnicontaMessageBox.Show("Rangt notendanafn eða lykilorð", "Villa"); // Wrong user/pass
      } catch (Exception e)
      {
        UnicontaMessageBox.Show(e.Message, "Villa");
      }

      return response?.SkilaVSKSkyrsluSvar;
    }

#endif
  }
}