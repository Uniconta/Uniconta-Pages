using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.DirectDebitPayment;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorPaymentMandateGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorMandateDirectDebit); } }
    }

    public partial class DebtorPaymentMandate : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorPaymentMandate; } }

        public DebtorPaymentMandate(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public DebtorPaymentMandate(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        public DebtorPaymentMandate(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorPaymentMandate.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            string header;
            var syncMaster = dgDebtorPaymentMandate.masterRecord as Debtor;
            if (syncMaster != null)
                header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("Mandates"), syncMaster._Account, syncMaster._Name);
            else
                header = string.Format("{0}", Uniconta.ClientTools.Localization.lookup("Mandates"));

            SetHeader(header);
        }


        SQLCache DebtorCache, PaymentFormatCache;

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgDebtorPaymentMandate.UpdateMaster(master);
            localMenu.dataGrid = dgDebtorPaymentMandate;
            dgDebtorPaymentMandate.api = api;
            dgDebtorPaymentMandate.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDebtorPaymentMandate);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var load = new List<Type>();
            if (master == null)
            {
                load.Add(typeof(Uniconta.DataModel.Debtor));
                load.Add(typeof(Uniconta.DataModel.Creditor));
            }
            if (load.Count != 0)
                LoadType(load.ToArray());

            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            PaymentFormatCache = api.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat));

            GetShowHideStatusInfoSection();
            SetShowHideStatusInfoSection(true);
        }

        protected override void OnLayoutLoaded()
        {
            bool showFields = (dgDebtorPaymentMandate.masterRecords == null);
            DCAccount.Visible = showFields;
            AccountName.Visible = showFields;
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorPaymentMandatePage2) 
                dgDebtorPaymentMandate.UpdateItemSource(argument);
        }


        public override bool HandledOnClearFilter()
        {
            InitQuery();
            return true;
        }
          
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            PaymentFormatCache = api.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)).ConfigureAwait(false);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorPaymentMandate.SelectedItem as DebtorMandateDirectDebit;
            var selectedItems = dgDebtorPaymentMandate.SelectedItems;

            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgDebtorPaymentMandate.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgDebtorPaymentMandate.masterRecord;
                    AddDockItem(TabControls.DebtorPaymentMandatePage2, param, Uniconta.ClientTools.Localization.lookup("Mandates"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] para = new object[3];
                        para[0] = selectedItem;
                        para[1] = true;
                        para[2] = dgDebtorPaymentMandate.masterRecord;
                        AddDockItem(TabControls.DebtorPaymentMandatePage2,para, string.Format("{0} : {1}",Uniconta.ClientTools.Localization.lookup("Mandates"), selectedItem.MandateId.ToString()), null,true);
                    }
                    break;
