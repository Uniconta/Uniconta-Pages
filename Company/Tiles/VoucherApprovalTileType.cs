using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Files;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.Controls;
using System.Text.RegularExpressions;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    [DisplayName("Voucher approval")]
    public sealed class VoucherApprovalTileType : ITileTypeWithParms, ITileDropTarget
    {
        public VoucherApprovalTileParameters Settings { get; private set; } = new VoucherApprovalTileParameters();
        public object Parameters => Settings;
        public string Serialize() => Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        public void Deserialize(string values)
        {
            Settings = string.IsNullOrWhiteSpace(values) ? new VoucherApprovalTileParameters() : Newtonsoft.Json.JsonConvert.DeserializeObject<VoucherApprovalTileParameters>(values) ?? new VoucherApprovalTileParameters();
        }

        DocumentAPI documentApi;
        CrudAPI Api;
        Task fetchTask;
        VoucherApprovalTileControl tileContent;
        TileDropState dropState;
        string dropStateTitle;
        string dropStateDescription;
        bool isImporting;
        int lastAddedVoucherRowId;

        public void Initialize(CrudAPI api)
        {
            if (api == null) throw new ArgumentNullException(nameof(api));
            Api = api;
            documentApi = new DocumentAPI(api);
        }

        public event EventHandler DropStateChanged;
        internal CrudAPI ApprovalApi { get { return Api; } }

        public System.Windows.Controls.UserControl GetContent()
        {
            if (tileContent == null)
            {
                tileContent = new VoucherApprovalTileControl();
                tileContent.SetTileType(this);
                tileContent.SetSummary(LastSummary);
                tileContent.SetDropState(DropState, DropStateTitle, DropStateDescription);
            }
            return tileContent;
        }

        public TileDropState DropState
        {
            get { return dropState; }
        }

        public string DropStateTitle
        {
            get { return dropStateTitle; }
        }

        public string DropStateDescription
        {
            get { return dropStateDescription; }
        }

        public VoucherInvoiceData LastExtractedInvoice { get; private set; }

        public VoucherApprovalTileData LastSummary { get; private set; }

        public string ErrorMessage { get; private set; }

        public Task FetchAsync()
        {
            if (fetchTask != null && !fetchTask.IsCompleted) return fetchTask;
            return fetchTask = FetchCoreAsync();
        }

        async Task FetchCoreAsync()
        {
            var vouchers = await Api.Query<VouchersClient>(new DocumentNoRef()) ?? new VouchersClient[0];
            var voucherType = Api.CompanyEntity.GetUserType(typeof(VouchersClient)) ?? typeof(VouchersClient);
            var approvalVouchers = (VouchersClient[])await documentApi.DocumentsForApproval((VouchersClient)Activator.CreateInstance(voucherType));
            var employees = await Api.LoadCache<EmployeeClient>();
            var currentApprovers = new HashSet<string>(employees?.Where(employee => employee.UserLoginId == Api.session.LoginId && !string.IsNullOrWhiteSpace(employee.Number))
                .Select(employee => employee.Number) ?? Enumerable.Empty<string>());

            // The service owns eligibility, including sequential/delegated approval rules.
            var needsApproval = (approvalVouchers ?? new VouchersClient[0])
                .OrderByDescending(x => x.RowId)
                .Select(CreateTileItem)
                .ToArray();

            var approvedByMe = vouchers.Where(x =>
                (currentApprovers.Contains(x._Approver1) && x._Approved) ||
                (currentApprovers.Contains(x._Approver2) && x._Approved2)).ToArray();

            var awaitingOthers = approvedByMe
                .Where(x => x._ViewInFolder != (byte)FolderType.Rejected &&
                    !string.IsNullOrWhiteSpace(x._Approver1) && !string.IsNullOrWhiteSpace(x._Approver2) &&
                    ((!x._Approved && !currentApprovers.Contains(x._Approver1)) ||
                     (!x._Approved2 && !currentApprovers.Contains(x._Approver2))))
                .OrderByDescending(x => CurrentUserApprovalDate(x, currentApprovers))
                .ThenByDescending(x => x.RowId)
                .Select(CreateTileItem)
                .ToArray();

            // Personal approval history can overlap Awaiting others until the second approval.
            // Missing legacy dates sort last; document/invoice dates are not approval dates.
            var approved = approvedByMe
                .OrderByDescending(x => CurrentUserApprovalDate(x, currentApprovers))
                .ThenByDescending(x => x.RowId)
                .Take(10)
                .Select(CreateTileItem)
                .ToArray();

            var summary = new VoucherApprovalTileData
            {
                NeedsApproval = needsApproval,
                AwaitingOthers = awaitingOthers,
                Approved = approved
            };

            LastSummary = summary;
            UpdateContent(content => content.SetSummary(summary));

        }

        void UpdateContent(Action<VoucherApprovalTileControl> update)
        {
            if (tileContent == null) return;
            if (tileContent.Dispatcher.CheckAccess()) update(tileContent);
            else tileContent.Dispatcher.Invoke(() => update(tileContent));
        }

        internal Task<ErrorCodes> ApproveAsync(VoucherApprovalTileItem item, string comment = null)
        {
            return item?.Voucher == null ? Task.FromResult(ErrorCodes.NoSucces) : documentApi.DocumentSetApprove(item.Voucher, comment);
        }

        internal Task<ErrorCodes> RejectAsync(VoucherApprovalTileItem item, string comment)
        {
            return item?.Voucher == null ? Task.FromResult(ErrorCodes.NoSucces) : documentApi.DocumentSetReject(item.Voucher, comment);
        }

        internal Task<ErrorCodes> ChangeApproverAsync(VoucherApprovalTileItem item, string approver, string comment)
        {
            return item?.Voucher == null ? Task.FromResult(ErrorCodes.NoSucces) : documentApi.ChangeApprover(item.Voucher, approver, comment);
        }

        static DateTime CurrentUserApprovalDate(VouchersClient voucher, HashSet<string> currentApprovers)
        {
            var first = voucher._Approved && currentApprovers.Contains(voucher._Approver1)
                ? voucher._Approved1Date : DateTime.MinValue;
            var second = voucher._Approved2 && currentApprovers.Contains(voucher._Approver2)
                ? voucher._Approved2Date : DateTime.MinValue;
            return first > second ? first : second;
        }

        static VoucherApprovalTileItem CreateTileItem(VouchersClient voucher)
            => new VoucherApprovalTileItem { RowId = voucher.RowId, Voucher = voucher };

        public bool CanAcceptFileDrop(string[] filePaths)
        {
            return !isImporting && filePaths != null && filePaths.Length > 0 && filePaths.All(File.Exists);
        }

        public void OnFileDragOver(string[] filePaths)
        {
            if (isImporting) return;
            SetDropState(CanAcceptFileDrop(filePaths) ? TileDropState.DragOver : TileDropState.InvalidDragOver,
                Uniconta.ClientTools.Localization.lookup("Vouchers"),
                Uniconta.ClientTools.Localization.lookup("TileDragYourVoucherAttachmentsHereToUpload"));
        }

        internal void OnOutlookDragOver()
        {
            if (!isImporting)
                SetDropState(TileDropState.DragOver, Uniconta.ClientTools.Localization.lookup("Vouchers"),
                    Uniconta.ClientTools.Localization.lookup("TileDragYourVoucherAttachmentsHereToUpload"));
        }

        internal bool CanAcceptOutlookDrop => !isImporting;

        public void OnFileDragLeave()
        {
            if (DropState == TileDropState.DragOver ||
                DropState == TileDropState.InvalidDragOver)
            {
                SetDropState(
                    TileDropState.Idle,
                    null,
                    null);
            }
        }

        internal async Task ViewLastExtractedVoucherAsync()
        {
            var rowId = lastAddedVoucherRowId;
            if (rowId == 0 || isImporting) return;
            try
            {
                // An in-flight refresh may have started before the voucher was saved.
                if (fetchTask != null && !fetchTask.IsCompleted) await fetchTask;
                await FetchAsync();
                if (isImporting || rowId != lastAddedVoucherRowId) return;
                ResetDropState();
                UpdateContent(content => content.ShowPendingVoucher(rowId));
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex.Message);
                SetError(Uniconta.ClientTools.Localization.lookup("Error"), ex.Message);
            }
        }

        internal void OpenVouchersPage()
        {
            UtilDisplay.GetDockingControl().AddDockItem(
                string.IsNullOrWhiteSpace(Settings.OpenTarget) ? Uniconta.ClientTools.UnicontaTabs.Vouchers : Settings.OpenTarget,
                null,
                null);
        }

        public Task OnFileDropAsync(string[] filePaths)
        {
            if (isImporting) return Task.CompletedTask;
            if (!CanAcceptFileDrop(filePaths))
            {
                SetError(Uniconta.ClientTools.Localization.lookup("Error"), Uniconta.ClientTools.Localization.lookup("FileNotFound"));
                return Task.CompletedTask;
            }
            return ImportAsync(add => ReadDroppedFiles(filePaths, add));
        }

        internal Task OnOutlookDropAsync(System.Windows.IDataObject data) => isImporting ? Task.CompletedTask : ImportAsync(add => ReadDroppedOutlookMails(data, add));

        // Match the voucher page drop readers without changing shared utilities.
        static string[] ReadDroppedFiles(string[] filePaths, Action<byte[], string, FileextensionsTypes> addContent)
        {
            var fileErrors = new List<string>();
            string currentFile = string.Empty;

            try
            {
                foreach (var file in filePaths)
                {
                    currentFile = System.IO.Path.GetFileName(file);
                    var fileBytes = UtilFunctions.GetFileBytes(file);
                    var text = System.IO.Path.GetFileNameWithoutExtension(file);
                    var ext = DocumentConvert.GetDocumentType(System.IO.Path.GetExtension(file));

                    if (fileBytes != null && fileBytes.Length <= TableAddOnData.MaxDocSize)
                        addContent(fileBytes, text, ext);
                    else
                        fileErrors.Add(string.Format("{0}: {1}", currentFile, Uniconta.ClientTools.Localization.lookup("MaxFileSize")));
                }
            }
            catch (Exception ex)
            {
                fileErrors.Add(string.Format("{0}: {1}", currentFile, ex.Message));
            }

            return fileErrors.ToArray();
        }

        static string[] ReadDroppedOutlookMails(System.Windows.IDataObject dataObject, Action<byte[], string, FileextensionsTypes> addContent)
        {
            var outlookErrors = new List<string>();
            string subject = string.Empty;

            try
            {
                var header = dataObject.GetData(System.Windows.DataFormats.UnicodeText)?.ToString();
                var outlookMailDataObject = new OutlookMailDataObject(dataObject);
                var filenames = (string[])outlookMailDataObject.GetData("FileGroupDescriptor");

                MemoryStream[] emailMemoryStream = (MemoryStream[])outlookMailDataObject.GetData("FileContents");

                for (int idx = 0; idx < filenames.Length; idx++)
                {
                    var fileExt = DocumentConvert.GetDocumentType(Path.GetExtension(filenames[idx]));
                    if (fileExt == FileextensionsTypes.MSG)
                        subject = UtilDisplay.GetMailSubject(header, filenames[idx]);
                    else
                    {
                        var sub = UtilDisplay.GetMailSubject((Stream)dataObject.GetData("FileGroupDescriptor"));
                        subject = idx == 0 && sub.Contains(filenames[idx]) ? sub : filenames[idx];
                    }

                    var binaryReader = new BinaryReader(emailMemoryStream[idx]);
                    var emailbytes = binaryReader.ReadBytes((int)emailMemoryStream[idx].Length);
                    if (emailbytes.Length <= TableAddOnData.MaxDocSize)
                        addContent(emailbytes, subject, fileExt);
                    else
                        outlookErrors.Add(string.Format("{0}: {1}", subject, Uniconta.ClientTools.Localization.lookup("MaxFileSize")));
                }
            }
            catch (Exception ex)
            {
                outlookErrors.Add(string.Format("{0}: {1}", subject, ex.Message));
            }

            return outlookErrors.ToArray();
        }

        static VouchersClient CreateDroppedVoucher(CrudAPI api, byte[] data, string text, FileextensionsTypes extension)
        {
            var voucher = new VouchersClient { _Data = data, _Text = text, _Fileextension = extension };
            ChildWindow.SetTablePropertiesValue(api, voucher, voucher.ClassId());
            return voucher;
        }

        async Task ImportAsync(Func<Action<byte[], string, FileextensionsTypes>, string[]> read)
        {
            isImporting = true;
            LastExtractedInvoice = null;
            lastAddedVoucherRowId = 0;
            ErrorMessage = null;
            SetDropState(TileDropState.Processing, Uniconta.ClientTools.Localization.lookup("Vouchers"),
                Uniconta.ClientTools.Localization.lookup("LoadingMsg"));
            var added = false;
            try
            {
                var vouchers = new List<VouchersClient>();
                var errors = new List<string>(read((bytes, text, ext) =>
                {
                    if (bytes?.Length > 0)
                        vouchers.Add(CreateDroppedVoucher(Api, bytes, text, ext));
                }));
                CreditorClient[] creditors = null;
                foreach (var voucher in vouchers)
                {
                    try
                    {
                        var invoice = InvoiceFromFileName(voucher._Text);
                        if (voucher._Fileextension == FileextensionsTypes.PDF)
                        {
                            // AI enriches a normal voucher import; unavailable AI must not prevent it.
                            try
                            {
                                if (GPTServiceFactory.IsGPTIntegrationEnabled())
                                {
                                    var context = new GPTConversationContextWithAttachment
                                    {
                                        File = new FileUploadData { FileData = voucher._Data, FileName = voucher._Text + ".pdf", ContentType = "application/pdf" }
                                    };
                                    InvoiceIdentificationPrompt.EnrichContext(context);
                                    var result = await Api.CompanyEntity.GetGPTService(Api).GetResponse<VoucherInvoiceIdentificationResult>(context);
                                    if (result?.IsInvoice == true && result.Invoice != null)
                                        invoice = result.Invoice;
                                }
                            }
                            catch (Exception ex) { Uniconta.ClientTools.Controls.AIErrorResolver.Handle(ex); }
                        }
                        ApplyInvoice(voucher, invoice);
                        if (!string.IsNullOrWhiteSpace(invoice.SupplierName) || !string.IsNullOrWhiteSpace(invoice.SupplierRegistrationNumber))
                        {
                            creditors = creditors ?? await Api.Query<CreditorClient>() ?? new CreditorClient[0];
                            var matches = creditors.Where(c => !string.IsNullOrWhiteSpace(invoice.SupplierRegistrationNumber)
                                ? string.Equals(c._LegalIdent, invoice.SupplierRegistrationNumber, StringComparison.OrdinalIgnoreCase)
                                : string.Equals(c._Name, invoice.SupplierName, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
                            if (matches.Length == 1) voucher._CreditorAccount = matches[0]._Account;
                        }
                        VouchersGrid.PrepareVoucherAttachment(voucher);
                        if (voucher._Data.Length > TableAddOnData.MaxDocSize)
                        {
                            errors.Add(voucher._Text + ": " + Uniconta.ClientTools.Localization.lookup("MaxFileSize"));
                            continue;
                        }
                        // Insert the attachment with its header so success means both have been saved.
                        var resultCode = await Api.Insert(voucher);
                        if (resultCode != ErrorCodes.Succes)
                        {
                            errors.Add(voucher._Text + ": " + resultCode);
                            continue;
                        }
                        added = true;
                        LastExtractedInvoice = invoice;
                        lastAddedVoucherRowId = voucher.RowId;
                    }
                    catch (Exception ex)
                    {
                        Uniconta.ClientTools.InfoLog.Log.Error(ex.Message);
                        errors.Add(voucher._Text + ": " + ex.Message);
                    }
                }
                if (errors.Count > 0 || !added)
                    SetError(Uniconta.ClientTools.Localization.lookup("Error"), errors.Count > 0 ? string.Join(Environment.NewLine, errors) : Uniconta.ClientTools.Localization.lookup("TileTheSelectedInvoiceIsEmpty"));
                else
                    SetDropState(TileDropState.Success, Uniconta.ClientTools.Localization.lookup("TileInvoiceAdded"),
                        Uniconta.ClientTools.Localization.lookup("Vouchers"));
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex.Message);
                SetError(Uniconta.ClientTools.Localization.lookup("Error"), ex.Message);
            }
            finally
            {
                isImporting = false;
                if (added)
                {
                    try
                    {
                        if (fetchTask != null && !fetchTask.IsCompleted) await fetchTask;
                        await FetchAsync();
                    }
                    catch (Exception ex) { Uniconta.ClientTools.InfoLog.Log.Error(ex.Message); }
                }
            }
        }

        internal static VoucherInvoiceData InvoiceFromFileName(string text)
        {
            // Only labeled values and unambiguous ISO dates are safe to infer.
            var invoice = new VoucherInvoiceData();
            var number = Regex.Match(text ?? "", @"(?:^|[ _])(?:invoice|inv|faktura)[ _#-]+([A-Za-z0-9]+(?:-[A-Za-z0-9]+)*)", RegexOptions.IgnoreCase);
            if (number.Success && !Regex.IsMatch(number.Groups[1].Value, @"^\d{4}-\d{2}-\d{2}$"))
                invoice.InvoiceNumber = number.Groups[1].Value;
            var date = Regex.Match(text ?? "", @"(?<!\d)\d{4}-\d{2}-\d{2}(?!\d)");
            if (date.Success && DateTime.TryParseExact(date.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                invoice.InvoiceDate = parsedDate;
            var amount = Regex.Match(text ?? "", @"(?:^|[ _])(?:total|amount)[ _=-]+(-?\d+(?:[.,]\d{1,2})?)(?=$|[ _])", RegexOptions.IgnoreCase);
            if (amount.Success && decimal.TryParse(amount.Groups[1].Value.Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var total))
                invoice.TotalAmount = total;
            foreach (Match currency in Regex.Matches(text ?? "", @"(?:^|[ _])([A-Z]{3})(?=$|[ _])"))
            {
                if (AppEnums.Currencies.TryIndexOf(currency.Groups[1].Value) < 0) continue;
                invoice.Currency = currency.Groups[1].Value;
                break;
            }
            return invoice;
        }

        internal static void ApplyInvoice(VouchersClient voucher, VoucherInvoiceData invoice)
        {
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber)) voucher._Invoice = invoice.InvoiceNumber;
            if (invoice.InvoiceDate.HasValue) voucher._DocumentDate = invoice.InvoiceDate.Value;
            if (invoice.DueDate.HasValue) voucher._DueDate = invoice.DueDate.Value;
            if (invoice.TotalAmount.HasValue) voucher._Amount = (double)invoice.TotalAmount.Value;
            if (!string.IsNullOrWhiteSpace(invoice.Currency) && AppEnums.Currencies.TryIndexOf(invoice.Currency) >= 0)
                voucher.Currency = invoice.Currency;
            if (!string.IsNullOrWhiteSpace(invoice.PaymentReference)) voucher._PaymentId = invoice.PaymentReference;
            if (invoice.VatAmount.HasValue) voucher._EnteredVatAmount = (double)invoice.VatAmount.Value;
        }

        public void ResetDropState()
        {
            if (isImporting) return;
            LastExtractedInvoice = null;
            lastAddedVoucherRowId = 0;
            ErrorMessage = null;

            SetDropState(
                TileDropState.Idle,
                null,
                null);
        }

        void SetError(string title, string description)
        {
            ErrorMessage = description;

            SetDropState(
                TileDropState.Error,
                title,
                description);
        }

        void SetDropState(
            TileDropState state,
            string title,
            string description)
        {
            dropState = state;
            dropStateTitle = title;
            dropStateDescription = description;
            UpdateContent(content => content.SetDropState(state, title, description));

            DropStateChanged?.Invoke(this, EventArgs.Empty);
        }

        [PromptDefinition(
            "Vouchers_IdentifyInvoice",
            "Extracts structured supplier invoice data from an attached PDF without creating a voucher.")]
        public static PromptDefinition InvoiceIdentificationPrompt =>
            PromptDefinition.FromMessages(
                new[]
                {
                    new PromptRoleNode
                    {
                        Role = MessageRole.Developer,
                        Content =
                            "Inspect the attached PDF and identify whether it is a supplier invoice. Extract only values supported by the document. " +
                            "Use ISO 4217 currency codes when a currency is shown. Preserve invoice, payment-reference and bank-account formatting. " +
                            "Amounts must be decimal numbers without currency symbols. Return IsInvoice false and a concise ErrorMessage when the document " +
                            "is unreadable, is not an invoice, or cannot be identified with sufficient confidence. Do not invent missing values."
                    },
                    new PromptRoleNode
                    {
                        Role = MessageRole.User,
                        Content =
                            "Analyze the attached PDF invoice and return the structured invoice header and line information."
                    }
                },
                true);
    }

    public sealed class VoucherApprovalTileParameters
    {
        [DisplayName("Open target")]
        [Description("The page to open from the voucher tile.")]
        [Category("Navigation")]
        [DefaultValue(Uniconta.ClientTools.UnicontaTabs.Vouchers)]
        [TypeConverter(typeof(TileNavigationTargetConverter))]
        public string OpenTarget { get; set; } = Uniconta.ClientTools.UnicontaTabs.Vouchers;
    }

    public sealed class VoucherApprovalTileData : UnicontaBaseEntity
    {
        public int NeedsApprovalCount => NeedsApproval?.Count ?? 0;
        public int AwaitingOthersCount => AwaitingOthers?.Count ?? 0;
        public int ApprovedCount => Approved?.Count ?? 0;

        public IReadOnlyList<VoucherApprovalTileItem> NeedsApproval { get; set; }
        public IReadOnlyList<VoucherApprovalTileItem> AwaitingOthers { get; set; }
        public IReadOnlyList<VoucherApprovalTileItem> Approved { get; set; }
        public int CompanyId { get; set; }

        public Type BaseEntityType() => typeof(VoucherApprovalTileData);
        public int ClassId() => -1;
        public void loadFields(CustomReader reader, int savedWithVersion) { }
        public void saveFields(CustomWriter writer, int saveVersion) { }
        public int Version(int clientApiVersion) => -1;
    }

    public sealed class VoucherApprovalTileItem
    {
        public int RowId { get; set; }

        public string Title { get; set; }
        public string DateText { get; set; }
        public string AmountText { get; set; }
        //public string StatusText { get; set; }

        private VouchersClient voucher;

        public VouchersClient Voucher
        {
            get => voucher;
            set
            {
                voucher = value;

                Title = voucher.GetVoucherAppprovalTileTitle();
                DateText = FormatDate(voucher.GetVoucherAppprovalTileSortDate());
                AmountText = FormatAmount(voucher);
            }
        }

        public string Approver1 { get => string.IsNullOrWhiteSpace(voucher.Approver1Name) ? voucher._Approver1 : voucher.Approver1Name; set { } }
        public string Approver2 { get => string.IsNullOrWhiteSpace(voucher.Approver2Name) ? voucher._Approver2 : voucher.Approver2Name; set { } }

        public bool Approver1Approved { get => voucher.Approved; set { } }
        public bool Approver2Approved { get => voucher.Approved2; set { } }

        // Rejection is document-wide. Attribute it only when exactly one assigned
        // approver remains unapproved; otherwise keep both neutral rather than guess.
        public bool Approver1Rejected => !voucher._Approved &&
            voucher._ViewInFolder == (byte)FolderType.Rejected &&
            (string.IsNullOrWhiteSpace(voucher._Approver2) || voucher._Approved2);
        public bool Approver2Rejected => !voucher._Approved2 &&
            voucher._ViewInFolder == (byte)FolderType.Rejected &&
            (string.IsNullOrWhiteSpace(voucher._Approver1) || voucher._Approved);

        public Visibility Approver1Visibility => string.IsNullOrWhiteSpace(Approver1) ? Visibility.Collapsed : Visibility.Visible;
        public Visibility Approver2Visibility => string.IsNullOrWhiteSpace(Approver2) ? Visibility.Collapsed : Visibility.Visible;

        private static string FormatDate(DateTime date)
        {
            if (date == DateTime.MinValue)
                return string.Empty;

            var today = DateTime.Today;

            if (date.Date == today)
                return string.Format(CultureInfo.CurrentCulture, Uniconta.ClientTools.Localization.lookup("TileToday0"), date.ToString("t", CultureInfo.CurrentCulture));

            if (date.Date == today.AddDays(-1))
                return string.Format(CultureInfo.CurrentCulture, Uniconta.ClientTools.Localization.lookup("TileYesterday0"), date.ToString("t", CultureInfo.CurrentCulture));

            if (date.Year == today.Year)
                return date.ToString("d MMM HH:mm", CultureInfo.CurrentCulture);

            return date.ToString("d MMM yyyy", CultureInfo.CurrentCulture);
        }

        private static string FormatAmount(VouchersClient voucher)
        {
            if (voucher == null)
                return string.Empty;

            if (voucher._Amount == 0)
                return string.Empty;

            return voucher._Amount.ToString(
                "N2",
                CultureInfo.CurrentCulture);
        }
    }

    public sealed class VoucherInvoiceIdentificationResult
    {
        public bool IsInvoice { get; set; }
        public string ErrorMessage { get; set; }
        public VoucherInvoiceData Invoice { get; set; }
    }

    public sealed class VoucherInvoiceData
    {
        public VoucherInvoiceData()
        {
            Lines = new List<VoucherInvoiceLineData>();
        }

        public string SupplierName { get; set; }
        public string SupplierRegistrationNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Currency { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string PaymentReference { get; set; }
        public string BankAccount { get; set; }
        public List<VoucherInvoiceLineData> Lines { get; set; }
    }

    public sealed class VoucherInvoiceLineData
    {
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? VatPercent { get; set; }
    }

    internal static class VouchersClientExtensions
    {
        internal static DateTime GetVoucherAppprovalTileSortDate(this VouchersClient myVoucherClient)
        {
            if (myVoucherClient == null)
                return DateTime.MinValue;

            if (myVoucherClient._Created != DateTime.MinValue)
                return myVoucherClient._Created;

            if (myVoucherClient._DocumentDate != DateTime.MinValue)
                return myVoucherClient._DocumentDate;

            if (myVoucherClient._PostingDate != DateTime.MinValue)
                return myVoucherClient._PostingDate;

            return DateTime.MinValue;
        }

        internal static string GetVoucherAppprovalTileTitle(this VouchersClient myVoucherClient)
        {
            if (myVoucherClient == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(myVoucherClient._Text))
                return myVoucherClient._Text;

            if (!string.IsNullOrWhiteSpace(myVoucherClient._Invoice))
                return myVoucherClient._Invoice;

            if (!string.IsNullOrWhiteSpace(myVoucherClient._CreditorAccount))
                return myVoucherClient._CreditorAccount;

            if (myVoucherClient.RowId != 0)
                return string.Format(CultureInfo.CurrentCulture, Uniconta.ClientTools.Localization.lookup("TileVoucher0"), myVoucherClient.RowId);

            return Uniconta.ClientTools.Localization.lookup("Voucher");
        }
    }
}
