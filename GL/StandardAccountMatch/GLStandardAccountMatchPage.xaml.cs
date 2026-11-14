using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Xpf.Grid;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Files;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.StandardAccountMatch
{
    public class GLAccountStandardAccountMatchGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(GLAccountStandardAccountMatchClient);
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
    }

    public sealed class GLAccountStandardAccountMatchResult
    {
        public List<GLAccountStandardAccountMatchRow> Matches { get; set; }
    }

    public sealed class GLAccountStandardAccountMatchRow
    {
        [Display(Name = "AccountNumber")]
        public string MyAccountNumber { get; set; }

        [Display(Name = "StandardAccount")]
        public string StandardAccountCode { get; set; }

        /// <summary>
        /// 0-100. How likely this GL account belongs to the selected standard account.
        /// </summary>
        [Display(Name = "Probability")]
        public int ProbabilityPercentage { get; set; }

        /// <summary>
        /// Short explanation. Max around 40 words.
        /// </summary>
        [Display(Name = "Reason")]
        public string Reasoning { get; set; }

        /// <summary>
        /// Optional but very useful for filtering bad matches.
        /// </summary>
        [Display(Name = "GLMatchStdNeedsReview")]
        public bool NeedsHumanReview { get; set; }
    }

    public class GLStandardAccountMatchText
    {
        public static string StandardAccountName => Localization.lookup("StandardAccount");
        public static string ProposedStandardAccountCode => Localization.lookup("GLMatchStdProposedAccountCode");
        public static string ProposedStandardAccountName => Localization.lookup("GLMatchStdProposedAccountName");
        public static string ProbabilityPercentage => Localization.lookup("Probability");
        public static string Reasoning => Localization.lookup("Reason");
        public static string NeedsHumanReview => Localization.lookup("GLMatchStdNeedsReview");
    }

    public sealed class GLAccountStandardAccountMatchClient : GLAccountClient
    {
        private string proposedStandardAccountCode;
        private string proposedStandardAccountName;
        private int probabilityPercentage;
        private string reasoning;
        private bool needsHumanReview;


        [Display(Name = nameof(GLStandardAccountMatchText.StandardAccountName), ResourceType = typeof(GLStandardAccountMatchText))]
        public string StandardAccountName => StandardAccountRef?.KeyName ?? string.Empty;


        public void SetProposedStandardAccountCode(string standardAccountCode)
        {
            if (proposedStandardAccountCode == standardAccountCode)
                return;

            proposedStandardAccountCode = standardAccountCode;

            NotifyPropertyChanged(nameof(ProposedStandardAccountCode));
            NotifyPropertyChanged(nameof(ProposedStandardAccountName));

        }

        [Display(Name = nameof(GLStandardAccountMatchText.ProposedStandardAccountCode), ResourceType = typeof(GLStandardAccountMatchText))]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLStandardCharOfAccount))]
        public string ProposedStandardAccountCode
        {
            get { return proposedStandardAccountCode; }
            set
            {
                SetProposedStandardAccountCode(value);

                NeedsHumanReview = false;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        [ReportingAttribute]
        public GLStandardCharOfAccountClient ProposedStandardAccountRef => ClientHelper.GetRefClient<GLStandardCharOfAccountClient>(CompanyId, typeof(GLStandardCharOfAccount), proposedStandardAccountCode);

        [Display(Name = nameof(GLStandardAccountMatchText.ProposedStandardAccountName), ResourceType = typeof(GLStandardAccountMatchText))]
        public string ProposedStandardAccountName => ProposedStandardAccountRef?.KeyName ?? string.Empty;

        [Display(Name = nameof(GLStandardAccountMatchText.ProbabilityPercentage), ResourceType = typeof(GLStandardAccountMatchText))]
        public int ProbabilityPercentage
        {
            get { return probabilityPercentage; }
            set
            {
                if (probabilityPercentage == value)
                    return;

                probabilityPercentage = Math.Max(0, Math.Min(100, value));
                NotifyPropertyChanged(nameof(ProbabilityPercentage));
            }
        }

        [Display(Name = nameof(GLStandardAccountMatchText.Reasoning), ResourceType = typeof(GLStandardAccountMatchText))]
        public string Reasoning
        {
            get { return reasoning; }
            set
            {
                if (reasoning == value)
                    return;

                reasoning = value;
                NotifyPropertyChanged(nameof(Reasoning));
            }
        }

        [Display(Name = nameof(GLStandardAccountMatchText.NeedsHumanReview), ResourceType = typeof(GLStandardAccountMatchText))]
        public bool NeedsHumanReview
        {
            get { return needsHumanReview; }
            set
            {
                if (needsHumanReview == value)
                    return;

                needsHumanReview = value;
                NotifyPropertyChanged(nameof(NeedsHumanReview));
            }
        }
    }

    public partial class GLStandardAccountMatchPage : GridBasePage, IAIPromptEditDialogModel, LastValue.IPackableWithCustomSerialization, LastValue.IPackableWithKey
    {
        private List<GLAccountClient> accounts;
        public override void PageClosing()
        {
            aiCancellationTokenSource?.Cancel();
            if (!skipLastValuePack)
                LastValue.Pack(api?.CompanyEntity, this);
            base.PageClosing();
        }

        public override string NameOfControl { get { return TabControls.GLStandardAccountMatchPage; } }
        private List<GLAccountStandardAccountMatchClient> viewRows;
        private bool isBusy;
        private bool skipLastValuePack;
        private CancellationTokenSource aiCancellationTokenSource;

        private PromptDefinition pagePrompt;
        public PromptDefinition Prompt { get { return pagePrompt ?? GetAccountMatchPrompt(); } set { pagePrompt = value; } }

        public PromptDefinition OriginalPrompt => GetAccountMatchPrompt();
        public string PackKey => nameof(GLStandardAccountMatchPage);

        private void CWGLAccountStandardAccountMatches_Loaded(object sender, RoutedEventArgs e)
        {
            dgGLAccountStandardAccountMatches.MakeEditable();
            dgGLAccountStandardAccountMatches.tableView.ShowAutoFilterRow = true;
        }

        // This grid is populated from the accounts supplied by GLAccountsPage. Do not let
        // GridBasePage replace that in-memory source with its default server query.
        public override Task InitQuery() { return null; }

        private void UpdateGL()
        {
            var confirmationDialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);

            confirmationDialog.Closing += async delegate
            {
                if (confirmationDialog.ConfirmationResult != CWConfirmationBox.ConfirmationResultEnum.Yes)
                    return;

                if (await ApplyProposedStandardAccountsAsync())
                {
                    CloseDockItem();
                }
            };

            confirmationDialog.Show();
        }

        private async Task<bool> ApplyProposedStandardAccountsAsync()
        {
            if (isBusy)
            {
                Uniconta.ClientTools.InfoLog.Log.Warning(Localization.lookup("Busy"));

                return false;
            }

            isBusy = true;
            SetStatus(Localization.lookup("GLMatchStdUpdating"));

            Func<GLAccountClient, string, CrudAPI.UpdatePair> updateGLAccount = (gl, standardAccountCode) =>
            {
                var loaded = gl.Orig();

                var modified = (GLAccountClient)gl.Orig();
                modified.StandardAccount = standardAccountCode;

                return new CrudAPI.UpdatePair
                {
                    loaded = loaded,
                    modified = modified
                };
            };

            try
            {
                busyIndicator.IsBusy = true;

                var standardAccounts = await api.LoadCache<GLStandardCharOfAccountClient>();
                var validCodes = new HashSet<string>(standardAccounts.Select(x => x.KeyStr), StringComparer.OrdinalIgnoreCase);
                if (viewRows.Any(row => !string.IsNullOrWhiteSpace(row.ProposedStandardAccountCode) && !validCodes.Contains(row.ProposedStandardAccountCode)))
                {
                    Uniconta.ClientTools.InfoLog.Log.Warning("Review the proposed standard accounts: one or more codes are not in the current standard chart of accounts.");
                    return false;
                }

                var actualAccounts = await api.LoadCache<GLAccountClient>();
                var rowsToUpdate = actualAccounts
                        .Where(actual => !string.IsNullOrWhiteSpace(actual.Account))
                        .Select(actual => new
                        {
                            Actual = actual,
                            View = viewRows.FirstOrDefault(view =>
                                !string.IsNullOrWhiteSpace(view.Account) &&
                                !string.IsNullOrWhiteSpace(view.ProposedStandardAccountCode) &&
                                string.Equals(
                                    actual.Account,
                                    view.Account,
                                    StringComparison.OrdinalIgnoreCase))
                        })
                        .Where(x =>
                            x.View != null &&
                            !string.Equals(
                                x.Actual.StandardAccount,
                                x.View.ProposedStandardAccountCode,
                                StringComparison.OrdinalIgnoreCase))
                        .Select(x => updateGLAccount(
                            x.Actual,
                            x.View.ProposedStandardAccountCode))
                        .ToList();

                if (rowsToUpdate.Count == 0)
                {
                    Uniconta.ClientTools.InfoLog.Log.Info(Localization.lookup("GLMatchStdNoAccountsNeedUpdating"));
                    return false;
                }

                ShowProgress(rowsToUpdate.Count);

                var processed = 0;

                var updateResult = await rowsToUpdate.SplitCrudAsync(api.Update, i =>
                {
                    processed += i;
                    AIProgressBar.Value = processed;
                    SetStatus(string.Format(Localization.lookup("GLMatchStdUpdatedAccounts"), processed, rowsToUpdate.Count));
                });

                if (updateResult != ErrorCodes.Succes)
                {
                    Uniconta.ClientTools.InfoLog.Log.Error($"Error during update of ledger account(s): {updateResult}");
                    // Keep the page and proposals available after a partial failure.
                    LastValue.Pack(api?.CompanyEntity, this);
                    return false;
                }

                if (processed == 0)
                    Uniconta.ClientTools.InfoLog.Log.Info(Localization.lookup("GLMatchStdNoAccountsUpdated"));
                else
                {
                    LastValue.Delete(api?.CompanyEntity, this);
                    skipLastValuePack = true;
                    Uniconta.ClientTools.InfoLog.Log.Info(string.Format(Localization.lookup("GLMatchStdAccountsUpdated"), processed));
                }

                return true;

            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex);
                return false;
            }
            finally
            {
                busyIndicator.IsBusy = false;

                HideProgress();
                SetStatus(Localization.lookup("GLMatchStdReady"));
                isBusy = false;

            }
        }


        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CloseDockItem();
        }

        public static async Task<byte[]> CreateStandardAccountsJson(CrudAPI Api)
        {
            var stream = new MemoryStream();

            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true))
            {
                var stdAccounts = await Api.LoadCache<GLStandardCharOfAccountClient>();

                foreach (var stdAccount in stdAccounts)
                {
                    writer.WriteLine($"Account: {stdAccount.KeyStr}, Name: {stdAccount.KeyName}"); // only write properties, not references
                }

                await writer.FlushAsync();
            }

            return stream.ToArray();
        }
        public static async Task<IFileUploadData> GetStandardAccountStructureData(CrudAPI api)
        {
            return new FileUploadData
            {
                FileName = "StandardAccountsStructure.json",
                ContentType = "text/plain",
                FileData = await CreateStandardAccountsJson(api)
            };
        }

        public GLStandardAccountMatchPage(IEnumerable<GLAccountClient> accounts) : base(accounts == null ? null : accounts.FirstOrDefault()) => InitializePage(accounts);
        public GLStandardAccountMatchPage(object accounts) : this(accounts as IEnumerable<GLAccountClient>) { }
        public GLStandardAccountMatchPage(CrudAPI api, string dummy) : base(api, dummy) => InitializePage(null);

        private static bool IsValidAccountType(GLAccountTypes accountType)
            => accountType != GLAccountTypes.CalculationExpression
            && accountType != GLAccountTypes.Header
            && accountType != GLAccountTypes.Sum;


        private void InitializePage(IEnumerable<GLAccountClient> accounts)
        {
            DataContext = this;
            InitializeComponent();
            ConfigureGridFormatting();
            frmRibbon.OnItemClicked += Ribbon_OnItemClicked;

            this.accounts = accounts == null ? new List<GLAccountClient>() : accounts.ToList();
            this.viewRows = this.accounts
                .Where(a => IsValidAccountType(a.AccountTypeEnum)).Select(a => new GLAccountStandardAccountMatchClient
            {
                CompanyId = a.CompanyId,
                Account = a.Account,
                Name = a.Name,
                StandardAccount = a.StandardAccount,
                NeedsHumanReview = true,
                Reasoning = Localization.lookup("GLMatchStdNotMatchedYet")
            }).ToList();

            LastValue.Unpack(api?.CompanyEntity, this);

            dgGLAccountStandardAccountMatches.api = api;
            dgGLAccountStandardAccountMatches.BusyIndicator = busyIndicator;
            //dgGLAccountStandardAccountMatches.Readonly = true;
            dgGLAccountStandardAccountMatches.SetSource(viewRows.ToArray());
            dgGLAccountStandardAccountMatches.Loaded += CWGLAccountStandardAccountMatches_Loaded;
            SetRibbonControl(frmRibbon, dgGLAccountStandardAccountMatches);

            ZoomEnabler.EnableZoom(LayoutRoot);

            //Uniconta.ClientTools.AI.Services.GPTServiceFactory.Instance.GetActiveService(api)?.Branding?.UpdateRibbonIcon(frmRibbon, "AskAI");
        }

        private void ConfigureGridFormatting()
        {
            var proposedAccountFormat = new DevExpress.Xpf.Core.ConditionalFormatting.Format
            {
                Background = System.Windows.Media.Brushes.PowderBlue
            };
            var changedAccountFormat = new DevExpress.Xpf.Core.ConditionalFormatting.Format
            {
                Background = System.Windows.Media.Brushes.Orange
            };

            dgGLAccountStandardAccountMatches.tableView.FormatConditions.Add(new FormatCondition
            {
                FieldName = nameof(GLAccountStandardAccountMatchClient.ProposedStandardAccountCode),
                Expression = "([StandardAccount] == null Or [StandardAccount] == '') And [ProposedStandardAccountCode] != null And [ProposedStandardAccountCode] != ''",
                ApplyToRow = false,
                Format = proposedAccountFormat
            });
            dgGLAccountStandardAccountMatches.tableView.FormatConditions.Add(new FormatCondition
            {
                FieldName = nameof(GLAccountStandardAccountMatchClient.ProposedStandardAccountCode),
                Expression = "[StandardAccount] != null And [StandardAccount] != '' And [ProposedStandardAccountCode] != null And [ProposedStandardAccountCode] != '' And [ProposedStandardAccountCode] != [StandardAccount]",
                ApplyToRow = false,
                Format = changedAccountFormat
            });
        }
        private void Ribbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AskAI":
                    _ = MatchWithAI();
                    break;
                case "UpdateGL":
                    UpdateGL();
                    break;
                case "Prompt":
                    ShowPromptEditor();
                    break;
                case "Cancel":
                    CancelRunningAIOperation();
                    break;
                case "Clear":
                    ClearAIValues();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async Task MatchWithAI()
        {
            if (api == null || !GPTServiceFactory.IsAccountMatchEnabled(api.CompanyEntity) || isBusy || accounts == null || accounts.Count == 0)
                return;

            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("StandardAccountExtraCostsMayBeInvolvedAreYouSure"),
                Localization.lookup("Confirmation"), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            isBusy = true;
            aiCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = aiCancellationTokenSource.Token;

            try
            {
                await api.Insert(new SubscriptionClickLog
                {
                    _Action = SubscriptionProduct.GLAccountMapping
                });

                SetStatus(Localization.lookup("GLMatchStdMatching"));
                ShowProgress(accounts.Count);

                var processed = 0;
                var chunks = Chunk(accounts, 10).ToList();

                var gptService = api.CompanyEntity.GetGPTService(api);
                var file = await GetStandardAccountStructureData(api);
                var standardAccounts = await api.LoadCache<GLStandardCharOfAccountClient>();
                var validCodes = new HashSet<string>(standardAccounts.Select(x => x.KeyStr), StringComparer.OrdinalIgnoreCase);

                foreach (var accountChunk in chunks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var context = GetContext(file, accountChunk);
                    var result = await gptService.GetResponse<GLAccountStandardAccountMatchResult>((IGPTConversationContext)context, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (result?.Matches != null && result.Matches.Count > 0)
                    {
                        var requested = new HashSet<string>(accountChunk.Select(x => x.Account), StringComparer.OrdinalIgnoreCase);
                        result.Matches = result.Matches.Where(x => x != null && requested.Contains(x.MyAccountNumber)).ToList();
                        foreach (var match in result.Matches)
                        {
                            if (!validCodes.Contains(match.StandardAccountCode ?? string.Empty))
                            {
                                match.StandardAccountCode = null;
                                match.ProbabilityPercentage = 0;
                                match.NeedsHumanReview = true;
                            }
                        }
                        ApplyAIResultToView(result);
                    }

                    processed += accountChunk.Count;
                    AIProgressBar.Value = processed;
                    SetStatus(string.Format(Localization.lookup("GLMatchStdMatchedAccounts"), processed, accounts.Count));
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus(Localization.lookup("GLMatchStdMatchingCancelled"));
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.Controls.AIErrorResolver.Handle(ex);
                SetStatus(Localization.lookup("GLMatchStdReady"));
            }
            finally
            {
                HideProgress();
                aiCancellationTokenSource?.Dispose();
                aiCancellationTokenSource = null;
                isBusy = false;
            }
        }

        private void CancelRunningAIOperation()
        {
            if (aiCancellationTokenSource == null)
            {
                CloseDockItem();
                return;
            }

            SetStatus(Localization.lookup("GLMatchStdCancelling"));
            aiCancellationTokenSource.Cancel();
        }

        private void ShowPromptEditor()
        {
            var promptEditor = new CWAIPromptEdit(this);
            promptEditor.Show();
        }

        private void ShowProgress(int maximum)
        {
            AIProgressBar.Visibility = Visibility.Visible;
            AIProgressBar.Minimum = 0;
            AIProgressBar.Maximum = maximum;
            AIProgressBar.Value = 0;
        }

        private void HideProgress()
        {
            AIProgressBar.Visibility = Visibility.Collapsed;
            AIProgressBar.Value = 0;
        }

        private void SetStatus(string statusText)
        {
            StatusText.Text = statusText;
        }

        private IGPTRequestContext GetContext(IFileUploadData file, List<GLAccountClient> accountChunk)
        {
            var context = new GPTConversationContextWithAttachment
            {
                File = file
            };

            if (Prompt != null)
            {
                Prompt.Replace(new Dictionary<string, string>
                {
                    {
                        "Accounts",
                        Newtonsoft.Json.JsonConvert.SerializeObject(accountChunk.Select(a => new
                        {
                            MyAccountNumber = a.Account,
                            MyAccountName = a.Name,
                            a.AccountType
                        }).ToList())
                    },
                    {
                        "CountryId",
                        api.CompanyEntity._CountryId.ToString()
                    }
                }).EnrichContext(context);
            }

            return context;
        }
        private static PromptDefinition GetAccountMatchPrompt() => PromptProvider.From(() => AccountMatchPrompt);

        [PromptDefinition("GeneralLedger_StandardAccountMatch", "Prompt used to match ledger accounts to government standard accounts.")]
        [PromptDefinitionReplacement("CountryId", "The country identifier used to select and describe the government standard accounts.")]
        [PromptDefinitionReplacement("accounts", "The GL account records to match against the government standard accounts.")]
        public static PromptDefinition AccountMatchPrompt =>
            PromptDefinition.FromMessages(
                new[]
                {
            new PromptRoleNode
            {
                Role = MessageRole.Developer,
                Content =
@"You are an ERP accounting classification assistant.

Your task is to match each provided GLAccountClient account to the most appropriate account in the attached {{CountryId}} government StandardAccounts list.

Rules:
- Return exactly one match object per provided GLAccountClient.
- Use only the attached StandardAccounts list as the source for standard account codes and names.
- Be conservative. If the match is uncertain, lower the probability and set NeedsHumanReview to true.
- Do not invent standard accounts.
- Do not ignore account type, account name, debit/credit meaning, VAT meaning, balance sheet versus profit-and-loss category, or currency context.
- Similar wording alone is not enough when the accounting meaning differs.
- If no good match exists, choose the closest available standard account, use a low probability, and explain why review is needed.
- ProbabilityPercentage must be an integer from 0 to 100.
- Reasoning must be short and factual.
- Use the default language of {{CountryId}} for the reasoning."
            },
            new PromptRoleNode
            {
                Role = MessageRole.User,
                Content =
@"Match the supplied GLAccountClient records against the attached {{CountryId}} StandardAccounts list.
Return only the structured result. Do not include markdown or additional text.
Accounts to match {{accounts}}"
            }
                });
        public void InitParmDefault()
        {
        }

        public string Pack()
        {
            var values = viewRows
                .Where(HasAIValue)
                .Select(x => new GLAccountStandardAccountMatchRow
                {
                    MyAccountNumber = x.Account,
                    StandardAccountCode = x.ProposedStandardAccountCode,
                    ProbabilityPercentage = x.ProbabilityPercentage,
                    Reasoning = x.Reasoning,
                    NeedsHumanReview = x.NeedsHumanReview
                })
                .ToList();

            if (values.Count == 0)
                return null;

            return Newtonsoft.Json.JsonConvert.SerializeObject(values, Newtonsoft.Json.Formatting.Indented);
        }

        public bool Unpack(string packedObject)
        {
            var values = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GLAccountStandardAccountMatchRow>>(packedObject);

            if (values == null || values.Count == 0)
                return true;

            ApplyAIResultToView(new GLAccountStandardAccountMatchResult { Matches = values });

            return true;
        }

        private static bool HasAIValue(GLAccountStandardAccountMatchClient row)
        {
            return !string.IsNullOrWhiteSpace(row.ProposedStandardAccountCode)
                || row.ProbabilityPercentage != 0
                || (!string.IsNullOrWhiteSpace(row.Reasoning) && row.Reasoning != Localization.lookup("GLMatchStdNotMatchedYet"))
                || !row.NeedsHumanReview;
        }

        private void ClearAIValues()
        {
            if (isBusy || viewRows == null)
                return;

            foreach (var row in viewRows)
                ClearAIValues(row);

            LastValue.Delete(api?.CompanyEntity, this);
            dgGLAccountStandardAccountMatches.RefreshData();
            SetStatus(Localization.lookup("GLMatchStdValuesCleared"));
        }

        private static void ClearAIValues(GLAccountStandardAccountMatchClient row)
        {
            row.SetProposedStandardAccountCode(null);
            row.ProbabilityPercentage = 0;
            row.Reasoning = Localization.lookup("GLMatchStdNotMatchedYet");
            row.NeedsHumanReview = true;
        }

        private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
        {
            var chunk = new List<T>(size);

            foreach (var item in source)
            {
                chunk.Add(item);

                if (chunk.Count == size)
                {
                    yield return chunk;
                    chunk = new List<T>(size);
                }
            }

            if (chunk.Count > 0)
                yield return chunk;
        }
        private void ApplyAIResultToView(GLAccountStandardAccountMatchResult result)
        {
            if (result?.Matches == null)
                return;

            skipLastValuePack = false;

            var matchesByAccount = result.Matches
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.MyAccountNumber))
                .GroupBy(x => x.MyAccountNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var row in viewRows)
            {
                GLAccountStandardAccountMatchRow match;
                if (!matchesByAccount.TryGetValue(row.Account, out match))
                    continue;

                row.SetProposedStandardAccountCode(match.StandardAccountCode);
                //row.ProposedStandardAccountName = match.StandardAccountName;
                row.ProbabilityPercentage = match.ProbabilityPercentage;
                row.Reasoning = match.Reasoning;
                row.NeedsHumanReview = match.NeedsHumanReview;
            }
        }
    }

    public sealed class ProposedStandardAccountNameBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var row = value as GLAccountStandardAccountMatchClient;
            if (row == null)
                return null;

            if (!string.IsNullOrWhiteSpace(row.ProposedStandardAccountName) &&
                !string.Equals(row.ProposedStandardAccountName, row.StandardAccount, StringComparison.OrdinalIgnoreCase))
            {
                return System.Windows.Media.Brushes.Orange;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
