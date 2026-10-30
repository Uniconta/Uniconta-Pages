
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VoucherApprovalTileControl : System.Windows.Controls.UserControl
    {
        enum VoucherTileView
        {
            NeedsApproval,
            AwaitingOthers,
            Approved
        }

        private VoucherApprovalTileType tileType;
        private VoucherApprovalTileData summary;
        private VoucherTileView selectedView = VoucherTileView.NeedsApproval;
        private bool isApprovalBusy;
        VouchersClient previewVoucher;
        object previewCache;
        readonly Dictionary<long, Task<CachedVoucherPreview>> previewDocuments = new Dictionary<long, Task<CachedVoucherPreview>>();
        int previewVersion;
        readonly DispatcherTimer previewDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };

        readonly DoubleAnimation processingAnimation = new DoubleAnimation(
            0d,
            360d,
            TimeSpan.FromSeconds(1.1))
        {
            RepeatBehavior = RepeatBehavior.Forever
        };

        public VoucherApprovalTileControl()
        {
            InitializeComponent();

            AwaitingOthersTabButton.Click += AwaitingOthersTabButton_Click;
            ApprovedTabButton.Click += ApprovedTabButton_Click;
            NeedsApprovalTabButton.Click += NeedsApprovalTabButton_Click;

            previewDebounceTimer.Tick += (s, e) =>
            {
                previewDebounceTimer.Stop();
                _ = UpdatePreviewAsync();
            };

            SizeChanged += (s, e) =>
            {
                IsWideLayout = e.NewSize.Width >= 600;
                IsPreviewLayout = e.NewSize.Width >= 1000;
                UpdateIdleLayout();
            };

            Loaded += (s, e) => QueuePreviewUpdate();
            Unloaded += (s, e) =>
            {
                ClearPreview();
                previewDocuments.Clear(); // clrea local cache
            };

            UpdateSelectedTab();
        }

        public void SetTileType(VoucherApprovalTileType tileType)
        {
            if (!ReferenceEquals(this.tileType, tileType))
            {
                ClearPreview();
                previewDocuments.Clear();
            }
            this.tileType = tileType;
            UpdateApprovalButtons();
            QueuePreviewUpdate();
        }

        public void SetSummary(VoucherApprovalTileData summary)
        {
            this.summary = summary;

            if (summary == null)
            {
                AwaitingOthersCountText.Text = "0";
                ApprovedCountText.Text = "0";
                NeedsApprovalCountText.Text = "0";

                VoucherList.ItemsSource = null;
                UpdateApprovalButtons();
                UpdateEmptyState();
                return;
            }

            AwaitingOthersCountText.Text = summary.AwaitingOthersCount.ToString("N0", CultureInfo.CurrentCulture);
            ApprovedCountText.Text = summary.ApprovedCount.ToString("N0", CultureInfo.CurrentCulture);
            NeedsApprovalCountText.Text = summary.NeedsApprovalCount.ToString("N0", CultureInfo.CurrentCulture);

            RefreshVoucherList();
        }

        void AwaitingOthersTabButton_Click(object sender, RoutedEventArgs e)
        {
            SelectView(VoucherTileView.AwaitingOthers);
        }

        void ApprovedTabButton_Click(object sender, RoutedEventArgs e)
        {
            SelectView(VoucherTileView.Approved);
        }

        void NeedsApprovalTabButton_Click(object sender, RoutedEventArgs e)
        {
            SelectView(VoucherTileView.NeedsApproval);
        }

        public static readonly DependencyProperty IsPreviewLayoutProperty = DependencyProperty.Register(
            nameof(IsPreviewLayout), typeof(bool), typeof(VoucherApprovalTileControl),
            new PropertyMetadata(false, OnPreviewLayoutChanged));

        public bool IsPreviewLayout
        {
            get => (bool)GetValue(IsPreviewLayoutProperty);
            set => SetValue(IsPreviewLayoutProperty, value);
        }

        static void OnPreviewLayoutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (VoucherApprovalTileControl)sender;
            control.PreviewColumn.Width = control.IsPreviewLayout ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            control.PreviewPanel.Visibility = control.IsPreviewLayout ? Visibility.Visible : Visibility.Collapsed;
            control.UpdateApprovalActionsPlacement();
            if (!control.IsPreviewLayout) control.ClearPreview();
            else control.QueuePreviewUpdate();
        }

        void QueuePreviewUpdate()
        {
            
            previewDebounceTimer.Stop();
            previewVersion++;  // Invalidate any pending asynchronous preview result
            previewDebounceTimer.Start();
        }

        static bool SamePreviewVoucher(VouchersClient first, VouchersClient second)
            => ReferenceEquals(first, second) || (first != null && second != null && first.RowId != 0 &&
                first.RowId == second.RowId && first.CompanyId == second.CompanyId);

        bool IsCurrentPreview(int version, VouchersClient voucher)
            => version == previewVersion && IsLoaded && IsPreviewLayout && MasterDetailContent.Visibility == Visibility.Visible &&
                SamePreviewVoucher(voucher, (VoucherList.SelectedItem as VoucherApprovalTileItem)?.Voucher);

        sealed class CachedVoucherPreview
        {
            public ErrorCodes Result;
            public VouchersClient Voucher;
            public VouchersClient[] Documents;
        }

        async Task<CachedVoucherPreview> GetPreviewDocumentAsync(VouchersClient selected)
        {
            // Unsaved vouchers have no stable cache key.
            if (selected.RowId == 0)
                return await LoadPreviewDocumentAsync(selected, tileType.ApprovalApi);

            var key = ((long)selected.CompanyId << 32) + (uint)selected.RowId;
            if (!previewDocuments.TryGetValue(key, out var loading))
            {
                loading = LoadPreviewDocumentAsync(selected, tileType.ApprovalApi);
                previewDocuments.Add(key, loading);
            }

            try
            {
                var preview = await loading;
                if (preview.Result != ErrorCodes.Succes || (preview.Voucher._Envelope && !(preview.Documents?.Length > 0)))
                    RemoveFailedPreview(key, loading);
                return preview;
            }
            catch
            {
                RemoveFailedPreview(key, loading);
                throw;
            }
        }

        void RemoveFailedPreview(long key, Task<CachedVoucherPreview> loading)
        {
            // A completion from before Unloaded must not evict a newer load after reopening.
            if (previewDocuments.TryGetValue(key, out var current) && ReferenceEquals(current, loading))
                previewDocuments.Remove(key);
        }

        static async Task<CachedVoucherPreview> LoadPreviewDocumentAsync(VouchersClient selected, CrudAPI api)
        {
            // Keep downloaded buffers out of the tile's voucher list so clearing the cache
            // can release them. Reuse data already held by the existing shared cache.
            var voucher = VoucherCache.GetGlobalVoucherCache(selected.CompanyId, selected.RowId)
                ?? (VouchersClient)StreamingManager.Clone(selected);
            var result = voucher._Data == null ? await UtilDisplay.GetData(voucher, api) : ErrorCodes.Succes;
            var preview = new CachedVoucherPreview { Result = result, Voucher = voucher };
            if (result != ErrorCodes.Succes)
                return preview;

            preview.Documents = voucher._Fileextension == FileextensionsTypes.XML ? UtilDisplay.GetXMLAttachments(voucher) : null;
            if (preview.Documents == null || preview.Documents.Length <= 1)
                if (voucher._Envelope)
                    preview.Documents = (VouchersClient[])await new DocumentAPI(api).GetEnvelopeContent(voucher, true);
            return preview;
        }

        async Task UpdatePreviewAsync()
        {
            var selected = (VoucherList.SelectedItem as VoucherApprovalTileItem)?.Voucher;
            if (!IsLoaded || !IsPreviewLayout || MasterDetailContent.Visibility != Visibility.Visible || tileType == null || selected == null)
            {
                ClearPreview();
                PreviewMessage.Text = Uniconta.ClientTools.Localization.lookup("TileSelectAVoucherToPreview");
                PreviewMessage.Visibility = Visibility.Visible;
                return;
            }
            if (SamePreviewVoucher(previewVoucher, selected)) return;
            ClearPreview();
            previewVoucher = selected;
            var version = previewVersion;
            PreviewMessage.Text = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            PreviewMessage.Visibility = Visibility.Visible;
            try
            {
                // Same cache, authorized retrieval and renderer factory as VoucherViewerWindow.
                previewCache = VoucherCache.HoldGlobalVoucherCache;
                var preview = await GetPreviewDocumentAsync(selected);
                if (!IsCurrentPreview(version, selected)) return;
                if (preview.Result != ErrorCodes.Succes)
                {
                    PreviewMessage.Text = preview.Result == ErrorCodes.CouldNotFind
                        ? Uniconta.ClientTools.Localization.lookup("NoVoucherExist")
                        : UtilDisplay.GetFormattedErrorCode(preview.Result, lastErrors: null);
                    return;
                }
                var voucher = preview.Voucher;
                var documents = preview.Documents;
                if (voucher._Envelope && (documents == null || documents.Length == 0))
                {
                    PreviewMessage.Text = Uniconta.ClientTools.Localization.lookup("NoRecords");
                    return;
                }
                if (documents != null && documents.Length > 1)
                {
                    // Reuse the existing embedded attachment viewer and its native navigation.
                    var viewer = new UnicontaVoucherViewer { HasMultipleVouchers = true };
                    RoutedEventHandler loaded = null;
                    loaded = (sender, args) =>
                    {
                        viewer.Loaded -= loaded;
                        if (IsCurrentPreview(version, selected) && ReferenceEquals(PreviewContent.Content, viewer))
                            viewer.Vouchers = documents;
                    };
                    viewer.Loaded += loaded;
                    PreviewContent.Content = viewer;
                }
                else
                    PreviewContent.Content = UtilDisplay.LoadControl(voucher._Envelope ? documents?.FirstOrDefault() ?? voucher : voucher, false, false);
                PreviewMessage.Visibility = Visibility.Collapsed;
            }
            catch (Exception exception)
            {
                if (!IsCurrentPreview(version, selected)) return;
                ReleasePreviewContent();
                PreviewMessage.Text = Uniconta.ClientTools.Localization.lookup("ViewerFailed") + ": " + exception.Message;
            }
        }

        void ClearPreview()
        {
            previewDebounceTimer.Stop();

            // Retrieval has no CancellationToken overload; invalidate late completions instead.
            previewVersion++;
            previewVoucher = null;
            previewCache = null;
            ReleasePreviewContent();
        }

        void ReleasePreviewContent()
        {
            var content = PreviewContent.Content;
            PreviewContent.Content = null;
            try
            {
                if (content is UnicontaWebViewer webViewer) webViewer.CloseUnicontaWebViewer();
                if (content is UnicontaVoucherViewer voucherViewer) voucherViewer.Dispose();
                if (content is PdfViewer pdfViewer)
                {
                    var source = pdfViewer.DocumentSource as IDisposable;
                    pdfViewer.DocumentSource = null;
                    source?.Dispose();
                }
                if (content is IDisposable disposable) disposable.Dispose();
            }
            catch (Exception exception) { Uniconta.ClientTools.InfoLog.Log.Error("Voucher preview cleanup failed: " + exception.Message); }
        }

        public static readonly DependencyProperty IsWideLayoutProperty =
            DependencyProperty.Register(
                nameof(IsWideLayout),
                typeof(bool),
                typeof(VoucherApprovalTileControl),
                new PropertyMetadata(false));

        public bool IsWideLayout
        {
            get => (bool)GetValue(IsWideLayoutProperty);
            set => SetValue(IsWideLayoutProperty, value);
        }

        void UpdateApprovalActionsPlacement()
        {
            var host = IsPreviewLayout ? MasterDetailContent : IdleContent;
            if (!ReferenceEquals(ApprovalActions.Parent, host))
            {
                ((Grid)ApprovalActions.Parent).Children.Remove(ApprovalActions);
                Grid.SetRow(ApprovalActions, IsPreviewLayout ? 1 : 4);
                Grid.SetColumn(ApprovalActions, IsPreviewLayout ? 1 : 0);
                ApprovalActions.Margin = IsPreviewLayout ? new Thickness(12, 8, 0, 0) : new Thickness(0);
                host.Children.Add(ApprovalActions);
            }
            UpdateIdleLayout();
        }

        private void InlineApprove_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.Tag is VoucherApprovalTileItem item)
            {
                // NEW: Prevent approval outside Needs approval or while busy.
                if (selectedView != VoucherTileView.NeedsApproval ||
                    tileType == null || isApprovalBusy)
                    return;

                VoucherList.SelectedItem = item;
                ApproveButton_Click(sender, e);
            }
        }

        private void InlineReject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.Tag is VoucherApprovalTileItem item)
            {
                // NEW: Prevent rejection outside Needs approval or while busy.
                if (selectedView != VoucherTileView.NeedsApproval ||
                    tileType == null || isApprovalBusy)
                    return;

                VoucherList.SelectedItem = item;
                RejectButton_Click(sender, e);
            }
        }

        void SelectView(VoucherTileView view)
        {
            if (selectedView == view)
                return;

            selectedView = view;

            UpdateSelectedTab();
            RefreshVoucherList();
        }

        internal void ShowPendingVoucher(int rowId)
        {
            // Clear any search that would hide the newly imported voucher.
            VoucherSearch.Text = string.Empty;
            SelectView(VoucherTileView.NeedsApproval);
            var item = summary?.NeedsApproval?.FirstOrDefault(voucher => voucher.RowId == rowId);
            if (item == null)
            {
                // Unassigned uploads no longer belong to a tile tab.
                tileType?.OpenVouchersPage();
                return;
            }
            VoucherList.SelectedItem = item;
            VoucherList.ScrollIntoView(item);
            QueuePreviewUpdate();
        }

        void RefreshVoucherList()
        {
            if (summary == null)
            {
                VoucherList.ItemsSource = null;
                UpdateApprovalButtons();
                UpdateEmptyState();
                return;
            }

            IEnumerable<VoucherApprovalTileItem> items;
            var selectedRowId = (VoucherList.SelectedItem as VoucherApprovalTileItem)?.RowId;

            switch (selectedView)
            {
                case VoucherTileView.AwaitingOthers:
                    items = summary.AwaitingOthers;
                    break;

                case VoucherTileView.Approved:
                    items = summary.Approved;
                    break;

                default:
                    items = summary.NeedsApproval;
                    break;
            }

            items = items ?? new VoucherApprovalTileItem[0];
            VoucherList.ItemsSource = items;

            // apply text filter
            var view = CollectionViewSource.GetDefaultView(VoucherList.ItemsSource);
            view.Filter = FilterVoucher;

            VoucherList.SelectedItem = items.FirstOrDefault(item => item.RowId == selectedRowId);
            UpdateApprovalButtons();
            UpdateEmptyState();
        }

        void UpdateEmptyState()
        {
            var hasItems = VoucherList.Items.Count > 0;

            VoucherList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
            EmptyVoucherList.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;

            switch (selectedView)
            {
                case VoucherTileView.AwaitingOthers:
                    EmptyVoucherListText.Text = Uniconta.ClientTools.Localization.lookup("TileNoVouchersWaitingForApproval") + Environment.NewLine +Uniconta.ClientTools.Localization.lookup("TileDragYourVoucherAttachmentsHereToUpload");
                    break;

                case VoucherTileView.Approved:
                    EmptyVoucherListText.Text = Uniconta.ClientTools.Localization.lookup("TileNoApprovedVouchers");
                    break;

                default:
                    EmptyVoucherListText.Text = Uniconta.ClientTools.Localization.lookup("VoucherTileNoMyApprovals") + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("TileDragYourVoucherAttachmentsHereToUpload");
                    break;
            }
        }

        void UpdateSelectedTab()
        {
            SetTabState(AwaitingOthersTabButton, selectedView == VoucherTileView.AwaitingOthers);
            SetTabState(ApprovedTabButton, selectedView == VoucherTileView.Approved);
            SetTabState(NeedsApprovalTabButton, selectedView == VoucherTileView.NeedsApproval);
            UpdateApprovalButtons();
        }

        void VoucherItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left || isApprovalBusy ||
                !(sender is ListBoxItem container) || !(container.DataContext is VoucherApprovalTileItem item))
                return;

            // Inline approve/reject and download buttons keep their own actions.
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source != container)
            {
                if (source is System.Windows.Controls.Primitives.ButtonBase) return;
                source = source is FrameworkContentElement content
                    ? content.Parent
                    : System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            VoucherList.SelectedItem = item;
            tileType?.OpenVouchersPage();
            e.Handled = true;
        }

        void VoucherList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateApprovalButtons();
            QueuePreviewUpdate();
        }

        void UpdateApprovalButtons()
        {
            var needsApproval = selectedView == VoucherTileView.NeedsApproval;
            AddVoucherButton.Visibility = needsApproval || selectedView == VoucherTileView.Approved ? Visibility.Collapsed : Visibility.Visible;
            ApprovalActions.Visibility = needsApproval ? Visibility.Visible : Visibility.Collapsed;
            var enabled = needsApproval && tileType != null && VoucherList.SelectedItem is VoucherApprovalTileItem && !isApprovalBusy;
            ApproveButton.IsEnabled = ApproveWithCommentButton.IsEnabled = RejectButton.IsEnabled = NewApproverButton.IsEnabled = enabled;
            VoucherList.IsEnabled = !isApprovalBusy;
            UpdateIdleLayout();
        }

        void IdleLayout_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateIdleLayout();

        void UpdateIdleLayout()
        {
            var hasFooter = selectedView == VoucherTileView.NeedsApproval
                ? !IsPreviewLayout
                : selectedView != VoucherTileView.Approved;
            IdleFooterGap.Height = new GridLength(hasFooter ? 8 : 0);

            var width = IdleContentScrollViewer.ViewportWidth > 0 ? IdleContentScrollViewer.ViewportWidth : IdleContentScrollViewer.ActualWidth;
            var height = IdleContentScrollViewer.ViewportHeight > 0 ? IdleContentScrollViewer.ViewportHeight : IdleContentScrollViewer.ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            var actionsWidth = IsPreviewLayout ? PreviewPanel.ActualWidth : width;
            ApprovalActions.Columns = actionsWidth < 300 ? 1 : 2;
            var footerHeight = selectedView == VoucherTileView.NeedsApproval
                ? (IsPreviewLayout ? 0d : ApprovalActions.ActualHeight)
                : selectedView == VoucherTileView.Approved ? 0d : AddVoucherButton.ActualHeight;
            var fixedHeight = VoucherTabs.ActualHeight + footerHeight + (hasFooter ? 16d : 8d);
            var listHeight = width < 300 || height < fixedHeight + 64d ? 64d : 0d;
            IdleContent.Height = Math.Max(height, fixedHeight + listHeight);
        }

        void VoucherListScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (IdleContentScrollViewer.ScrollableHeight > 0 &&
                ((e.Delta > 0 && VoucherListScrollViewer.VerticalOffset <= 0) ||
                 (e.Delta < 0 && VoucherListScrollViewer.VerticalOffset >= VoucherListScrollViewer.ScrollableHeight)))
            {
                IdleContentScrollViewer.ScrollToVerticalOffset(IdleContentScrollViewer.VerticalOffset - e.Delta / 3d);
                e.Handled = true;
            }
        }

        async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteApprovalAsync(async item => await tileType.ApproveAsync(item));
        }

        async void ApproveWithCommentButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteApprovalAsync(item => ApproveWithCommentAsync(item, false));
        }

        async void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteApprovalAsync(item => ApproveWithCommentAsync(item, true));
        }

        async Task<ErrorCodes?> ApproveWithCommentAsync(VoucherApprovalTileItem item, bool reject)
        {
            var dialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("Note"), false, DateTime.MinValue);
            dialog.DialogTableId = 2000000068;
            var closed = new TaskCompletionSource<bool>();
            dialog.Closing += (sender, args) => closed.TrySetResult(dialog.DialogResult == true);
            dialog.Show();
            if (!await closed.Task)
                return null;
            return reject ? await tileType.RejectAsync(item, dialog.Comments) : await tileType.ApproveAsync(item, dialog.Comments);
        }

        async void NewApproverButton_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteApprovalAsync(async item =>
            {
                var dialog = new CWEmployee(tileType.ApprovalApi);
                dialog.DialogTableId = 2000000076;
                var closed = new TaskCompletionSource<bool>();
                dialog.Closed += (dialogSender, args) => closed.TrySetResult(dialog.DialogResult == true);
                dialog.Show();
                if (!await closed.Task)
                    return null;
                return await tileType.ChangeApproverAsync(item, dialog.Employee, dialog.Comment);
            });
        }

        async Task ExecuteApprovalAsync(Func<VoucherApprovalTileItem, Task<ErrorCodes?>> action)
        {
            var item = VoucherList.SelectedItem as VoucherApprovalTileItem;
            if (item == null || tileType == null || selectedView != VoucherTileView.NeedsApproval || isApprovalBusy)
                return;
            isApprovalBusy = true;
            UpdateApprovalButtons();
            try
            {
                var result = await action(item);
                if (result == ErrorCodes.Succes)
                {
                    if (summary != null)
                    {
                        summary.NeedsApproval = (summary.NeedsApproval ?? new VoucherApprovalTileItem[0]).Where(voucher => voucher.RowId != item.RowId).ToArray();
                        SetSummary(summary);
                    }
                    await tileType.FetchAsync();
                    SetSummary(tileType.LastSummary);
                }
                else if (result.HasValue)
                    UtilDisplay.ShowErrorCode(result.Value);
            }
            catch (Exception exception)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(exception.Message);
            }
            finally
            {
                isApprovalBusy = false;
                UpdateApprovalButtons();
            }
        }

        void SetTabState(
            System.Windows.Controls.Button button,
            //System.Windows.Controls.Border badge,
            bool selected)
        {
            button.Tag = selected;

            //var foregroundBrush =
            //    TryFindResource("ForegroundBrush") as System.Windows.Media.Brush;

            //var componentBrush =
            //    TryFindResource("LightBackgroundComponentBackground") as System.Windows.Media.Brush;

            //var accentBrush = System.Windows.Media.Brushes.Red;
            //    //TryFindResource("VoucherAccentBrush") as System.Windows.Media.SolidColorBrush ??
            //    //TryFindResource("RibbonControlTabHeaderBackground") as System.Windows.Media.SolidColorBrush;

            //if (selected)
            //{
            //    if (accentBrush != null)
            //    {
            //        var color = accentBrush.Color;

            //        button.Background = new System.Windows.Media.SolidColorBrush(
            //            System.Windows.Media.Color.FromArgb(
            //                0x18,
            //                color.R,
            //                color.G,
            //                color.B));

            //        badge.Background = accentBrush;
            //    }

            //    SetBadgeForeground(
            //        badge,
            //        System.Windows.Media.Brushes.White);

            //    SetTabFontWeight(
            //        button,
            //        System.Windows.FontWeights.SemiBold);
            //}
            //else
            //{
            //    button.Background = System.Windows.Media.Brushes.Transparent;

            //    badge.Background =
            //        componentBrush ?? System.Windows.Media.Brushes.Transparent;

            //    SetBadgeForeground(
            //        badge,
            //        foregroundBrush ?? System.Windows.Media.Brushes.Black);

            //    SetTabFontWeight(
            //        button,
            //        System.Windows.FontWeights.Normal);
            //}
        }

        static void SetBadgeForeground(
            System.Windows.Controls.Border badge,
            System.Windows.Media.Brush foreground)
        {
            var textBlock = badge?.Child as System.Windows.Controls.TextBlock;

            if (textBlock != null)
                textBlock.Foreground = foreground;
        }

        static void SetTabFontWeight(
            System.Windows.Controls.Button button,
            System.Windows.FontWeight fontWeight)
        {
            var panel = button?.Content as System.Windows.Controls.StackPanel;
            if (panel == null)
                return;

            foreach (var child in panel.Children)
            {
                var textBlock = child as System.Windows.Controls.TextBlock;
                if (textBlock == null)
                    continue;

                textBlock.FontWeight = fontWeight;
                break;
            }
        }

        public void SetDropState(TileDropState state, string title, string description)
        {
            var isDragOver = state == TileDropState.DragOver ||
                             state == TileDropState.InvalidDragOver;

            var idle = state == TileDropState.Idle || isDragOver;

            // Keep the underlying content alive during drag-over.
            IdleContentScrollViewer.Visibility =
                idle ? Visibility.Visible : Visibility.Collapsed;

            MasterDetailContent.Visibility =
                idle ? Visibility.Visible : Visibility.Collapsed;

            IdleContent.Visibility =
                idle ? Visibility.Visible : Visibility.Collapsed;

            // Show the overlay without removing the drop target.
            StateContent.Visibility =
                state == TileDropState.Idle
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            DragOverContent.Visibility =
                state == TileDropState.DragOver
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ProcessingContent.Visibility =
                state == TileDropState.Processing
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            SuccessContent.Visibility =
                state == TileDropState.Success
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ErrorContent.Visibility =
                state == TileDropState.Error ||
                state == TileDropState.InvalidDragOver
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (state == TileDropState.DragOver)
            {
                DragOverTitle.Text = title;
                DragOverDescription.Text = description;
            }
            else if (state == TileDropState.Processing)
            {
                ProcessingTitle.Text = title;
                ProcessingDescription.Text = description;
            }
            else if (state == TileDropState.Success)
            {
                SuccessTitle.Text = title;
                SuccessDescription.Text = description;
            }
            else if (state == TileDropState.Error ||
                     state == TileDropState.InvalidDragOver)
            {
                ErrorTitle.Text = title;
                ErrorDescription.Text = description;
            }

            ErrorReturnToStartButton.Visibility = state == TileDropState.Error ? Visibility.Visible : Visibility.Collapsed;

            ProcessingRotation.BeginAnimation(
                System.Windows.Media.RotateTransform.AngleProperty,
                state == TileDropState.Processing
                    ? processingAnimation
                    : null);
        }

        async void AddInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (tileType == null)
                return;

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Uniconta.ClientTools.Localization.lookup("TileSelectInvoice"),
                Filter = Uniconta.ClientTools.Localization.lookup("AllFiles") + " (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            await tileType.OnFileDropAsync(dialog.FileNames);
        }

        async void ViewVoucherButton_Click(object sender, RoutedEventArgs e)
        {
            if (tileType != null)
                await tileType.ViewLastExtractedVoucherAsync();
        }

        void ReturnToStartButton_Click(object sender, RoutedEventArgs e)
        {
            tileType?.ResetDropState();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button
                && button.DataContext is VoucherApprovalTileItem tileItem
                && tileItem.Voucher != null)
            {
                BasePage.ShowInvoiceVoucher(TabControls.VouchersPage3, tileItem.Voucher);
            }
        }

        private void VoucherSearch_TextChanged(object sender, TextChangedEventArgs e) => CollectionViewSource.GetDefaultView(VoucherList.ItemsSource)?.Refresh();

        private bool FilterVoucher(object obj)
        {
            Func<string, string, bool> Contains = (v, s) => v?.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!(obj is VoucherApprovalTileItem item))
                return false;

            var search = VoucherSearch?.Text?.Trim();

            if (string.IsNullOrEmpty(search))
                return true;

            return Contains(item.Title, search)
                || Contains(item.DateText, search)
                || Contains(item.AmountText, search)
                || Contains(item.Approver1, search)
                || Contains(item.Approver2, search);
        }
    }
}