#if !SILVERLIGHT
                case "RegisterMandate":
                    if (dgDebtorPaymentMandate.ItemsSource == null) return;
                        RegisterMandate(selectedItems); 
                    break;
                case "UnregisterMandate":
                    if (dgDebtorPaymentMandate.ItemsSource == null) return;
                        UnregisterMandate(selectedItems); 
                    break;
                case "ChangeMandateId":
                    if (dgDebtorPaymentMandate.ItemsSource == null) return;
                        ChangeMandateId(selectedItems);
                    break;
                case "ActivateMandateId":
                    if (dgDebtorPaymentMandate.ItemsSource == null) return;
                        ActivateMandateId(selectedItems);
                    break;
                case "ImportMandate":
                    if (dgDebtorPaymentMandate.ItemsSource == null) return;
                        ImportMandate();
                    break;
                case "EnableStatusInfoSection":
                    hideStatusInfoSection = !hideStatusInfoSection;
                    SetShowHideStatusInfoSection(hideStatusInfoSection);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "Filter":
                    InitQuery(); 
                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if !SILVERLIGHT
        private void CallValidateMandate()
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Validate"));
            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    List<DebtorMandateDirectDebit> LstDebMandate = new List<DebtorMandateDirectDebit>();
                    int index = 0;
                    foreach (var rec in (IEnumerable<DebtorMandateDirectDebit>)dgDebtorPaymentMandate.ItemsSource)
                    {
                        int rowHandle = dgDebtorPaymentMandate.GetRowHandleByListIndex(index);
                        index++;
                        if (dgDebtorPaymentMandate.IsRowVisible(rowHandle) && rec.PaymentFormat == cwwin.PaymentFormat._Format && rec._Status == Uniconta.DataModel.MandateStatus.None)
                            LstDebMandate.Add(rec);
                        else
                            continue;

                        if (LstDebMandate.Count == 0)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                            return;
                        }
                    }

                    IEnumerable<DebtorMandateDirectDebit> queryDebMandate = LstDebMandate.AsEnumerable();

                    ValidateMandate(queryDebMandate, cwwin.PaymentFormat, Uniconta.DataModel.MandateStatus.Register, true);
                 }
            };
            cwwin.Show();
               
        }
        private bool ValidateMandate(IEnumerable<DebtorMandateDirectDebit> queryDebMandate, DebtorPaymentFormat debPaymFormat, MandateStatus mandateStatus, bool validateOnly = false)
        {
            try
            {
                var mandateHelper = Common.MandateHelperInstance(debPaymFormat);
                mandateHelper.DirectDebitId = debPaymFormat._CredDirectDebitId;

                var preValErrors = new List<MandateError>();
                var result = mandateHelper.PreValidateMandates(api.CompanyEntity, debPaymFormat, out preValErrors);

                if (result == DirectMandateResults.Error)
                {
                    var preErrors = new List<string>();
                    foreach (MandateError error in preValErrors)
                    {
                        preErrors.Add(error.Message);
                    }

                    if (preErrors.Count != 0)
                    {
                        CWErrorBox cwError = new CWErrorBox(preErrors.ToArray());
                        cwError.Show();
                        return false;
                    }
                }
  
                var valErrors = new List<MandateError>();
                if (mandateStatus == Uniconta.DataModel.MandateStatus.Register)
                    mandateHelper.ValidateRegister(api.CompanyEntity, queryDebMandate, DebtorCache, debPaymFormat, out valErrors);
                else if (mandateStatus == Uniconta.DataModel.MandateStatus.Unregister)
                    mandateHelper.ValidateUnregister(api.CompanyEntity, queryDebMandate, DebtorCache, debPaymFormat, out valErrors);
                else if (mandateStatus == Uniconta.DataModel.MandateStatus.Registered)
                    mandateHelper.ValidateActivate(api.CompanyEntity, queryDebMandate, DebtorCache, debPaymFormat, out valErrors);
                else
                    mandateHelper.ValidateChange(api.CompanyEntity, queryDebMandate, DebtorCache, debPaymFormat, out valErrors);

                foreach (var rec in queryDebMandate)
                {
                    rec.ErrorInfo = Common.VALIDATE_OK;
                }

                foreach (MandateError error in valErrors)
                {
                    var recErr = queryDebMandate.First(s => s.RowId == error.RowId);

                    if (recErr.ErrorInfo == Common.VALIDATE_OK)
                        recErr.ErrorInfo = error.Message;
                    else
                        recErr.ErrorInfo += Environment.NewLine + error.Message;
                }

                if (queryDebMandate.Any(s => s.ErrorInfo == Common.VALIDATE_OK) == false && validateOnly == false)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return false;
                }

                if (validateOnly)
                {
                    var countErr = queryDebMandate.Where(s => (s.ErrorInfo != Common.VALIDATE_OK) && (s.ErrorInfo != null)).Count();
                    var infoTxt = countErr != 0 ? string.Format(Uniconta.ClientTools.Localization.lookup("ValidateFailInLines"), countErr) : Uniconta.ClientTools.Localization.lookup("ValidateNoError");
                    UnicontaMessageBox.Show(infoTxt, Uniconta.ClientTools.Localization.lookup("Warning"));
                }

            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }

            return true;
        }

        private void RegisterMandate(IList lstMandates)
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Register"), true);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var debPaymentFormat = cwwin.PaymentFormat;
                    var schemeType = cwwin.directDebitScheme;

                    if (debPaymentFormat._ExportFormat ==  (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsBS)
                    {
                        UnicontaMessageBox.Show("Denne funktion understøttes ikke af Uniconta", Uniconta.ClientTools.Localization.lookup("Register"));
                        return;
                    }

                    List<DebtorMandateDirectDebit> LstDebMandate = new List<DebtorMandateDirectDebit>();
                    foreach (var s in lstMandates.Cast<DebtorMandateDirectDebit>())
                    {
                        if ((s._Status == Uniconta.DataModel.MandateStatus.None ||
                             s._Status == Uniconta.DataModel.MandateStatus.Unregistered ||
                             s._Status == Uniconta.DataModel.MandateStatus.Unregister ||
                             s._Status == Uniconta.DataModel.MandateStatus.Error) && s.PaymentFormat == debPaymentFormat._Format)
                            LstDebMandate.Add(s);
                    }

                    if (LstDebMandate.Count == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    IEnumerable<DebtorMandateDirectDebit> queryDebMandate = LstDebMandate.AsEnumerable();
                    if (ValidateMandate(queryDebMandate, debPaymentFormat, Uniconta.DataModel.MandateStatus.Register))
                    {
                        try
                        {
                            Common.CreateMandateFile(api, queryDebMandate.Where(s => s.ErrorInfo == Common.VALIDATE_OK), DebtorCache, debPaymentFormat, Uniconta.DataModel.MandateStatus.Register, schemeType);
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }

                }
            };
            cwwin.Show();
        }

        private void UnregisterMandate(IList lstMandates)
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("UnRegister"));

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var debPaymentFormat = cwwin.PaymentFormat;
                    List<DebtorMandateDirectDebit> LstDebMandate = new List<DebtorMandateDirectDebit>();
                    var qrMandates = lstMandates.Cast<DebtorMandateDirectDebit>();
                    foreach (var s in qrMandates)
                    {
                        if (s._Status == Uniconta.DataModel.MandateStatus.Registered && s.PaymentFormat == debPaymentFormat._Format)
                            LstDebMandate.Add(s);
                    }
                    if (LstDebMandate.Count == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    IEnumerable<DebtorMandateDirectDebit> queryDebMandate = LstDebMandate.AsEnumerable();
                    if (ValidateMandate(queryDebMandate, debPaymentFormat, Uniconta.DataModel.MandateStatus.Unregister))
                    {
                        try
                        {
                            Common.CreateMandateFile(api, queryDebMandate.Where(s => s.ErrorInfo == Common.VALIDATE_OK), DebtorCache, debPaymentFormat, Uniconta.DataModel.MandateStatus.Unregister);
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }
                }
            };
            cwwin.Show();
        }

        private void ChangeMandateId(IList lstMandates)
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, string.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"), Uniconta.ClientTools.Localization.lookup("Mandates")));

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var debPaymentFormat = cwwin.PaymentFormat;
                    if (debPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsLS && debPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsBS)
                    {
                        UnicontaMessageBox.Show(string.Format("Function not available for Payment format '{0}'", (Uniconta.DataModel.DebtorPaymFormatType)debPaymentFormat._ExportFormat), string.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"), Uniconta.ClientTools.Localization.lookup("Mandates")));
                        return;
                    }

                    dgDebtorPaymentMandate.Columns.GetColumnByName("OldMandateId").Visible = true;

                    List<DebtorMandateDirectDebit> LstDebMandate = new List<DebtorMandateDirectDebit>();
                    var qrMandates = lstMandates.Cast<DebtorMandateDirectDebit>();
                    foreach (var s in qrMandates)
                    {
                        if (s.PaymentFormat == debPaymentFormat._Format)
                            LstDebMandate.Add(s);
                    }
                    if (LstDebMandate.Count == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    IEnumerable<DebtorMandateDirectDebit> queryDebMandate = LstDebMandate.AsEnumerable();
                    if (ValidateMandate(queryDebMandate, debPaymentFormat, Uniconta.DataModel.MandateStatus.Change))
                    {
                        try
                        {
                            Uniconta.DirectDebitPayment.Common.CreateMandateFile(api, queryDebMandate.Where(s => s.ErrorInfo == Common.VALIDATE_OK), DebtorCache, debPaymentFormat, Uniconta.DataModel.MandateStatus.Change);
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }
                }
            };
            cwwin.Show();
        }

        private void ActivateMandateId(IList lstMandates)
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, string.Format(Uniconta.ClientTools.Localization.lookup("Activate")), showActivateWarning: true);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var debPaymentFormat = cwwin.PaymentFormat;
                    if (debPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsLS && debPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsBS)
                    {
                        UnicontaMessageBox.Show(string.Format("{0} '{1}'", Uniconta.ClientTools.Localization.lookup("NonSupportSEPA"), (Uniconta.DataModel.DebtorPaymFormatType)debPaymentFormat._ExportFormat), Uniconta.ClientTools.Localization.lookup("Activate")); //TODO:Test denne
                        return;
                    }

                    List<DebtorMandateDirectDebit> LstDebMandate = new List<DebtorMandateDirectDebit>();
                    var qrMandates = lstMandates.Cast<DebtorMandateDirectDebit>();
                    foreach (var s in qrMandates)
                    {
                        if (s.PaymentFormat == debPaymentFormat._Format)
                            LstDebMandate.Add(s);
                    }
                    if (LstDebMandate.Count == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    IEnumerable<DebtorMandateDirectDebit> queryDebMandate = LstDebMandate.AsEnumerable();
                    if (ValidateMandate(queryDebMandate, debPaymentFormat, Uniconta.DataModel.MandateStatus.Registered))
                    {
                        try
                        {
                            var lstUpdate = new List<DebtorMandateDirectDebit>();
                            foreach (var rec in queryDebMandate)
                            {
                                if (rec == null)
                                    continue;

                                var statusInfoTxt = rec._StatusInfo;
                                
                                statusInfoTxt = string.Format("({0}) Mandate registered manually\n{1}", Common.GetTimeStamp(), statusInfoTxt);
                                rec.MandateStatus = AppEnums.MandateStatus.ToString((int)Uniconta.DataModel.MandateStatus.Registered);
                                rec.CancellationDate = DateTime.MinValue;
                                rec.ActivationDate = DateTime.Today;
                                                          
                                rec._StatusInfo = Common.StatusInfoTruncate(statusInfoTxt);

                                lstUpdate.Add(rec);
                            }
                            api.UpdateNoResponse(lstUpdate);
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }
                }
            };
            cwwin.Show();
        }

        private void ImportMandate()
        {
            CWDirectDebit cwwin = new CWDirectDebit(api, string.Format(Uniconta.ClientTools.Localization.lookup("Load"), Uniconta.ClientTools.Localization.lookup("Mandates")));

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    try
                    {
                        var debtorPaymentFormat = cwwin.PaymentFormat;
                        if (debtorPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsLS && debtorPaymentFormat._ExportFormat != (byte)Uniconta.DataModel.DebtorPaymFormatType.NetsBS)
                        {
                            UnicontaMessageBox.Show(string.Format("Function not available for Payment format '{0}'", (Uniconta.DataModel.DebtorPaymFormatType)debtorPaymentFormat._ExportFormat), string.Format(Uniconta.ClientTools.Localization.lookup("Load"), Uniconta.ClientTools.Localization.lookup("Mandates")));
                            return;
                        }

                        var sfd = UtilDisplay.LoadOpenFileDialog;
                        var userClickedSave = sfd.ShowDialog();
                        if (userClickedSave != true)
                            return;

                        List<DebtorMandateDirectDebit> mandateLst = new List<DebtorMandateDirectDebit>();
                        int index = 0;
                        foreach (var rec in (IEnumerable<DebtorMandateDirectDebit>)dgDebtorPaymentMandate.ItemsSource)
                        {
                            int rowHandle = dgDebtorPaymentMandate.GetRowHandleByListIndex(index);
                            index++;
                            if (!dgDebtorPaymentMandate.IsRowVisible(rowHandle))
                                continue;

                            mandateLst.Add(rec);
                        }

                        var lines = File.ReadAllLines(sfd.FileName).Select(a => a.Split(';'));

                        var updateRecords = new List<UnicontaBaseEntity>();
                        var cnt = 0;
                        foreach (var rec in lines)
                        {
                            if (debtorPaymentFormat._ExportFormat == (byte)DebtorPaymFormatType.NetsBS)
                            {
                                if (rec.Length < 3)
                                    continue;

                                var debtorAccValue = rec.GetValue(0).ToString();
                                var oldMandateValue = rec.GetValue(1).ToString();
                                var agreementId = rec.GetValue(2).ToString();

                                var mandate = mandateLst.FirstOrDefault(s => s.DCAccount == debtorAccValue);
                                if (mandate != null)
                                {
                                    var debtor = mandate.Debtor;
                                    debtor._PaymentFormat = debtorPaymentFormat.Format;
                                    updateRecords.Add(debtor);

                                    var statusInfoText = string.Format("({0}) Mandate agreement id set to {1}\n{2}", Uniconta.DirectDebitPayment.Common.GetTimeStamp(), agreementId, mandate.StatusInfo);

                                    mandate.OldMandateId = oldMandateValue;
                                    mandate._AgreementId = agreementId;
                                    mandate._StatusInfo = Uniconta.DirectDebitPayment.Common.StatusInfoTruncate(statusInfoText);

                                    updateRecords.Add(mandate);
                                    cnt++;
                                }
                            }
                            else
                            {
                                if (rec.Length < 2)
                                    continue;

                                var debtorAccValue = rec.GetValue(0).ToString();
                                var oldMandateValue = rec.GetValue(1).ToString();

                                var mandate = mandateLst.FirstOrDefault(s => s.DCAccount == debtorAccValue);
                                if (mandate != null)
                                {
                                    var debtor = mandate.Debtor;
                                    debtor._PaymentFormat = debtorPaymentFormat.Format;
                                    updateRecords.Add(debtor);

                                    mandate.OldMandateId = oldMandateValue;
                                    cnt++;
                                }
                            }
                        }

                        dgDebtorPaymentMandate.Columns.GetColumnByName("OldMandateId").Visible = true;

                        if (cnt == 0)
                        {
                            if (debtorPaymentFormat._ExportFormat == (byte)DebtorPaymFormatType.NetsBS)
                                UnicontaMessageBox.Show(string.Format("Couldn't find any match in the file.\n" +
                                    "Note:\nColumn 1 = Customer account\nColumn 2 = Old MandateId\nColumn 3 = AgreementId\nUse a semicolon as field delimiter"), Uniconta.ClientTools.Localization.lookup("Error"));
                            else
                                UnicontaMessageBox.Show(string.Format("Couldn't find any match in the file.\n" +
                                    "Note:\nColumn 1 = Customer account\nColumn 2 = Old MandateId\nUse a semicolon as field delimiter"), Uniconta.ClientTools.Localization.lookup("Error"));
                        }
                        else
                        {
                            if (updateRecords != null && updateRecords.Count != 0)
                                api.Update(updateRecords);
                        }
                    }
                    catch (Exception e)
                    {
                        UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                }
            };
            cwwin.Show();
        }
#endif
        ItemBase ibase;
        bool hideStatusInfoSection = true;
        void GetShowHideStatusInfoSection()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "EnableStatusInfoSection");
        }
        private void SetShowHideStatusInfoSection(bool _hideStatusInfoSection)
        {
            if (ibase == null)
                return;
            if (_hideStatusInfoSection)
            {
                rowgridSplitter.Height = new GridLength(0);
                rowStatusInfoSection.Height = new GridLength(0);
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
            }
        }
    }
}
