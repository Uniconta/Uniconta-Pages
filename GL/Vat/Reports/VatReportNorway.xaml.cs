using DevExpress.Xpf.Core.ConditionalFormatting;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.Vat.Norway;
using UnicontaClient.Pages.GL.Vat.Norway.Helpers;
using UnicontaClient.Pages.GL.Vat.Norway.Models;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Feedback;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;
using ItemChoiceTypeValidation = UnicontaClient.Pages.GL.Vat.Norway.Models.Validation.ItemChoiceType;
using SubmissionsModels = UnicontaClient.Pages.GL.Vat.Norway.Models.Submission;
using ValidationModels = UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatReportNorwayGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(NorwayVATReportLine); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class VatReportNorway : GridBasePage
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public override string NameOfControl
        {
            get { return nameof(VatReportNorway); }
        }

        //How long Send waits for Skatteetaten's receipt before leaving it to the Feedback button
        private static readonly TimeSpan FeedbackWaitTime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan FeedbackPollInterval = TimeSpan.FromSeconds(5);

        private readonly List<NorwayVATReportLine> _vatReportLines;
        private readonly GLVatReportedClient _glVatReported;
        private MvaMeldingDto _lastValidationModel;

        //The VAT report keeps one reference (GLVatReported.Reference: instance id, prefixed for the test environment).
        //A test submission sent without saving only gets this in-page reference, so Feedback can still read it
        private string _unsavedTestReference;

        //Receipt and Skatteetaten's validation result from Feedback, with the reference they belong to
        private byte[] _lastReceipt;
        private Valideringsresultat _submittedValidationResult;
        private string _feedbackReference;

        private bool _isGLVATReportedUploaded;
        private bool _warningFormatAdded;

        private readonly NorwayVATReportHelpers _vatHelper;

        public VatReportNorway(GLVatReportedClient glVatReported, List<VatReportLine> vatReportLines, DateTime fromDate, DateTime toDate) : base(null)
        {
            _vatReportLines = NorwayVATReportMapper.ToReportLines(vatReportLines);
            _glVatReported = glVatReported;

            _isGLVATReportedUploaded = _glVatReported.UploadedAt != DateTime.MinValue;

            FromDate = fromDate;
            ToDate = toDate;

            _vatHelper = new NorwayVATReportHelpers();

            //ID-porten tokens are cached per process. They must not carry over to another Uniconta user or company
            NorwayVATReportHelpers.SetTokenOwner($"{api.session.Uid}|{api.CompanyEntity.CompanyId}");

            InitializeComponent();

            DataContext = this;

            localMenu.dataGrid = dgVatReportNorway;
            SetRibbonControl(localMenu, dgVatReportNorway);
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            dgVatReportNorway.api = api;
            dgVatReportNorway.BusyIndicator = busyIndicator;
            dgVatReportNorway.ItemsSource = _vatReportLines;
            dgVatReportNorway.Visibility = Visibility.Visible;
            dgVatReportNorway.ShowTotalSummary();

            cmbSpecification.ItemsSource = NorwayVATReportConsts.SpecificationsList;
            cmbDeclarationType.ItemsSource = NorwayVATReportConsts.DeclarationTypeList;
            cmbCommentType.ItemsSource = NorwayVATReportConsts.CommentsOptionsList;
            cmbCommentOption.ItemsSource = NorwayVATReportConsts.PredefinedVATReportCommentsList;
            cmbSubmissionType.ItemsSource = NorwayVATReportConsts.SubmissionTypeList;

            txtFromDate.DateTime = FromDate;
            txtToDate.DateTime = ToDate;
            cmbDeclarationType.EditValue = ValidationModels.Meldingskategori.alminnelig.ToString();
            cmbSubmissionType.EditValue = SubmissionsModels.Innsendingstype.komplett.ToString();

            //Show the rate and sign checks right away, before anything is sent
            ApplyLineChecks();

            //Send is disabled to start with because validation has to pass
            localMenu.DisableButtons("Send");

            //Any change that affects the payload invalidates the last validation
            cmbDeclarationType.EditValueChanged += (s, e) => { ApplyLineChecks(); InvalidateValidation(); };
            cmbSubmissionType.EditValueChanged += (s, e) => InvalidateValidation();
            cmbCommentOption.EditValueChanged += (s, e) => InvalidateValidation();
            txtComment.EditValueChanged += (s, e) => InvalidateValidation();
            dgVatReportNorway.Loaded += dgVatReportNorway_Loaded;

            //Feedback needs an Altinn instance. It exists after a completed submission, or as a draft after a submission that failed half way
            if (!HasReference)
                localMenu.DisableButtons("Feedback");

            UpdateSendCaption();

            //Skatteetaten's test environment is preselected only in Uniconta's test environments. In a live Uniconta environment it starts off
            if (api.session.Connection.Target != Uniconta.API.Service.APITarget.Live)
                ceTest.IsChecked = true;

            UpdateEnvironmentLabel();
        }

        private bool IsTest { get { return ceTest.IsChecked == true; } }

        private bool HasReference
        {
            get
            {
                return NorwayVATReportHelpers.TryParseReference(_glVatReported.Reference, out _, out _) ||
                       NorwayVATReportHelpers.TryParseReference(_unsavedTestReference, out _, out _);
            }
        }

        /// <summary>
        /// The reference stored on the VAT report was created in the given environment. Without a reference, production is assumed
        /// (as for a report uploaded before references were kept)
        /// </summary>
        private bool ReferenceIsFrom(bool test)
        {
            return NorwayVATReportHelpers.TryParseReference(_glVatReported.Reference, out _, out var referenceTest) ? referenceTest == test : !test;
        }

        /// <summary>
        /// The VAT report is marked as uploaded by a submission in the given environment
        /// </summary>
        private bool UploadedIn(bool test)
        {
            return _isGLVATReportedUploaded && ReferenceIsFrom(test);
        }

        /// <summary>
        /// The reference of the last submission in the given environment, or null. stored says whether it is the one on the VAT report
        /// </summary>
        private string ReferenceFor(bool test, out bool stored)
        {
            stored = NorwayVATReportHelpers.TryParseReference(_glVatReported.Reference, out _, out var storedTest) && storedTest == test;
            if (stored)
                return _glVatReported.Reference;

            return test && NorwayVATReportHelpers.TryParseReference(_unsavedTestReference, out _, out _) ? _unsavedTestReference : null;
        }

        /// <summary>
        /// A test submission may be saved on the VAT report unless that would replace the production submission's reference
        /// (needed for its receipt, Feedback and the duplicate check) or the production upload
        /// </summary>
        private bool CanSaveTestSubmission
        {
            get
            {
                if (UploadedIn(false))
                    return false;
                return !NorwayVATReportHelpers.TryParseReference(_glVatReported.Reference, out _, out var storedTest) || storedTest;
            }
        }

        private string Category { get { return cmbDeclarationType.EditValue?.ToString(); } }

        private string PeriodText { get { return $"{FromDate.ToShortDateString()} - {ToDate.ToShortDateString()}"; } }

        /// <summary>
        /// Drops the validated model so that Send requires a new validation
        /// </summary>
        private void InvalidateValidation()
        {
            _lastValidationModel = null;
            localMenu.DisableButtons("Send");
        }

        /// <summary>
        /// Runs the rate and opposite sign checks and refreshes the grid
        /// </summary>
        private void ApplyLineChecks()
        {
            NorwayVATReportMapper.ApplyChecks(_vatReportLines, FromDate, Category);
            dgVatReportNorway.RefreshData();
        }

        /// <summary>
        /// Busy state. The environment cannot change while a call to Skatteetaten or Altinn runs, nor anywhere in a ribbon operation
        /// (Send saves and polls for the receipt after its busy part)
        /// </summary>
        private void SetBusy(bool busy, string busyText = null)
        {
            busyIndicator.BusyContent = busyText ?? Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            busyIndicator.IsBusy = busy;
            ceTest.IsEnabled = !busy && !_operationRunning;
        }

        private void UpdateEnvironmentLabel()
        {
            txtEnvironment.Text = EnvironmentText(IsTest);
            txtEnvironment.Foreground = IsTest ? System.Windows.Media.Brushes.SeaGreen : System.Windows.Media.Brushes.Firebrick;
        }

        static string EnvironmentText(bool test)
        {
            return test ? Uniconta.ClientTools.Localization.lookup("NorwayVATEnvironmentTest") : Uniconta.ClientTools.Localization.lookup("Production").ToUpper();
        }

        private void dgVatReportNorway_Loaded(object sender, RoutedEventArgs e)
        {
            if (dgVatReportNorway.View is TableView tableView)
            {
                tableView.CellValueChanged -= dgVatReportNorway_CellValueChanged;
                tableView.CellValueChanged += dgVatReportNorway_CellValueChanged;
                tableView.ShownEditor -= dgVatReportNorway_ShownEditor;
                tableView.ShownEditor += dgVatReportNorway_ShownEditor;

                //Lines with a rate warning or an opposite sign are highlighted
                if (!_warningFormatAdded)
                {
                    _warningFormatAdded = true;
                    tableView.FormatConditions.Add(new FormatCondition()
                    {
                        FieldName = nameof(NorwayVATReportLine.Check),
                        Expression = "[HasWarning]",
                        ApplyToRow = true,
                        Format = new Format() { Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 228, 225)) }
                    });
                }
            }
        }

        private void dgVatReportNorway_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            //The specification changes which sign Skatteetaten expects and which remarks are valid
            if (e.Column.FieldName == nameof(NorwayVATReportLine.Specification))
                ApplyLineChecks();

            InvalidateValidation();
        }

        /// <summary>
        /// The remark list depends on the line's code, side, specification and sign, so it is filled per row when the editor opens
        /// </summary>
        private void dgVatReportNorway_ShownEditor(object sender, EditorEventArgs e)
        {
            if (e.Column.FieldName != nameof(NorwayVATReportLine.PredefinedComment) || !(e.Editor is ComboBoxEdit comboBox))
                return;

            var line = dgVatReportNorway.GetRow(e.RowHandle) as NorwayVATReportLine;
            if (line == null)
                return;

            var remarks = NorwayVATRemarkCatalog.ValidRemarks(line, Category);
            remarks.Insert(0, String.Empty);
            comboBox.ItemsSource = remarks;
        }

        //Ribbon actions that talk to Skatteetaten, Altinn or the journal. Only one may run at a time
        private static readonly HashSet<string> _exclusiveActions = new HashSet<string>() { "Validate", "Send", "Feedback", "TransferToJournal", "SignOut" };
        private bool _operationRunning;

        private async void localMenu_OnItemClicked(string ActionType)
        {
            if (_exclusiveActions.Contains(ActionType))
            {
                //A click while Send is still waiting for the receipt (or any other call) is ignored, so two calls never overlap
                if (_operationRunning)
                    return;

                _operationRunning = true;
                ceTest.IsEnabled = false;
            }

            try
            {
                switch (ActionType)
                {
                    case "Validate":
                        await Validate();
                        break;
                    case "Send":
                        await Send();
                        break;
                    case "Feedback":
                        await GetFeedback(false);
                        break;
                    case "TransferToJournal":
                        await TransferToJournal();
                        break;
                    case "SignOut":
                        NorwayVATReportHelpers.SignOut(IsTest);
                        txtSystemInfo.Text = Uniconta.ClientTools.Localization.lookup("NorwayVATSignedOut");
                        break;
                    default:
                        gridRibbon_BaseActions(ActionType);
                        break;
                }
            }
            catch (Exception ex)
            {
                SetBusy(false);

                //The Altinn instance may exist (a draft, or even a delivered return): Feedback can read it and the next Send checks it first
                if (HasReference)
                    localMenu.EnableButtons("Feedback");
                if (ActionType == "Send")
                    InvalidateValidation();

                UnicontaMessageBox.Show(ex);
            }
            finally
            {
                if (_exclusiveActions.Contains(ActionType))
                {
                    _operationRunning = false;
                    ceTest.IsEnabled = !busyIndicator.IsBusy;
                }
            }
        }

        #region Validate

        private async Task Validate()
        {
            if (_vatReportLines == null || !_vatReportLines.Any())
                return;

            txtSystemInfo.Text = String.Empty;
            txtRequestId.Text = String.Empty;

            //Period comes from the dates. The company's VAT period setting only gives a warning
            Enum period;
            try
            {
                period = NorwayVATReportMapper.CalculatePeriod(FromDate, ToDate, true);
            }
            catch (InvalidOperationException ex)
            {
                txtSystemInfo.Text = ex.Message;
                return;
            }

            var periodWarning = NorwayVATReportMapper.CheckCompanyPeriod(FromDate, ToDate, api.CompanyEntity._VatPeriod);
            if (periodWarning != null &&
                UnicontaMessageBox.Show($"{periodWarning}{Environment.NewLine}{Environment.NewLine}{Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue")}",
                                        Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            var organisationNumber = NorwayVATReportMapper.ToOrganisationNumber(api.CompanyEntity._Id, api.CompanyEntity._VatNumber);
            if (organisationNumber.Length != 9)
            {
                txtSystemInfo.Text = Uniconta.ClientTools.Localization.lookup("NorwayVATInvalidOrganisationNumber");
                return;
            }

            //Line checks: missing SAF-T code, remarks that Skatteetaten requires or rejects
            ApplyLineChecks();

            var lineErrors = new List<string>();
            var mvaSpecifications = NorwayVATReportMapper.ToSpecificationLines(_vatReportLines, lineErrors);
            lineErrors.AddRange(NorwayVATReportMapper.CheckRemarks(_vatReportLines, Category));

            if (lineErrors.Any())
            {
                txtSystemInfo.Text = string.Join(Environment.NewLine, lineErrors);
                return;
            }

            //Set request ID
            var requestId = Guid.NewGuid().ToString();
            txtRequestId.Text = requestId;

            //Set declaration comment
            var merknad = NorwayVATReportMapper.ToDeclarationComment(cmbCommentType.EditValue?.ToString(),
                                                                     cmbCommentOption.EditValue?.ToString(),
                                                                     txtComment.Text);
            //Create VAT declaration
            var mva = new MvaMeldingDto()
            {
                //Payment info
                betalingsinformasjon = new Betalingsinformasjon(),
                //System info
                innsending = new Innsending()
                {
                    regnskapssystem = new Kildesystem()
                    {
                        systemnavn = "Uniconta",
                        systemversjon = api.session.ServerVersion.ToString()
                    },
                    //Submission reference from the system
                    regnskapssystemsreferanse = requestId
                },
                merknad = merknad,
                meldingskategori = (ValidationModels.Meldingskategori)Enum.Parse(typeof(ValidationModels.Meldingskategori), Category),
                skattegrunnlagOgBeregnetSkatt = new SkattegrunnlagOgBeregnetSkatt()
                {
                    fastsattMerverdiavgift = mvaSpecifications.Sum(x => x.merverdiavgift),
                    mvaSpesifikasjonslinje = mvaSpecifications.ToArray(),
                    skattleggingsperiode = new ValidationModels.Skattleggingsperiode()
                    {
                        aar = FromDate.Year.ToString(),
                        periode = new ValidationModels.PeriodeBeskrivelse() { Item = period }
                    }
                },
                skattepliktig = new ValidationModels.Skattepliktig() { organisasjonsnummer = organisationNumber }
            };

            SetBusy(true);
            var validationResponse = await _vatHelper.ValidateAsync(mva, IsTest);
            SetBusy(false);

            if (!validationResponse.IsSuccess)
            {
                txtSystemInfo.Text = $"{Uniconta.ClientTools.Localization.lookup("Error")}: {validationResponse.Error}";
                return;
            }

            //avvikVedMeldingslevering: ingen avvik, avvikende, mangelfull or ugyldig skattemelding. Only ugyldig blocks submission
            var validationResult = validationResponse.Result;
            txtSystemInfo.Text = NorwayVATReportMapper.BuildValidationText(validationResult);

            if (validationResult.avvikVedMeldingslevering == Avvikstype.ugyldigskattemelding)
                return;

            //A configured rate that does not fit the SAF-T code means a wrong mapping in Uniconta. Validation may still show it, but it cannot be sent
            var rateWarnings = _vatReportLines.Where(x => !String.IsNullOrEmpty(x.RateWarning)).Select(x => x.RateWarning).ToList();
            if (rateWarnings.Any())
            {
                txtSystemInfo.Text += Environment.NewLine + Uniconta.ClientTools.Localization.lookup("NorwayVATRateBlocksSend") +
                                      Environment.NewLine + string.Join(Environment.NewLine, rateWarnings);
                return;
            }

            //An effective rate (VAT / basis) off the code list rate can be legitimate, e.g. a manual VAT correction. Shown here and confirmed on Send
            var effectiveRateWarnings = EffectiveRateWarnings();
            if (effectiveRateWarnings.Any())
                txtSystemInfo.Text += Environment.NewLine + Uniconta.ClientTools.Localization.lookup("NorwayVATEffectiveRateCheck") +
                                      Environment.NewLine + string.Join(Environment.NewLine, effectiveRateWarnings);

            _lastValidationModel = mva;
            localMenu.EnableButtons("Send");
        }

        private List<string> EffectiveRateWarnings()
        {
            return _vatReportLines.Where(x => !String.IsNullOrEmpty(x.EffectiveRateWarning)).Select(x => x.EffectiveRateWarning).ToList();
        }

        #endregion

        #region Send

        private async Task Send()
        {
            if (_lastValidationModel == null)
                return;

            //The environment is fixed for the whole operation (the checkbox is disabled while it runs)
            var test = IsTest;

            txtSystemInfo.Text = String.Empty;

            //Find out what happened to an earlier submission for this period before a new instance is created
            var previous = await CheckPreviousSubmission(test);
            if (!previous.Proceed)
                return;

            //Production always saves. A test submission saves only when the user says so
            var persist = ConfirmSend(test, previous.DeliveredAt);
            if (persist == null)
                return;

            var organisationNumber = _lastValidationModel.skattepliktig.organisasjonsnummer;
            var createdBy = api.session.User._Name;

            //Collect data for submission
            var instanceCreationRequest = new SubmissionsModels.InstanceCreationRequest()
            {
                InstanceOwner = new SubmissionsModels.InstanceCreationRequestInstanceOwner() { OrganisationNumber = organisationNumber }
            };

            var mvaSending = new SubmissionsModels.MvaMeldingInnsending()
            {
                meldingskategori = (SubmissionsModels.Meldingskategori)Enum.Parse(typeof(SubmissionsModels.Meldingskategori), Category),
                innsendingstype = (SubmissionsModels.Innsendingstype)Enum.Parse(typeof(SubmissionsModels.Innsendingstype), cmbSubmissionType.EditValue?.ToString()),
                innsendingstypeSpecified = true,
                opprettetAv = createdBy,
                norskIdentifikator = new SubmissionsModels.Identifikator()
                {
                    ItemElementName = SubmissionsModels.ItemChoiceType.organisasjonsnummer,
                    Item = organisationNumber
                },
                skattleggingsperiode = new SubmissionsModels.Skattleggingsperiode()
                {
                    aar = _lastValidationModel.skattegrunnlagOgBeregnetSkatt.skattleggingsperiode.aar,
                    periode = new SubmissionsModels.PeriodeBeskrivelse()
                    {
                        Item = NorwayVATReportMapper.CalculatePeriod(FromDate, ToDate, false)
                    }
                },
                //Not required by the XSD (minOccurs 0), but Skatteetaten's example and accepted submissions describe the attached return
                vedlegg = new[]
                {
                    new SubmissionsModels.Vedlegg()
                    {
                        vedleggstype = SubmissionsModels.Vedleggstype.mvamelding,
                        kildegruppe = SubmissionsModels.Kildegruppe.sluttbrukersystem,
                        opprettetAv = createdBy,
                        vedleggsfil = new SubmissionsModels.Vedleggsfil()
                        {
                            filnavn = NorwayVATReportConsts.VatReturnFileName,
                            filekstensjon = "xml",
                            filinnhold = "mva-melding"
                        }
                    }
                }
            };

            var validatedModel = _lastValidationModel;

            //Perform multi-step submission. Only the instance id is persisted as soon as the instance exists,
            //so a failure in a later step leaves a draft that Feedback and the next Send can still find. The report counts as uploaded only after the last step
            SetBusy(true, Uniconta.ClientTools.Localization.lookup("SendingWait"));
            var submissionResponse = await _vatHelper.SubmitAsync(instanceCreationRequest, mvaSending, validatedModel, test,
                async createdInstance =>
                {
                    //Id has the form "{partyId}/{guid}"
                    var reference = NorwayVATReportHelpers.ToStoredReference(createdInstance.Id, test);
                    if (!persist.Value)
                    {
                        _unsavedTestReference = reference;
                        return null;
                    }

                    //Without the stored id a later failure could not be traced, so the submission stops here (the instance stays an unsubmitted draft)
                    var err = await StoreReference(reference);
                    if (err != ErrorCodes.Succes)
                        return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATReferenceNotSaved"), err);

                    if (test)
                        _unsavedTestReference = null;
                    return null;
                });
            SetBusy(false);

            if (!submissionResponse.IsSuccess)
            {
                txtSystemInfo.Text = $"{Uniconta.ClientTools.Localization.lookup("Error")}: {submissionResponse.Error}";

                //The instance may exist in Altinn: Feedback can inspect it and the next Send checks it first. Send needs a new validation
                if (HasReference)
                    localMenu.EnableButtons("Feedback");
                InvalidateValidation();

                return;
            }

            var info = new StringBuilder(Uniconta.ClientTools.Localization.lookup("NorwayVATSuccess"));
            var instanceId = submissionResponse.Result.Id;

            if (persist.Value)
            {
                var saveError = await MarkUploaded(DateTime.Now);
                if (saveError != null)
                    info.AppendLine().Append(saveError);

                //Keep what was sent with the VAT report
                await SaveAttachment(Encoding.UTF8.GetBytes(NorwayVATReportHelpers.SerializeToXml(validatedModel)), FileextensionsTypes.XML,
                                     AttachmentText("NorwayVATAttachmentReturn", instanceId));
                await SaveAttachment(Encoding.UTF8.GetBytes(NorwayVATReportHelpers.SerializeToXml(mvaSending)), FileextensionsTypes.XML,
                                     AttachmentText("NorwayVATAttachmentEnvelope", instanceId));
            }

            //Show result and change ribbon button status
            txtSystemInfo.Text = info.ToString();

            UpdateSendCaption();

            InvalidateValidation();
            localMenu.EnableButtons(new[] { "Validate", "Feedback", "TransferToJournal" });

            //Wait for Skatteetaten's receipt; if it takes longer, the Feedback button fetches it later
            await GetFeedback(true);
        }

        /// <summary>
        /// Stores the upload time on the VAT report. Returns an error text with the reference when the VAT report could not be updated, otherwise null
        /// </summary>
        private async Task<string> MarkUploaded(DateTime uploadedAt)
        {
            var previousUploadedAt = _glVatReported.UploadedAt;
            _glVatReported.UploadedAt = uploadedAt;

            var err = await api.Update(_glVatReported);
            if (err == ErrorCodes.Succes)
            {
                _isGLVATReportedUploaded = true;
                return null;
            }

            _glVatReported.UploadedAt = previousUploadedAt;
            return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATUploadNotSaved"), err, _glVatReported.Reference);
        }

        /// <summary>
        /// Stores a reference (null: none) on the VAT report. The upload mark belongs to the environment of the stored reference, and without
        /// a reference the VAT report counts as uploaded in production. So when a saved test submission is replaced by a production submission
        /// or removed, its upload mark goes with it. Nothing changes in memory when the VAT report cannot be updated
        /// </summary>
        private async Task<ErrorCodes> StoreReference(string reference)
        {
            var newIsTest = NorwayVATReportHelpers.TryParseReference(reference, out _, out var referenceTest) && referenceTest;
            var clearUpload = UploadedIn(true) && !newIsTest;

            var previousReference = _glVatReported.Reference;
            var previousUploadedAt = _glVatReported.UploadedAt;
            _glVatReported.Reference = reference;
            if (clearUpload)
                _glVatReported.UploadedAt = DateTime.MinValue;

            var err = await api.Update(_glVatReported);
            if (err != ErrorCodes.Succes)
            {
                _glVatReported.Reference = previousReference;
                _glVatReported.UploadedAt = previousUploadedAt;
                return err;
            }

            if (clearUpload)
            {
                _isGLVATReportedUploaded = false;
                UpdateSendCaption();
            }
            return err;
        }

        /// <summary>
        /// Forgets a reference whose Altinn instance no longer exists. Returns an error text when the VAT report could not be updated, otherwise null
        /// </summary>
        private async Task<string> ClearReference(bool stored)
        {
            if (stored)
            {
                var err = await StoreReference(null);
                if (err != ErrorCodes.Succes)
                    return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATReferenceNotCleared"), err);
            }
            else
                _unsavedTestReference = null;

            if (!HasReference)
                localMenu.DisableButtons("Feedback");
            return null;
        }

        /// <summary>
        /// Before a new instance is created, the instance of the last submission in the same environment is read from Altinn:
        /// - processed by Skatteetaten: the return was delivered, also when the earlier Send was cut off before it could record that.
        ///   The VAT report is marked as uploaded, so the confirmation presents the new submission as a replacement
        /// - submitted and still being processed: no new submission until Skatteetaten is done (use Feedback)
        /// - an unsubmitted draft from a failed Send: it is deleted, so it is not left behind in Altinn
        /// When the instance cannot be read (other than "not found"), nothing is sent, so a delivered return is never sent twice by accident
        /// </summary>
        private async Task<(bool Proceed, DateTime? DeliveredAt)> CheckPreviousSubmission(bool test)
        {
            var reference = ReferenceFor(test, out var stored);
            if (!NorwayVATReportHelpers.TryParseReference(reference, out var instanceId, out _))
                return (true, null);

            SetBusy(true);
            var instanceResponse = await _vatHelper.GetInstanceAsync(instanceId, test);
            SetBusy(false);

            if (!instanceResponse.IsSuccess)
            {
                if (instanceResponse.StatusCode == System.Net.HttpStatusCode.NotFound || instanceResponse.StatusCode == System.Net.HttpStatusCode.Gone)
                    return await ProceedAfterClearing(stored, null);

                txtSystemInfo.Text = string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATCannotCheckPrevious"), instanceResponse.Error);
                return (false, null);
            }

            var instance = instanceResponse.Result;
            switch (NorwayVATReportHelpers.ClassifyInstance(instance))
            {
                case NorwayVATInstanceState.Gone:
                    return await ProceedAfterClearing(stored, null);

                case NorwayVATInstanceState.Delivered:
                {
                    var deliveredAt = instance.Process.Ended.Value.ToLocalTime();
                    if (stored && !_isGLVATReportedUploaded)
                    {
                        var saveError = await MarkUploaded(deliveredAt);
                        UpdateSendCaption();
                        txtSystemInfo.Text = string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATPreviousSubmissionDelivered"), deliveredAt) +
                                             (saveError != null ? Environment.NewLine + saveError : String.Empty);
                    }
                    return (true, deliveredAt);
                }

                case NorwayVATInstanceState.Processing:
                    localMenu.EnableButtons("Feedback");
                    txtSystemInfo.Text = Uniconta.ClientTools.Localization.lookup("NorwayVATPreviousSubmissionProcessing");
                    return (false, null);

                case NorwayVATInstanceState.Draft:
                {
                    SetBusy(true);
                    var deleteResponse = await _vatHelper.DeleteInstanceAsync(instanceId, test);
                    SetBusy(false);

                    //A draft that cannot be deleted is harmless: Skatteetaten never processes it
                    if (deleteResponse.IsSuccess)
                        return await ProceedAfterClearing(stored, Uniconta.ClientTools.Localization.lookup("NorwayVATDraftDeleted"));

                    txtSystemInfo.Text = string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATDraftNotDeleted"), deleteResponse.Error);
                    return (true, null);
                }

                default:
                    //Unknown state: do not risk a second return
                    txtSystemInfo.Text = string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATCannotCheckPrevious"),
                                                       instance?.Process?.CurrentTask?.AltinnTaskType ?? instance?.Process?.CurrentTask?.Name ?? "?");
                    return (false, null);
            }
        }

        /// <summary>
        /// The earlier instance is gone: its reference is removed and Send continues. When the VAT report cannot be updated nothing is sent,
        /// because the next Send would have to store a reference on it as well
        /// </summary>
        private async Task<(bool Proceed, DateTime? DeliveredAt)> ProceedAfterClearing(bool stored, string info)
        {
            var clearError = await ClearReference(stored);
            if (clearError != null)
            {
                txtSystemInfo.Text = info != null ? info + Environment.NewLine + clearError : clearError;
                return (false, null);
            }

            if (info != null)
                txtSystemInfo.Text = info;
            return (true, null);
        }

        /// <summary>
        /// Asks before sending: environment, organisation, period, category and amount, lines with an unusual effective rate, and whether
        /// the submission replaces an earlier one. Returns whether to save the submission on the VAT report, or null to cancel.
        /// Production always saves. In the test environment Yes sends and saves, No sends without saving
        /// </summary>
        private bool? ConfirmSend(bool test, DateTime? previousDeliveredAt)
        {
            var total = _lastValidationModel.skattegrunnlagOgBeregnetSkatt.fastsattMerverdiavgift;
            var text = new StringBuilder();
            text.AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATConfirmSend"),
                                          Environment.NewLine,
                                          EnvironmentText(test),
                                          _lastValidationModel.skattepliktig.organisasjonsnummer,
                                          PeriodText,
                                          NorwayVATReportConsts.DeclarationTypeList.TryGetValue(Category ?? "", out var categoryName) ? categoryName : Category,
                                          Math.Abs(total).ToString("N0", CultureInfo.CurrentCulture),
                                          Uniconta.ClientTools.Localization.lookup(total >= 0 ? "NorwayVATToPay" : "NorwayVATToReceive")));

            //Only a return in the same environment is replaced
            var replacedAt = previousDeliveredAt ?? (UploadedIn(test) ? _glVatReported.UploadedAt : (DateTime?)null);
            if (replacedAt.HasValue)
            {
                text.AppendLine();
                text.AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATConfirmResend"), replacedAt.Value));
            }

            var effectiveRateWarnings = EffectiveRateWarnings();
            if (effectiveRateWarnings.Any())
            {
                text.AppendLine();
                text.AppendLine(Uniconta.ClientTools.Localization.lookup("NorwayVATEffectiveRateCheck"));
                foreach (var warning in effectiveRateWarnings)
                    text.AppendLine(warning);
            }

            if (!test)
                return UnicontaMessageBox.Show(text.ToString(), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel,
                                               MessageBoxImage.Warning) == MessageBoxResult.OK ? true : (bool?)null;

            text.AppendLine();

            //The VAT report holds the production submission: a test submission may be sent, but is never saved over it
            if (!CanSaveTestSubmission)
            {
                text.AppendLine(Uniconta.ClientTools.Localization.lookup("NorwayVATTestNotSaved"));
                return UnicontaMessageBox.Show(text.ToString(), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel,
                                               MessageBoxImage.Question) == MessageBoxResult.OK ? false : (bool?)null;
            }

            text.AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATConfirmTestSave"), Environment.NewLine));

            var answer = UnicontaMessageBox.Show(text.ToString(), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNoCancel,
                                                 MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes)
                return true;
            if (answer == MessageBoxResult.No)
                return false;
            return null;
        }

        #endregion

        #region Feedback and attachments

        /// <summary>
        /// Reads the submission's status from Altinn. When processing is done, the receipt, validation result and payment information
        /// are stored as attachments on the VAT report. waitForCompletion polls for up to two minutes right after sending
        /// </summary>
        private async Task GetFeedback(bool waitForCompletion)
        {
            //The submission of the selected environment, else the one on the VAT report. It is read in the environment it was created in
            var reference = ReferenceFor(IsTest, out var stored) ?? ReferenceFor(!IsTest, out stored);
            if (!NorwayVATReportHelpers.TryParseReference(reference, out var instanceId, out var referenceTest))
                return;

            SetBusy(true, Uniconta.ClientTools.Localization.lookup("NorwayVATWaitingForFeedback"));

            NorwayVATAPIResponse<NorwayVATReportFeedback> feedbackResponse;
            var deadline = DateTime.Now + FeedbackWaitTime;
            while (true)
            {
                feedbackResponse = await _vatHelper.GetFeedbackAsync(instanceId, referenceTest);
                if (!waitForCompletion || !feedbackResponse.IsSuccess || feedbackResponse.Result.IsCompleted || DateTime.Now >= deadline)
                    break;

                await Task.Delay(FeedbackPollInterval);
            }

            SetBusy(false);

            if (!feedbackResponse.IsSuccess)
            {
                txtSystemInfo.Text = $"{Uniconta.ClientTools.Localization.lookup("Error")}: {feedbackResponse.Error}";
                return;
            }

            var feedback = feedbackResponse.Result;

            var feedbackSB = new StringBuilder();
            if (waitForCompletion)
                feedbackSB.AppendLine(Uniconta.ClientTools.Localization.lookup("NorwayVATSuccess"));

            feedbackSB.AppendLine(feedback.IsCompleted
                ? Uniconta.ClientTools.Localization.lookup("NorwayVATProcessed")
                : string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATStillProcessing"), feedback.CurrentTask));

            //KID, account, amount and due date. Shown raw until the schema is confirmed
            if (!String.IsNullOrEmpty(feedback.PaymentInformationXml))
                feedbackSB.AppendLine(feedback.PaymentInformationXml);

            if (feedback.ValidationResult != null)
            {
                feedbackSB.AppendLine();
                feedbackSB.Append(NorwayVATReportMapper.BuildValidationText(feedback.ValidationResult));
            }

            if (feedback.IsCompleted)
            {
                //Documents and the upload time go on the VAT report only for a saved submission (always in production, in test when chosen)
                if (stored)
                {
                    _submittedValidationResult = feedback.ValidationResult;
                    _feedbackReference = reference;

                    //Processed, but the Send that created it did not get to record it (timeout, crash)
                    if (!_isGLVATReportedUploaded)
                    {
                        var saveError = await MarkUploaded(feedback.Ended?.ToLocalTime() ?? DateTime.Now);
                        UpdateSendCaption();
                        if (saveError != null)
                            feedbackSB.AppendLine(saveError);
                    }

                    var saved = true;
                    if (feedback.ReceiptPDFData != null)
                        saved &= await SaveAttachment(feedback.ReceiptPDFData, FileextensionsTypes.PDF, AttachmentText("NorwayVATAttachmentReceipt", reference));
                    if (!String.IsNullOrEmpty(feedback.ValidationResultXml))
                        saved &= await SaveAttachment(Encoding.UTF8.GetBytes(feedback.ValidationResultXml), FileextensionsTypes.XML, AttachmentText("NorwayVATAttachmentValidation", reference));
                    if (!String.IsNullOrEmpty(feedback.PaymentInformationXml))
                        saved &= await SaveAttachment(Encoding.UTF8.GetBytes(feedback.PaymentInformationXml), FileextensionsTypes.XML, AttachmentText("NorwayVATAttachmentPayment", reference));

                    feedbackSB.AppendLine();
                    feedbackSB.AppendLine(Uniconta.ClientTools.Localization.lookup(saved ? "NorwayVATAttachmentsSaved" : "NorwayVATAttachmentsNotSaved"));

                    //Only a saved receipt may go with the settlement journal
                    _lastReceipt = feedback.ReceiptPDFData;
                }
            }

            txtSystemInfo.Text = feedbackSB.ToString();

            if (feedback.ReceiptPDFData != null)
                ShowReceipt(feedback.ReceiptPDFData);
        }

        /// <summary>
        /// Attachment name: what it is, the period and the first part of the Altinn instance guid, so each submission's files are distinct
        /// </summary>
        private string AttachmentText(string kindKey, string instanceId)
        {
            var guid = instanceId?.Split('/').LastOrDefault() ?? "";
            var shortId = guid.Length > 8 ? guid.Substring(0, 8) : guid;
            return $"{Uniconta.ClientTools.Localization.lookup(kindKey)} {PeriodText} ({shortId})";
        }

        /// <summary>
        /// Stores a file as an attachment (UserDocs) on the VAT report record. A file with the same text is not stored twice
        /// </summary>
        private async Task<bool> SaveAttachment(byte[] data, FileextensionsTypes type, string text)
        {
            var existing = await api.Query<UserDocsClient>(_glVatReported);
            if (existing != null && existing.Any(x => x.Text == text))
                return true;

            var doc = new UserDocsClient();
            doc.SetMaster(_glVatReported);
            doc._Data = data;
            doc._DocumentType = type;
            doc.Text = text;

            return await api.Insert(doc) == ErrorCodes.Succes;
        }

        /// <summary>
        /// Shows PDF receipt
        /// </summary>
        private void ShowReceipt(byte[] data)
        {
            var att = new UserDocsClient()
            {
                _TableId = GLVatReported.CLASSID,
                _TableRowId = _glVatReported.RowId,
                _RowId = 1,
                _Data = data,
                _DocumentType = FileextensionsTypes.PDF
            };

            ViewDocument(TabControls.UserDocsPage3, att, string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"),
                         Uniconta.ClientTools.Localization.lookup("Receipt")), ViewerType.Attachment);
        }

        #endregion

        #region Settlement journal

        /// <summary>
        /// Transfers the VAT settlement to a journal: the VAT accounts are reset with their posted amounts (with øre), the øre difference goes
        /// to the VAT rounding account and the reported amount (whole kroner) to the VAT settlement account. The journal is never posted here
        /// </summary>
        private async Task TransferToJournal()
        {
            //Only a submission in the selected environment counts: a saved test submission never unlocks a production settlement
            if (!UploadedIn(IsTest))
            {
                //In production the return must be submitted first. In test a trial settlement helps checking the postings
                if (!IsTest)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NorwayVATSendBeforeSettlement"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NorwayVATTrialSettlement") + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Warning"),
                                            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                //Skatteetaten validates the return again after submission. An invalid ("ugyldig") or not yet known result is confirmed first
                var submittedResult = (_feedbackReference != null && _feedbackReference == _glVatReported.Reference ? _submittedValidationResult : null) ??
                                      await LoadStoredValidationResult();
                string warningKey = null;
                if (submittedResult == null)
                    warningKey = "NorwayVATResultUnknownSettlement";
                else if (submittedResult.avvikVedMeldingslevering == Avvikstype.ugyldigskattemelding)
                    warningKey = "NorwayVATInvalidReturnSettlement";

                if (warningKey != null &&
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(warningKey), Uniconta.ClientTools.Localization.lookup("Warning"),
                                            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;
            }

            var accountCache = api.GetCache(typeof(GLAccount)) ?? await api.LoadCache(typeof(GLAccount));
            var accounts = (GLAccount[])accountCache?.GetNotNullArray;
            var settlementAccount = accounts?.FirstOrDefault(x => x.SystemAccountEnum == SystemAccountTypes.VATSettlement)?._Account;
            var roundingAccount = accounts?.FirstOrDefault(x => x.SystemAccountEnum == SystemAccountTypes.VatRounding)?._Account;
            if (String.IsNullOrEmpty(settlementAccount) || String.IsNullOrEmpty(roundingAccount))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NorwayVATSettlementAccountsMissing"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var specificationLines = NorwayVATReportMapper.ToSpecificationLines(_vatReportLines, new List<string>());
            var reportedTotal = specificationLines.Sum(x => x.merverdiavgift);
            var text = Uniconta.ClientTools.Localization.lookup("VATSettlement") + " " + PeriodText;

            CWCopyVoucherToJrnl.Date = ToDate;
            CWCopyVoucherToJrnl.Comment = text;
            CWCopyVoucherToJrnl.InvertSign = false;
            CWCopyVoucherToJrnl.CopyVATTrans = false;

            var cwObj = new CWCopyVoucherToJrnl(api);
            cwObj.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");

            //The dialog is shared with "copy voucher to journal". Invert sign (row 1) and copy VAT transactions (row 5) do not apply here
            foreach (UIElement element in cwObj.contentlayout.Children)
            {
                var row = System.Windows.Controls.Grid.GetRow(element);
                if (row == 1 || row == 5)
                    element.Visibility = Visibility.Collapsed;
            }

            //Wait until the dialog and the transfer are done, so the page treats the transfer as one operation
            var completed = new TaskCompletionSource<bool>();
            cwObj.Closed += async delegate
            {
                try
                {
                    if (cwObj.DialogResult == true)
                        await CreateSettlementLines(cwObj.GlDailyJournal, reportedTotal, settlementAccount, roundingAccount,
                                                    CWCopyVoucherToJrnl.Date == DateTime.MinValue ? ToDate : CWCopyVoucherToJrnl.Date,
                                                    String.IsNullOrEmpty(CWCopyVoucherToJrnl.Comment) ? text : CWCopyVoucherToJrnl.Comment,
                                                    CWCopyVoucherToJrnl.TransType);
                }
                catch (Exception ex)
                {
                    SetBusy(false);
                    UnicontaMessageBox.Show(ex);
                }
                finally
                {
                    completed.TrySetResult(true);
                }
            };
            cwObj.Show();

            await completed.Task;
        }

        private async Task CreateSettlementLines(Uniconta.DataModel.GLDailyJournal journal, decimal reportedTotal, string settlementAccount, string roundingAccount,
                                                 DateTime date, string text, string transType)
        {
            if (journal == null)
                return;

            List<NorwayVATSettlementLine> lines;
            try
            {
                lines = NorwayVATReportMapper.BuildSettlementLines(_vatReportLines, reportedTotal, settlementAccount, roundingAccount, journal._NewVoucherPerLine);
            }
            catch (InvalidOperationException ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            if (lines.Count == 0)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NorwayVATNothingToSettle"), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }

            //Show exactly what will be inserted
            var summary = new StringBuilder();
            summary.AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATSettlementConfirm"), journal._Journal, date.ToShortDateString()));
            summary.AppendLine();
            foreach (var line in lines)
                summary.AppendLine($"{line.Account,-12} {line.OffsetAccount,-12} {line.Amount.ToString("N2", CultureInfo.CurrentCulture),16}");
            summary.AppendLine();
            summary.AppendLine(Uniconta.ClientTools.Localization.lookup("NorwayVATPostedVAT") + ": " + reportedTotal.ToString("N0", CultureInfo.CurrentCulture));

            //Warn when the same settlement seems to be in the journal already. The journal line keeps only the first 50 characters of the text
            const int JournalTextLength = 50;
            var storedText = text != null && text.Length > JournalTextLength ? text.Substring(0, JournalTextLength) : text;
            var existingLines = await api.Query<GLDailyJournalLineClient>(journal);
            if (existingLines != null && existingLines.Any(x => x.Text == storedText && x.Date == date))
            {
                summary.AppendLine();
                summary.AppendLine(Uniconta.ClientTools.Localization.lookup("NorwayVATSettlementAlreadyInJournal"));
            }

            if (UnicontaMessageBox.Show(summary.ToString(), Uniconta.ClientTools.Localization.lookup("GenerateJournalLines"),
                                        MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            SetBusy(true);

            //The receipt goes to the voucher inbox and is linked to every settlement line.
            //If it was not fetched in this page, it is taken from the attachment stored on the VAT report
            //Only the receipt of the submission on the VAT report, and only when it is from the selected environment
            byte[] receipt = null;
            if (ReferenceIsFrom(IsTest))
                receipt = (_feedbackReference != null && _feedbackReference == _glVatReported.Reference ? _lastReceipt : null) ?? await LoadStoredReceipt();

            var documentRef = 0;
            VouchersClient voucher = null;
            if (receipt != null)
            {
                voucher = new VouchersClient()
                {
                    _Data = receipt,
                    Fileextension = FileextensionsTypes.PDF,
                    Text = text,
                    _Content = ContentTypes.Documents
                };
                if (await api.Insert(voucher) == ErrorCodes.Succes)
                    documentRef = voucher.RowId;
                else
                    voucher = null;
            }

            var journalLines = lines.Select(x =>
            {
                var journalLine = new GLDailyJournalLineClient();
                journalLine.SetMaster(journal);
                journalLine.Date = date;
                journalLine.Account = x.Account;
                if (!String.IsNullOrEmpty(x.OffsetAccount))
                    journalLine.OffsetAccount = x.OffsetAccount;
                journalLine.Text = text;
                if (!String.IsNullOrEmpty(transType))
                    journalLine.TransType = transType;
                journalLine.Amount = x.Amount;
                if (documentRef != 0)
                    journalLine.DocumentRef = documentRef;
                return journalLine;
            }).ToList();

            var result = await api.Insert(journalLines);

            //No journal lines: remove the receipt again, so no unlinked voucher is left in the inbox
            if (result != ErrorCodes.Succes && voucher != null)
                await api.Delete(voucher);

            SetBusy(false);

            if (result != ErrorCodes.Succes)
            {
                UtilDisplay.ShowErrorCode(result);
                return;
            }

            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATSettlementDone"), journalLines.Count, journal._Journal),
                                    Uniconta.ClientTools.Localization.lookup("Information"));
        }

        #endregion

        /// <summary>
        /// Adjust vat reporting service test value according to the user selected option
        /// </summary>
        private void ceTest_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue == null)
                return;

            if (txtEnvironment != null)
                UpdateEnvironmentLabel();

            //"Resend" depends on whether the selected environment has a submission
            if (ribbonControl != null)
                UpdateSendCaption();

            //Test and production use different URLs, so a validation result no longer applies
            InvalidateValidation();
        }

        /// <summary>
        /// Adjust free text comment or preselected comment fields according to user selected option
        /// </summary>
        private void cmbCommentType_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            InvalidateValidation();

            if (e.NewValue == null ||
                String.IsNullOrEmpty(e.NewValue.ToString()))
            {
                cmbCommentOption.Visibility = Visibility.Hidden;
                txtComment.Visibility = Visibility.Hidden;
                return;
            }

            if (e.NewValue?.ToString() == ItemChoiceTypeValidation.utvalgtMerknad.ToString())
            {
                cmbCommentOption.Visibility = Visibility.Visible;
                txtComment.Visibility = Visibility.Hidden;
            }
            else
            {
                cmbCommentOption.Visibility = Visibility.Hidden;
                txtComment.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// "Resend" once the return for this period has been sent in the selected environment, otherwise "Send"
        /// </summary>
        private void UpdateSendCaption()
        {
            var sendMenuCommand = UtilDisplay.GetMenuCommandByName((RibbonBase)ribbonControl.DataContext, "Send");
            if (sendMenuCommand != null)
                sendMenuCommand.Caption = Uniconta.ClientTools.Localization.lookup(UploadedIn(IsTest) ? "Resend" : "Send");
        }

        /// <summary>
        /// Attachments of the given type that Send and Feedback stored on the VAT report for the submission recorded on it
        /// </summary>
        private async Task<List<UserDocsClient>> LoadStoredDocuments(FileextensionsTypes type)
        {
            if (!NorwayVATReportHelpers.TryParseReference(_glVatReported.Reference, out var instanceId, out _))
                return new List<UserDocsClient>();

            var docs = await api.Query<UserDocsClient>(_glVatReported);

            var guid = instanceId.Split('/').LastOrDefault() ?? "";
            var shortId = guid.Length > 8 ? guid.Substring(0, 8) : guid;
            var result = docs?.Where(x => x._DocumentType == type && x.Text != null && x.Text.EndsWith($"({shortId})")).ToList() ?? new List<UserDocsClient>();

            //The attachment list is read without file data
            foreach (var doc in result.Where(x => x._Data == null))
                await api.Read(doc);

            return result;
        }

        /// <summary>
        /// The receipt PDF stored on the VAT report by Feedback, or null
        /// </summary>
        private async Task<byte[]> LoadStoredReceipt()
        {
            return (await LoadStoredDocuments(FileextensionsTypes.PDF)).FirstOrDefault()?._Data;
        }

        /// <summary>
        /// Skatteetaten's validation result of the submitted return, stored on the VAT report by Feedback, or null.
        /// The XML attachments are named by language, so the validation result is recognised by its content
        /// </summary>
        private async Task<Valideringsresultat> LoadStoredValidationResult()
        {
            foreach (var doc in await LoadStoredDocuments(FileextensionsTypes.XML))
            {
                if (doc._Data == null)
                    continue;

                try
                {
                    var result = NorwayVATReportHelpers.DeserializeFromXml<Valideringsresultat>(Encoding.UTF8.GetString(doc._Data));
                    if (result != null)
                        return result;
                }
                catch (InvalidOperationException)
                {
                    //Another document (the return, the envelope or the payment information)
                }
            }

            return null;
        }
    }
}
