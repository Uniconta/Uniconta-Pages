using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.Controls.AI.Assistant.DataProviders;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class TilePage : ControlBasePage, IAIAssistantConsumer, IAIAssistantConsumerWithPromptSuggestions
    {
        List<UserMenuClient> tiles;
        readonly TileLayoutService tileLayout = new TileLayoutService();
        readonly Dictionary<UserMenuClient, TileHost> hosts = new Dictionary<UserMenuClient, TileHost>();
        readonly List<System.Windows.Controls.Button> dashboardDropTargets = new List<System.Windows.Controls.Button>();
        ResizeSession resize;
        DragSession drag;
        bool savingPlacement, editMode, pageClosing;
        Task reloadTilesTask;
        bool reloadTilesRequested;
        readonly HashSet<UserMenuClient> defaultTiles = new HashSet<UserMenuClient>();
        DashboardKey selectedDashboard = new DashboardKey(TileScope.Company, null);
        readonly DashboardSelectionState dashboardSelection;

        public string LastUpdated { get; private set; }

        // Hook for company dashboard permissions; implement the permission check here.
        protected virtual bool CanAddCompanyTiles => api.session.User.IsResellerOrOwner(api.CompanyEntity);

        bool CanAddTilesToDashboard(DashboardKey dashboard) => dashboard.Scope == TileScope.User 
                                                            || CanAddCompanyTiles;

        public TilePage(CrudAPI crudApi) : base(crudApi, string.Empty)
        {
            InitializeComponent();
            btnCopyToApp.Content = string.Format(Uniconta.ClientTools.Localization.lookup("CopyToOBJ"), Uniconta.ClientTools.Localization.lookup("App"));
            Loaded += TilePage_Loaded;
            IsVisibleChanged += TilePage_IsVisibleChanged;
            txtGreeting.Text = GetGreeting(api.session.User._Name);

            dashboardSelection = new DashboardSelectionState("TilePage.ActiveDashboard_" + api.session.User.Uid);
            LastValue.Unpack(api.CompanyEntity, dashboardSelection);
            selectedDashboard = new DashboardKey(dashboardSelection.Scope, dashboardSelection.Name, dashboardSelection.Profile);

            tiles = (UserMenuItems.GetTiles(api.CompanyEntity) ?? Enumerable.Empty<UserMenuClient>())
                                  .OrderBy(t => t._LineNumber)
                                  .ToList();

            PreviewKeyDown += TilePage_PreviewKeyDown;

            ZoomEnabler.EnableZoom(tileViewport);

            RebuildTiles();
        }

        private async void TilePage_Loaded(object sender, RoutedEventArgs e)
        {
            await ReloadTilesAsync();
            await UpdateGreetingAsync();
        }

        void TilePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateTileCreationAvailability();
            if (IsVisible && IsLoaded) _ = ReloadTilesAsync();
            else if (!IsVisible) CancelInteractions();
        }

        async Task ReloadTilesAsync()
        {
            if (pageClosing || savingPlacement) return;
            if (reloadTilesTask != null) reloadTilesRequested = true;
            var pending = reloadTilesTask ?? (reloadTilesTask = ReloadTilesCoreAsync());
            try { await pending; }
            catch (Exception ex) { Uniconta.ClientTools.InfoLog.Log.Error(ex.Message); }
            finally { if (ReferenceEquals(reloadTilesTask, pending)) reloadTilesTask = null; }
        }

        async Task ReloadTilesCoreAsync()
        {
            do
            {
                reloadTilesRequested = false;
                await UserMenuItems.Load(api);

                if (pageClosing || savingPlacement) return;
                tiles = (UserMenuItems.GetTiles(api.CompanyEntity) ?? new List<UserMenuClient>())
                    .OrderBy(tile => tile._LineNumber).ToList();
                LoadDefaultTilesIfEmpty();
                RebuildTiles();
                UpdateLastUpdated();
            } while (reloadTilesRequested);
        }

        private async Task UpdateGreetingAsync()
        {
            try
            {
                var employees = await api.LoadCache<EmployeeClient>();
                var currentEmployee = employees?.FirstOrDefault(e => e.UserLoginId == api.session.LoginId);

                if (currentEmployee != null)
                {
                    var name = currentEmployee.Name;

                    if (!string.IsNullOrWhiteSpace(name))
                        txtGreeting.Text = GetGreeting(name);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to load employee greeting: {ex}");
            }
        }

        private void UpdateLastUpdated()
        {
            LastUpdated = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Updatedon"), DateTime.Now.ToString("g"));
            txtLastUpdated.Text = LastUpdated;
        }

        private void RebuildTiles()
        {
            if (savingPlacement)
                return;
            CancelInteractions();
            tiles?.RemoveAll(tile => !UserMenuItems.IsTileVisible(tile, api.CompanyEntity));
            RebuildDashboardTabs();
            tilesPanel.Children.Clear();
            tilesPanel.IsEditing = editMode;
            hosts.Clear();

            if (tiles == null)
                return;

            var dashboardTiles = tiles.Where(t => !TileDefinition.FromUserMenu(t).IsDashboardDefinition)
                .Where(t => DashboardKey.From(t).Equals(selectedDashboard)).ToList();
            emptyDashboard.Visibility = dashboardTiles.Count == 0 && !editMode ? Visibility.Visible : Visibility.Collapsed;

            foreach (var menu in dashboardTiles.OrderBy(t => t._LineNumber))
                AddTile(tilesPanel, menu);
        }

        private void RebuildDashboardTabs()
        {
            var dashboards = GetDashboards();
            if (!dashboards.Contains(selectedDashboard))
                selectedDashboard = dashboards.FirstOrDefault();

            var savedDashboard = new DashboardKey(dashboardSelection.Scope, dashboardSelection.Name, dashboardSelection.Profile);
            if (!savedDashboard.Equals(selectedDashboard))
            {
                dashboardSelection.Scope = selectedDashboard.Scope;
                dashboardSelection.Name = selectedDashboard.Name;
                dashboardSelection.Profile = selectedDashboard.Profile;
                LastValue.Pack(api.CompanyEntity, dashboardSelection);
            }

            UpdateTileCreationAvailability();

            dashboardTabs.Children.Clear();
            dashboardDropTargets.Clear();
            foreach (var dashboard in dashboards)
            {
                var button = new System.Windows.Controls.Button
                {
                    Content = dashboard.DisplayName,
                    Tag = dashboard,
                    Style = (Style)FindResource("DashboardTabButtonStyle"),
                    FontWeight = dashboard.Equals(selectedDashboard) ? FontWeights.SemiBold : FontWeights.Normal,
                    BorderBrush = (System.Windows.Media.Brush)FindResource("ForegroundBrush"),
                    BorderThickness = dashboard.Equals(selectedDashboard) ? new Thickness(0, 0, 0, 2) : new Thickness(0)
                };
                button.Click += DashboardTab_Click;
                dashboardDropTargets.Add(button);

                if (editMode && !string.IsNullOrWhiteSpace(dashboard.Name))
                {
                    var header = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
                    button.Margin = new Thickness(0);
                    header.Children.Add(button);

                    var edit = new System.Windows.Controls.Button
                    {
                        Content = true,
                        ContentTemplate = (DataTemplate)FindResource("EditIconTemplate"),
                        Tag = dashboard,
                        ToolTip = Uniconta.ClientTools.Localization.lookup("Edit"),
                        IsEnabled = CanAddTilesToDashboard(dashboard),
                        Style = (Style)FindResource("DashboardTabButtonStyle"),
                        Margin = new Thickness(3, 0, 0, 0),
                        Padding = new Thickness(2, 0, 2, 0)
                    };
                    edit.Click += EditDashboard_Click;
                    header.Children.Add(edit);

                    var delete = new System.Windows.Controls.Button
                    {
                        Content = true,
                        ContentTemplate = (DataTemplate)FindResource("DeleteIconTemplate"),
                        Tag = dashboard,
                        ToolTip = Uniconta.ClientTools.Localization.lookup("Delete"),
                        Style = (Style)FindResource("DashboardTabButtonStyle"),
                        Margin = new Thickness(3, 0, 18, 0),
                        Padding = new Thickness(2, 0, 2, 0)
                    };
                    delete.Click += DeleteDashboard_Click;
                    header.Children.Add(delete);
                    dashboardTabs.Children.Add(header);
                }
                else
                    dashboardTabs.Children.Add(button);
            }

            var create = new System.Windows.Controls.Button { Content = Uniconta.ClientTools.Localization.lookup("TileCreateNew"), Style = (Style)FindResource("DashboardTabButtonStyle") };
            create.Click += CreateDashboard_Click;
            dashboardTabs.Children.Add(create);
        }

        private void UpdateTileCreationAvailability()
        {
            btnImport.Visibility = btnExport.Visibility = editMode ? Visibility.Visible : Visibility.Collapsed;
            btnEdit.IsEnabled = !savingPlacement && (editMode || CanAddTilesToDashboard(selectedDashboard));
            btnImport.IsEnabled = !savingPlacement && CanAddTilesToDashboard(selectedDashboard);
            btnExport.IsEnabled = !savingPlacement;
            btnCopyToApp.Visibility = Utility.GetCurrentApp() != null ? Visibility.Visible : Visibility.Collapsed;
            // new tiles default to My Dashboard, regardless of the active tab.
            btnAdd.Visibility = btnAddEmptyDashboard.Visibility = btnAddDashboard.Visibility = Visibility.Visible;
            btnCreateWithAI.Visibility = btnCreateWithAIBottom.Visibility = !GPTServiceFactory.HideAIControls ? Visibility.Visible : Visibility.Collapsed;
            btnAdd.IsEnabled = btnAddEmptyDashboard.IsEnabled = btnAddDashboard.IsEnabled = btnCreateWithAI.IsEnabled = btnCreateWithAIBottom.IsEnabled = !savingPlacement;
        }

        private List<DashboardKey> GetDashboards()
        {
            var companyDashboard = new DashboardKey(TileScope.Company, null);
            var dashboards = new List<DashboardKey>
            {
                new DashboardKey(TileScope.User, null)
            };

            if (CanAddCompanyTiles || (tiles != null && tiles.Any(tile =>
                !TileDefinition.FromUserMenu(tile).IsDashboardDefinition && DashboardKey.From(tile).Equals(companyDashboard))))
                dashboards.Add(companyDashboard);

            if (tiles != null)
                dashboards.AddRange(tiles.Select(DashboardKey.From).Where(x => !string.IsNullOrWhiteSpace(x.Name)));

            return dashboards.Distinct().OrderByDescending(x => x.Equals(companyDashboard))
                .ThenByDescending(x => x.Scope)
                .ThenBy(x => string.IsNullOrWhiteSpace(x.Name) ? 0 : 1)
                .ThenBy(x => x.Name).ToList();
        }

        private void DashboardTab_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement)
                return;
            var dashboard = (DashboardKey)((System.Windows.Controls.Button)sender).Tag;
            if (editMode && !CanAddTilesToDashboard(dashboard))
                btnEdit_Click(btnEdit, new RoutedEventArgs());
            selectedDashboard = dashboard;
            RebuildTiles();
        }

        private async void DeleteDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement)
                return;
            CancelInteractions();
            var dashboard = (DashboardKey)((System.Windows.Controls.Button)sender).Tag;
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"),
                Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;

            var dashboardItems = tiles.Where(tile => DashboardKey.From(tile).Equals(dashboard)).ToList();
            var result = await api.Delete(dashboardItems);
            if (result != ErrorCodes.Succes)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(result);
                return;
            }

            foreach (var item in dashboardItems)
                tiles.Remove(item);

            if (selectedDashboard.Equals(dashboard))
                selectedDashboard = new DashboardKey(TileScope.Company, null);

            RebuildTiles();
        }

        private void EditDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement || !editMode)
                return;
            CancelInteractions();
            var dashboard = (DashboardKey)((System.Windows.Controls.Button)sender).Tag;
            if (!CanAddTilesToDashboard(dashboard))
                return;
            var dashboardItems = tiles.Where(tile => DashboardKey.From(tile).Equals(dashboard)).ToList();
            var dialog = new DashboardDetailsDialog(api, dashboardItems);
            dialog.Closing += delegate
            {
                if (dialog.DialogResult != true)
                    return;
                tiles.RemoveAll(tile => dashboardItems.Contains(tile));
                tiles.AddRange(dialog.SavedMenus);
                selectedDashboard = DashboardKey.From(dialog.SavedMenu);
                RebuildTiles();
            };
            dialog.Show();
        }

        private void CreateDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement)
                return;
            CancelInteractions();
            var dialog = new DashboardDetailsDialog(api);
            dialog.Closing += delegate
            {
                if (dialog.DialogResult != true)
                    return;
                tiles.Add(dialog.SavedMenu);
                selectedDashboard = DashboardKey.From(dialog.SavedMenu);
                RebuildTiles();
            };
            dialog.Show();
        }

        private void AddTile(TileGridPanel panel, UserMenuClient menu)
        {
            var control = new TileControl(this, menu)
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            control.IsDeleteMode = editMode;

            var host = new TileHost { Content = control, Tag = menu, Focusable = true };
            var definition = control.Definition;

            TileGridPanel.SetColumnSpan(host, definition.ColumnSpan);
            TileGridPanel.SetRowSpan(host, definition.RowSpan);
            TileGridPanel.SetGridColumn(host, definition.GridColumn ?? -1);
            TileGridPanel.SetGridRow(host, definition.GridRow ?? -1);

            hosts[menu] = host;

            control.Deleted += () =>
            {
                CancelInteractions();
                tiles.RemoveAll(tile => tile.RowId == menu.RowId);
                RebuildTiles();
            };

            control.Changed += () => _ = ReloadTilesAsync();
            control.MenuRequested += () => { if (!savingPlacement && !pageClosing) ShowTileMenu(control, menu); };
            control.ResizeStarted += (s, e) => BeginResize(host, panel, control, menu);
            control.ResizePreview += (s, e) => UpdateResize();
            control.ResizeCompleted += async (s, e) => await CommitResizeAsync();
            control.ResizeCanceled += (s, e) => CancelResize();

            control.DragHandle.PreviewMouseLeftButtonDown += (s, e) => BeginDrag(host, e);
            host.PreviewMouseMove += TileHost_PreviewMouseMove;
            host.PreviewMouseLeftButtonUp += async (s, e) =>
            {
                if (drag == null || drag.Host != host)
                    return;
                e.Handled = true;
                var session = drag;
                UpdateDrag(e.GetPosition(tilesPanel));
                var commit = ReferenceEquals(drag, session) && session.IsDragging && session.IsValid;
                var destination = session.DashboardTarget == null ? (DashboardKey?)null : (DashboardKey)session.DashboardTarget.Tag;
                CancelDrag();
                if (commit)
                {
                    if (destination.HasValue)
                        await MoveTileAsync(menu, destination.Value);
                    else
                        await SavePlacementAsync(host, session.Candidate);
                }
            };
            host.LostMouseCapture += (s, e) => { if (drag != null && drag.Host == host && Mouse.Captured != host) CancelDrag(); };
            host.QueryCursor += (s, e) =>
            {
                if (!CanOpenTile(menu, e.OriginalSource as DependencyObject))
                    return;
                e.Cursor = System.Windows.Input.Cursors.Hand;
                e.Handled = true;
            };
            host.MouseLeftButtonUp += (s, e) =>
            {
                if (editMode || savingPlacement || resize != null || IsInteractiveElement(e.OriginalSource as DependencyObject))
                    return;
                var filters = control.GetFilterPair(menu._Filter);
                AddDockItem(menu.Control, null, null, null, true, null, filters);
                e.Handled = true;
            };
            panel.Children.Add(host);

            /* reload plugin tiles if not available yet */
            if (definition.IsCustomTile && !Uniconta.ClientTools.Plugin.DisableAllPlugins &&
               Uniconta.ClientTools.Plugin.TileTypeCache.Resolve(definition.TileTypeId) == null)
                _ = ReloadPluginTileAfterDelayAsync(menu, host, control);
        }
        private bool CanOpenTile(UserMenuClient menu, DependencyObject source)
            => !editMode && !savingPlacement && resize == null 
            && !string.IsNullOrWhiteSpace(menu.Control) && !IsInteractiveElement(source);

        private const int PluginTileReloadDelaySeconds = 3;
        private async Task ReloadPluginTileAfterDelayAsync(UserMenuClient menu, TileHost host, TileControl control)
        {
            try
            {
                // User plugin types may still be registering when the dashboard first opens.
                await Task.Delay(TimeSpan.FromSeconds(PluginTileReloadDelaySeconds));
                if (pageClosing || !hosts.TryGetValue(menu, out var currentHost) || currentHost != host)
                    return;

                await control.LoadAsync();
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex.Message);
            }
        }

        private void ShowTileMenu(TileControl control, UserMenuClient menu)
        {
            var contextMenu = new ContextMenu();
            var edit = new MenuItem { Header = Uniconta.ClientTools.Localization.lookup("Edit") };
            edit.Click += (sender, args) => { if (!savingPlacement && !pageClosing) control.EditTile(); };
            contextMenu.Items.Add(edit);

            var move = new MenuItem { Header = Uniconta.ClientTools.Localization.lookup("TileMoveTo") };
            foreach (var dashboard in GetDashboards().Where(x => !x.Equals(selectedDashboard)))
            {
                var destination = dashboard;
                var item = new MenuItem { Header = destination.DisplayName, IsEnabled = CanAddTilesToDashboard(destination) };
                item.Click += async (sender, args) => await MoveTileAsync(menu, destination);
                move.Items.Add(item);
            }

            move.IsEnabled = move.Items.Cast<MenuItem>().Any(item => item.IsEnabled);
            contextMenu.Items.Add(move);

            contextMenu.Items.Add(new Separator());

            var delete = new MenuItem
            {
                Header = Uniconta.ClientTools.Localization.lookup("Delete")
            };

            delete.Click += async (sender, args) =>
            {
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    return;

                var result = await RemoveTileAsync(menu);

                if (result != ErrorCodes.Succes)
                    Uniconta.ClientTools.InfoLog.Log.Error(result);
            };

            contextMenu.Items.Add(delete);

            contextMenu.PlacementTarget = control;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            contextMenu.IsOpen = true;
        }

        private async Task MoveTileAsync(UserMenuClient menu, DashboardKey destination)
        {
            if (savingPlacement || !CanAddTilesToDashboard(destination))
                return;
            CancelInteractions();

            var definition = TileDefinition.FromUserMenu(menu);
            var original = new UserMenuClient();
            original.Copy(menu);

            definition.GridColumn = definition.GridRow = null;
            definition.Scope = destination.Scope;
            definition.Profile = destination.Profile;
            definition.DashboardTab = destination.Name;
            definition.Order = tiles.Where(t => !TileDefinition.FromUserMenu(t).IsDashboardDefinition)
                .Where(t => DashboardKey.From(t).Equals(destination))
                .Select(t => t._LineNumber)
                .DefaultIfEmpty()
                .Max() + 1;

            try
            {
                var result = await new TileCRUDController(api).SaveAsync(definition, menu);
                if (result.Item1 == ErrorCodes.Succes)
                {
                    await ReloadTilesAsync();
                    return;
                }
                Uniconta.ClientTools.InfoLog.Log.Error(result.Item1);
            }
            catch (Exception ex) { Uniconta.ClientTools.InfoLog.Log.Error(ex.Message); }
            menu.Copy(original);
        }

        private void BeginDrag(TileHost host, MouseButtonEventArgs e)
        {
            if (!editMode || savingPlacement || resize != null || pageClosing)
                return;
            tilesPanel.UpdateLayout();
            TileGridRect original;
            if (tilesPanel.CurrentPlan == null || !tilesPanel.CurrentPlan.TryGetValue(host, out original))
                return;
            CancelDrag();
            var pointer = e.GetPosition(tilesPanel);
            drag = new DragSession
            {
                Host = host,
                Original = original,
                Candidate = original,
                Start = pointer,
                Offset = new System.Windows.Point(pointer.X - original.X, pointer.Y - original.Y),
                Columns = tilesPanel.AvailableColumns,
                IsValid = true
            };
            host.Focus();
            if (!host.CaptureMouse())
                CancelDrag();
            e.Handled = true;
        }

        private void TileHost_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (drag == null || drag.Host != sender)
                return;
            if (!editMode || e.LeftButton != MouseButtonState.Pressed || drag.Columns != tilesPanel.AvailableColumns)
            {
                CancelDrag();
                return;
            }
            UpdateDrag(e.GetPosition(tilesPanel));
            e.Handled = true;
        }

        private void UpdateDrag(System.Windows.Point pointer)
        {
            if (drag == null)
                return;
            if (!editMode || drag.Columns != tilesPanel.AvailableColumns)
            {
                CancelDrag();
                return;
            }
            if (!drag.IsDragging && Math.Abs(pointer.X - drag.Start.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pointer.Y - drag.Start.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;
            drag.IsDragging = true;
            // Mouse capture keeps events on the tile, so hit-test the tabs explicitly.
            var hit = dashboardTabs.InputHitTest(tilesPanel.TranslatePoint(pointer, dashboardTabs)) as DependencyObject;
            while (hit != null && !(hit is System.Windows.Controls.Button) && hit != dashboardTabs)
                hit = VisualTreeHelper.GetParent(hit);
            var target = hit as System.Windows.Controls.Button;
            if (!dashboardDropTargets.Contains(target))
                target = null;
            SetDashboardDragTarget(target);
            if (target != null)
            {
                var destination = (DashboardKey)target.Tag;
                drag.IsValid = !destination.Equals(selectedDashboard) && CanAddTilesToDashboard(destination);
                if (drag.IsValid)
                {
                    target.Opacity = 1;
                    target.BorderThickness = new Thickness(0, 0, 0, 2);
                }
                HideResizeVisual();
                return;
            }
            drag.Candidate = tileLayout.SnapPosition(pointer.X - drag.Offset.X, pointer.Y - drag.Offset.Y,
                new TileGridSize(drag.Original.ColumnSpan, drag.Original.RowSpan), drag.Columns);
            drag.IsValid = tileLayout.CanPlace(tilesPanel.CurrentPlan, drag.Host, drag.Candidate, drag.Columns);
            ShowPlacementPreview(drag.Candidate, drag.IsValid);
        }

        private void SetDashboardDragTarget(System.Windows.Controls.Button target)
        {
            if (drag.DashboardTarget == target)
                return;
            if (drag.DashboardTarget != null)
            {
                drag.DashboardTarget.ClearValue(UIElement.OpacityProperty);
                drag.DashboardTarget.BorderThickness = ((DashboardKey)drag.DashboardTarget.Tag).Equals(selectedDashboard)
                    ? new Thickness(0, 0, 0, 2) : new Thickness(0);
            }
            drag.DashboardTarget = target;
        }

        private void CancelDrag()
        {
            if (drag != null)
                SetDashboardDragTarget(null);
            var session = drag;
            drag = null;
            if (session != null && Mouse.Captured == session.Host)
                session.Host.ReleaseMouseCapture();
            HideResizeVisual();
        }

        private static bool IsInteractiveElement(DependencyObject value)
        {
            while (value != null)
            {
                if (value is System.Windows.Controls.Button || value is System.Windows.Controls.Primitives.Thumb)
                    return true;
                value = VisualTreeHelper.GetParent(value);
            }
            return false;
        }

        private void BeginResize(TileHost host, TileGridPanel panel, TileControl control, UserMenuClient menu)
        {
            if (!editMode || savingPlacement || pageClosing)
                return;
            CancelInteractions();
            panel.UpdateLayout();
            TileGridRect original;
            if (panel.CurrentPlan == null || !panel.CurrentPlan.TryGetValue(host, out original))
                return;
            resize = new ResizeSession
            {
                Host = host,
                Control = control,
                Original = original,
                Candidate = original,
                Start = Mouse.GetPosition(panel),
                Columns = panel.AvailableColumns,
                IsValid = true
            };
            ShowPlacementPreview(original, true);
        }

        private void UpdateResize()
        {
            if (resize == null)
                return;
            if (!editMode || resize.Columns != tilesPanel.AvailableColumns)
            {
                CancelResize();
                return;
            }
            var pointer = Mouse.GetPosition(tilesPanel);
            var size = tileLayout.Snap(resize.Original.Width + pointer.X - resize.Start.X,
                resize.Original.Height + pointer.Y - resize.Start.Y, resize.Columns);
            resize.Candidate = tileLayout.GetRect(resize.Original.Column, resize.Original.Row, size.ColumnSpan, size.RowSpan);
            resize.IsValid = tileLayout.CanPlace(tilesPanel.CurrentPlan, resize.Host, resize.Candidate, resize.Columns);
            ShowPlacementPreview(resize.Candidate, resize.IsValid);
            resize.Control.ShowResizePreview(size.ColumnSpan, size.RowSpan);
        }

        private void ShowPlacementPreview(TileGridRect candidate, bool valid)
        {
            var origin = tilesPanel.TranslatePoint(new System.Windows.Point(candidate.X, candidate.Y), resizeOverlay);
            Canvas.SetLeft(resizeGhost, origin.X);
            Canvas.SetTop(resizeGhost, origin.Y);
            resizeGhost.Width = candidate.Width;
            resizeGhost.Height = candidate.Height;
            resizeGhost.Opacity = valid ? 0.72 : 0.35;
            resizeGhostText.Text = candidate.ColumnSpan + " × " + candidate.RowSpan + (valid ? string.Empty : " · " + Uniconta.ClientTools.Localization.lookup("TileInvalidPlacement"));
            resizeGhost.Visibility = Visibility.Visible;
        }

        private async Task CommitResizeAsync()
        {
            var session = resize;
            if (session == null)
                return;
            UpdateResize();
            if (!ReferenceEquals(resize, session))
                return;
            CancelResize();
            if (session.IsValid && (session.Candidate.ColumnSpan != session.Original.ColumnSpan || session.Candidate.RowSpan != session.Original.RowSpan))
                await SavePlacementAsync(session.Host, session.Candidate, true);
        }

        private async Task<ErrorCodes> SavePlacementAsync(TileHost target, TileGridRect candidate, bool changeSize = false, bool forceSize = false)
        {
            if (savingPlacement || pageClosing || target.Parent != tilesPanel)
                return ErrorCodes.NoChange;
            tilesPanel.UpdateLayout();
            var plan = tilesPanel.CurrentPlan;
            if (plan == null || !tileLayout.CanPlace(plan, target, candidate, tilesPanel.AvailableColumns))
                return ErrorCodes.InvalidObject;
            var changes = new List<PlacementChange>();
            foreach (var pair in hosts)
            {
                var definition = TileDefinition.FromUserMenu(pair.Key);
                var original = plan[pair.Value];
                var rect = pair.Value == target ? candidate : original;
                var columnSpan = pair.Value == target && changeSize && (forceSize || candidate.ColumnSpan != original.ColumnSpan) ? candidate.ColumnSpan : definition.ColumnSpan;
                var rowSpan = pair.Value == target && changeSize && (forceSize || candidate.RowSpan != original.RowSpan) ? candidate.RowSpan : definition.RowSpan;
                if (definition.GridColumn == rect.Column && definition.GridRow == rect.Row && definition.ColumnSpan == columnSpan && definition.RowSpan == rowSpan)
                    continue;
                var originalMenu = new UserMenuClient();
                originalMenu.Copy(pair.Key);
                changes.Add(new PlacementChange { Menu = pair.Key, Host = pair.Value, Original = definition, OriginalMenu = originalMenu });
                definition = TileDefinition.FromUserMenu(pair.Key);
                definition.GridColumn = rect.Column;
                definition.GridRow = rect.Row;
                definition.ColumnSpan = columnSpan;
                definition.RowSpan = rowSpan;
                changes[changes.Count - 1].Updated = definition;
            }
            if (changes.Count == 0)
                return ErrorCodes.Succes;
            // Editing adopts visible fallback positions as well as legacy positions, so neighbors stay fixed on reload.
            SetPlacementBusy(true);
            var error = ErrorCodes.Exception;
            try
            {
                foreach (var change in changes)
                {
                    change.Updated.ApplyTo(change.Menu);
                    ApplyPlacement(change.Host, change.Updated);
                }
                tilesPanel.UpdateLayout();
                error = await api.MultiCrud(null, changes.Select(change => change.Menu).ToList(), null);
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(ex.Message);
            }
            finally
            {
                if (error != ErrorCodes.Succes)
                {
                    foreach (var change in changes)
                    {
                        change.Menu.Copy(change.OriginalMenu);
                        if (!pageClosing)
                            ApplyPlacement(change.Host, change.Original);
                    }
                }
                SetPlacementBusy(false);
                if (pageClosing)
                    UserMenuItems.Load(api);
            }
            if (error != ErrorCodes.Succes)
                Uniconta.ClientTools.InfoLog.Log.Error(error);
            return error;
        }

        private static void ApplyPlacement(TileHost host, TileDefinition definition)
        {
            TileGridPanel.SetGridColumn(host, definition.GridColumn ?? -1);
            TileGridPanel.SetGridRow(host, definition.GridRow ?? -1);
            TileGridPanel.SetColumnSpan(host, definition.ColumnSpan);
            TileGridPanel.SetRowSpan(host, definition.RowSpan);
            var current = ((TileControl)host.Content).Definition;
            current.GridColumn = definition.GridColumn;
            current.GridRow = definition.GridRow;
            current.ColumnSpan = definition.ColumnSpan;
            current.RowSpan = definition.RowSpan;
        }

        private void SetPlacementBusy(bool value)
        {
            savingPlacement = value;
            if (pageClosing)
                return;
            tilesPanel.IsEnabled = dashboardTabs.IsEnabled = !value;
            btnRefresh.IsEnabled = btnEdit.IsEnabled = !value;
            UpdateTileCreationAvailability();
        }

        private void CancelResize()
        {
            var session = resize;
            resize = null;
            if (session != null)
                session.Control.CancelResizeInteraction();
            HideResizeVisual();
        }

        private void CancelInteractions()
        {
            CancelDrag();
            CancelResize();
        }

        void HideResizeVisual() { resizeGhost.Visibility = Visibility.Collapsed; }

        void TileViewport_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.HorizontalChange != 0 || e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0)
                CancelInteractions();
            else if (e.VerticalChange != 0)
            {
                UpdateDrag(Mouse.GetPosition(tilesPanel));
                UpdateResize();
            }
        }

        void TilePage_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape && (resize != null || drag != null))
            {
                CancelInteractions();
                e.Handled = true;
            }
        }

        public override void PageClosing()
        {
            pageClosing = true;
            CancelInteractions();
            Loaded -= TilePage_Loaded;
            IsVisibleChanged -= TilePage_IsVisibleChanged;
            PreviewKeyDown -= TilePage_PreviewKeyDown;

            if (!savingPlacement)
                UserMenuItems.Load(api);

            base.PageClosing();
        }

        int AvailableColumns { get { return Math.Max(1, tilesPanel.AvailableColumns == 0 ? tileLayout.GetColumnCount(tileSurface.ActualWidth) : tilesPanel.AvailableColumns); } }

        private string GetGreeting(string userName)
        {
            var h = DateTime.Now.Hour;
            var key = h < 12 ? "GoodMorning" : h < 17 ? "GoodAfternoon" : "GoodEvening";
            return Uniconta.ClientTools.Localization.lookup(key) + ", " + userName;
        }

        void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement || pageClosing)
                return;
            CancelInteractions();

            var dialog = new CWTileDetails(api, new UserMenuClient());

            dialog.Closing += delegate
            {
                if (dialog.DialogResult == true)
                {
                    selectedDashboard = DashboardKey.From(dialog.userMenu);
                    if (dialog.userMenu._LineNumber == 0)
                    {
                        dialog.userMenu._LineNumber = tiles
                            .Where(t => DashboardKey.From(t).Equals(selectedDashboard))
                            .Select(t => t._LineNumber)
                            .DefaultIfEmpty()
                            .Max() + 1;
                    }

                    tiles.Add(dialog.userMenu);
                    RebuildTiles();
                }
            };

            dialog.Show();
        }

        async void btnRefresh_Click(object sender, RoutedEventArgs e) => await ReloadTilesAsync();

        async void btnCopyToApp_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement || pageClosing || !btnCopyToApp.IsEnabled)
                return;

            // Include the dashboard definition along with the selected dashboard's tiles.
            var copyFrom = tiles.Where(t => DashboardKey.From(t).Equals(selectedDashboard)).ToList();
            if (copyFrom.Count == 0 || !Utility.ConfirmCopyToApp())
                return;

            var currentApp = Utility.GetCurrentApp();
            if (currentApp == null)
                return;

            btnCopyToApp.IsEnabled = false;
            try
            {
                var copies = new List<AppUserMenuClient>();
                foreach (var tile in copyFrom)
                {
                    var appTile = new AppUserMenuClient();
                    appTile.SetMaster(tile);
                    appTile.SetMaster(currentApp);
                    copies.Add(appTile);
                }

                var err = await api.Insert(copies);
                UtilDisplay.ShowErrorCode(err);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnCopyToApp.IsEnabled = true;
            }
        }

        async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement || pageClosing || reloadTilesTask != null)
                return;
            CancelInteractions();
            
            if (!editMode && !CanAddTilesToDashboard(selectedDashboard))
                return;
            var defaultsToSave = defaultTiles.Where(tile => tiles.Contains(tile) && CanAddTilesToDashboard(DashboardKey.From(tile))).ToList();
            if (!editMode && defaultsToSave.Count != 0)
            {
                savingPlacement = true;
                UpdateTileCreationAvailability();
                try
                {
                    var result = await api.Insert(defaultsToSave);
                    if (result != ErrorCodes.Succes)
                    {
                        UtilDisplay.ShowErrorCode(result);
                        return;
                    }
                    foreach (var tile in defaultsToSave)
                    {
                        TileCRUDController.UpdateTileCache(api.CompanyEntity, tile);
                        defaultTiles.Remove(tile);
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                finally { savingPlacement = false; UpdateTileCreationAvailability(); }
            }
            editMode = !editMode;
            btnEdit.Content = editMode ? Uniconta.ClientTools.Localization.lookup("Save") : Uniconta.ClientTools.Localization.lookup("TileEditLayout");
            ButtonAppearance.SetGlyph(btnEdit, GlyphHelper.GetGlyph(editMode ? "save-32x32.svg" : "Edit_16x16.svg"));
            tilesPanel.IsEditing = editMode;
            emptyDashboard.Visibility = hosts.Count == 0 && !editMode ? Visibility.Visible : Visibility.Collapsed;

            foreach (var host in hosts.Values)
                ((TileControl)host.Content).IsDeleteMode = editMode;

            RebuildDashboardTabs();
        }

        static List<TileDefinition> ReadTileDefinitions(string json)
        {
            var definitions = JsonConvert.DeserializeObject<List<TileDefinition>>(json,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
            if (definitions == null || definitions.Any(definition => definition == null))
                throw new JsonSerializationException("Expected an array of tile definitions.");
            foreach (var definition in definitions)
            {
                if (definition.Version > TileDefinition.CurrentVersion)
                    throw new JsonSerializationException("This tile format requires a newer application version.");
                definition.Validate();
            }
            return definitions;
        }

        void LoadDefaultTilesIfEmpty()
        {
            defaultTiles.Clear();
            LoadDefaultTilesIfEmpty(TileScope.Company, "DefaultTiles_Company.json");
            LoadDefaultTilesIfEmpty(TileScope.User, "DefaultTiles_User.json");
        }

        void LoadDefaultTilesIfEmpty(TileScope scope, string resourceName)
        {
            // Check stored records as well, so hidden/disabled tiles do not trigger defaults.
            var storedTiles = api.CompanyEntity.userMenuDefs;
            bool hasStoredTiles = storedTiles?.Any(menu => menu._IsTile &&
                (scope == TileScope.Company
                    ? menu._IsCompanyTile && string.IsNullOrWhiteSpace(menu._Profile)
                    : menu._Uid == api.session.User.Uid && !menu._IsCompanyTile)) == true;
            if (hasStoredTiles || tiles.Any(tile => TileDefinition.FromUserMenu(tile).Scope == scope))
                return;

            using (var stream = typeof(TilePage).Assembly.GetManifestResourceStream("UnicontaClient." + resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("Default tile resource is missing: " + resourceName);
                using (var reader = new StreamReader(stream))
                {
                    foreach (var definition in ReadTileDefinitions(reader.ReadToEnd()))
                    {
                        definition.Scope = scope;
                        definition.Profile = null;
                        var menu = new UserMenuClient();
                        menu.CompanyId = api.CompanyId;
                        definition.ApplyTo(menu);
                        menu._Uid = scope == TileScope.User ? api.session.User.Uid : 0;
                        tiles.Add(menu);
                        defaultTiles.Add(menu);
                    }
                }
            }
        }

        void btnExport_Click(object sender, RoutedEventArgs e)
        {
            if (!editMode || savingPlacement || pageClosing) return;
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "JSON files (*.json)|*.json", DefaultExt = ".json", FileName = "Dashboard.json" };
            if (dialog.ShowDialog() != true) return;
            try
            {
                File.WriteAllText(dialog.FileName, JsonConvert.SerializeObject(
                    tiles.Where(tile => DashboardKey.From(tile).Equals(selectedDashboard)).Select(TileDefinition.FromUserMenu), Formatting.Indented));
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        async void btnImport_Click(object sender, RoutedEventArgs e)
        {
            if (!editMode || savingPlacement || pageClosing || !CanAddTilesToDashboard(selectedDashboard)) return;
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON files (*.json)|*.json", DefaultExt = ".json" };
            if (dialog.ShowDialog() != true) return;
            CancelInteractions();
            savingPlacement = true;
            UpdateTileCreationAvailability();
            try
            {
                var definitions = ReadTileDefinitions(File.ReadAllText(dialog.FileName));
                var imported = new List<UserMenuClient>();
                foreach (var definition in definitions)
                {
                    // Import adds tiles to the selected dashboard; never copies record IDs or ownership.
                    if (definition.IsDashboardDefinition) continue;
                    definition.Scope = selectedDashboard.Scope;
                    definition.Profile = selectedDashboard.Profile;
                    definition.DashboardTab = selectedDashboard.Name;
                    definition.Validate();
                    if (definition.IsCustomTile) TileMetadataService.ResolveStandaloneTileType(definition.TileTypeId);
                    var rowType = definition.IsCustomTile || definition.Visualization == TileVisualization.HtmlWidget ? null : string.IsNullOrWhiteSpace(definition.SourceName)
                        ? UtilDisplay.GetTypeFromTableId(api.CompanyEntity, definition.TableId)
                        : TileDataSource.Create(api, definition).DataType;
                    TileTableCapabilities.Validate(rowType, definition);
                    var menu = new UserMenuClient();
                    definition.ApplyTo(menu);
                    menu._Uid = definition.Scope == TileScope.User ? api.session.User.Uid : 0;
                    imported.Add(menu);
                }
                if (imported.Count == 0) return;
                var result = await api.Insert(imported);
                if (result != ErrorCodes.Succes) { UtilDisplay.ShowErrorCode(result); return; }
                foreach (var menu in imported) TileCRUDController.UpdateTileCache(api.CompanyEntity, menu);
                tiles.AddRange(imported);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                savingPlacement = false;
                RebuildTiles();
                UpdateLastUpdated();
            }
        }

        async void btnRefreshTiles_Click(object sender, RoutedEventArgs e) => await ReloadTilesAsync();

        void btnCreateWithAI_Click(object sender, RoutedEventArgs e)
        {
            if (savingPlacement || pageClosing)
                return;
            if(App.MainPageObj.assistantHost is AIAssistantHost assistantHost)
            {
                assistantHost.SetActiveConsumer(this);
                assistantHost.Show();

            }
        }

        public FrameworkElement AIAdornedElement { get { return tileSurface; } }
        public string AIPromptContextName { get { return Uniconta.ClientTools.Localization.lookup("TileDashboardTiles"); } }

        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool attached)
        {
            var value = base.GetAIAssistantCapabilities(attached);
            value.Add(new AddTileAICapability(this));
            value.Add(new RemoveTileAICapability(this));
            value.Add(new ResizeTileAICapability(this));
            return value;
        }

        public override IEnumerable<IAIAssistantDataProvider> GetAIAssistantDataProviders()
        {
            return base.GetAIAssistantDataProviders().Concat(new IAIAssistantDataProvider[]
            {
            new TilePageDataProvider(this),
            new TileTypesDataProvider(this),
            new TileDataSourcesDataProvider(this),
            new TileFieldsDataProvider(this)
            });
        }

        public List<string> GetAIAssistantPromptSuggestions()
        {
            return new List<string>
        {
            Uniconta.ClientTools.Localization.lookup("TileWhatNeedsMyAttentionOnThisScreen"),
            Uniconta.ClientTools.Localization.lookup("TileAddAKPIForTotalRevenue"),
            Uniconta.ClientTools.Localization.lookup("TileRemoveTheInvoiceCountTile")
        };
        }

        internal IReadOnlyList<UserMenuClient> CurrentTiles { get { return tiles.Where(t => !TileDefinition.FromUserMenu(t).IsDashboardDefinition && DashboardKey.From(t).Equals(selectedDashboard)).ToList(); } }
        internal int AvailableTileColumns { get { return AvailableColumns; } }

        internal async Task<ErrorCodes> AddTileAsync(TileDefinition definition)
        {
            definition.NormalizeNewDashboard();
            var destination = new DashboardKey(definition.Scope, definition.DashboardTab, definition.Profile);
            if (savingPlacement || pageClosing || !CanAddTilesToDashboard(destination))
                return ErrorCodes.NoChange;
            CancelInteractions();

            var r = await new TileCRUDController(api).SaveAsync(definition);

            if (r.Item1 == ErrorCodes.Succes)
            {
                tiles.Add(r.Item2);
                selectedDashboard = DashboardKey.From(r.Item2);
                RebuildTiles();
            }

            return r.Item1;
        }

        internal async Task<ErrorCodes> RemoveTileAsync(UserMenuClient menu)
        {
            if (savingPlacement || pageClosing)
                return ErrorCodes.NoChange;
            CancelInteractions();

            var r = await new TileCRUDController(api).DeleteAsync(menu);

            if (r == ErrorCodes.Succes)
            {
                tiles.Remove(menu);
                RebuildTiles();
            }

            return r;
        }

        internal Task<ErrorCodes> ResizeTileAsync(UserMenuClient menu, int columns, int rows)
        {
            if (savingPlacement || pageClosing || !CurrentTiles.Contains(menu) || !hosts.ContainsKey(menu))
                return Task.FromResult(ErrorCodes.NoChange);
            CancelInteractions();
            tilesPanel.UpdateLayout();
            var host = hosts[menu];
            TileGridRect original;
            if (tilesPanel.CurrentPlan == null || !tilesPanel.CurrentPlan.TryGetValue(host, out original))
                return Task.FromResult(ErrorCodes.InvalidObject);
            return SavePlacementAsync(host, tileLayout.GetRect(original.Column, original.Row, columns, rows), true, true);
        }

        sealed class ResizeSession
        {
            public TileHost Host;
            public TileControl Control;
            public int Columns;
            public System.Windows.Point Start;
            public TileGridRect Original, Candidate;
            public bool IsValid;
        }

        sealed class DragSession
        {
            public TileHost Host;
            public System.Windows.Controls.Button DashboardTarget;
            public int Columns;
            public System.Windows.Point Start, Offset;
            public TileGridRect Original, Candidate;
            public bool IsDragging, IsValid;
        }

        sealed class PlacementChange
        {
            public UserMenuClient Menu;
            public TileHost Host;
            public TileDefinition Original, Updated;
            public UserMenuClient OriginalMenu;
        }

        sealed class TileHost : ContentControl { }

        sealed class DashboardSelectionState : LastValue.IPackableWithKey
        {
            readonly string packKey;

            public DashboardSelectionState(string packKey) { this.packKey = packKey; }

            string LastValue.IPackableWithKey.PackKey => packKey;
            public TileScope Scope { get; set; } = TileScope.Company;
            public string Name { get; set; }
            public string Profile { get; set; }

            public void InitParmDefault()
            {
                Scope = TileScope.Company;
                Name = null;
                Profile = null;
            }
        }

        struct DashboardKey : IEquatable<DashboardKey>
        {
            public DashboardKey(TileScope scope, string name, string profile = null) { Scope = scope; Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(); Profile = scope == TileScope.Profile ? profile : null; }
            public TileScope Scope { get; private set; }
            public string Profile { get; private set; }
            public string Name { get; private set; }
            public string DisplayName { get { return Name ?? (Scope == TileScope.Company ? Uniconta.ClientTools.Localization.lookup("Company") : Uniconta.ClientTools.Localization.lookup("TileMyDashboard")); } }
            public static DashboardKey From(UserMenuClient menu)
            {
                var definition = TileDefinition.FromUserMenu(menu);
                return new DashboardKey(definition.Scope, definition.DashboardTab, definition.Profile);
            }
            public bool Equals(DashboardKey other) { return Scope == other.Scope && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(Profile, other.Profile, StringComparison.OrdinalIgnoreCase); }
            public override bool Equals(object obj) { return obj is DashboardKey && Equals((DashboardKey)obj); }
            public override int GetHashCode() { return ((int)Scope * 397) ^ (Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name)) ^ (Profile == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Profile)); }
        }
    }

    internal sealed class TileLayoutService
    {
        public const double CellWidth = 180d;
        public const double CellHeight = 130d;
        public const double HorizontalGap = 12d;
        public const double VerticalGap = 12d;
        public const int MaximumRowSpan = 20;

        public int GetColumnCount(double availableWidth)
        {
            if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
                return 1;

            return Math.Max(1, (int)Math.Floor((availableWidth + HorizontalGap) / (CellWidth + HorizontalGap)));
        }

        public TileGridSize Normalize(int columns, int rows, int availableColumns)
        {
            return new TileGridSize(
                Math.Max(1, Math.Min(Math.Max(1, availableColumns), columns)),
                Math.Max(1, Math.Min(MaximumRowSpan, rows)));
        }

        public static double GetSpanLength(int span, double cell, double gap)
        {
            span = Math.Max(1, span);
            return span * cell + (span - 1) * gap;
        }

        public double GetWidth(int columns) { return GetSpanLength(columns, CellWidth, HorizontalGap); }
        public double GetHeight(int rows) { return GetSpanLength(rows, CellHeight, VerticalGap); }

        public TileGridRect GetRect(int column, int row, int columnSpan, int rowSpan)
        {
            return new TileGridRect(column, row, columnSpan, rowSpan,
                column * (CellWidth + HorizontalGap), row * (CellHeight + VerticalGap), GetWidth(columnSpan), GetHeight(rowSpan));
        }

        public TileGridRect SnapPosition(double x, double y, TileGridSize size, int availableColumns)
        {
            return GetRect(SnapCoordinate(x, CellWidth + HorizontalGap), SnapCoordinate(y, CellHeight + VerticalGap), size.ColumnSpan, size.RowSpan);
        }

        static int SnapCoordinate(double value, double pitch)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return int.MinValue;
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(value / pitch, MidpointRounding.AwayFromZero)));
        }

        public bool CanPlace(TileLayouts plan, object excluded, TileGridRect candidate, int availableColumns)
        {
            if (availableColumns < 1 || candidate.Column < 0 || candidate.Row < 0 || candidate.ColumnSpan < 1 || candidate.RowSpan < 1 ||
                candidate.RowSpan > MaximumRowSpan || (long)candidate.Column + candidate.ColumnSpan > availableColumns || (long)candidate.Row + candidate.RowSpan > int.MaxValue)
                return false;
            foreach (var entry in plan)
            {
                if (Equals(entry.Key, excluded))
                    continue;
                var rect = entry.Value;
                if (candidate.Column < (long)rect.Column + rect.ColumnSpan && (long)candidate.Column + candidate.ColumnSpan > rect.Column &&
                    candidate.Row < (long)rect.Row + rect.RowSpan && (long)candidate.Row + candidate.RowSpan > rect.Row)
                    return false;
            }
            return true;
        }

        public TileGridSize Snap(double width, double height, int availableColumns)
        {
            return new TileGridSize(
                Math.Max(1, SnapCoordinate(Math.Max(0, width) + HorizontalGap, CellWidth + HorizontalGap)),
                Math.Max(1, Math.Min(MaximumRowSpan, SnapCoordinate(Math.Max(0, height) + VerticalGap, CellHeight + VerticalGap))));
        }

        public TileGridSize Resize(int columns, int rows, double horizontalChange, double verticalChange, int availableColumns)
        {
            return Snap(GetWidth(columns) + horizontalChange, GetHeight(rows) + verticalChange, availableColumns);
        }

        public TileLayouts CreatePlan(IEnumerable<TileLayoutItem> items, int availableColumns)
        {
            availableColumns = Math.Max(1, availableColumns);
            var plan = new TileLayouts();
            var pending = new List<TileLayoutItem>();

            foreach (var item in items)
            {
                var size = Normalize(item.ColumnSpan, item.RowSpan, availableColumns);
                if (item.Column.HasValue && item.Row.HasValue)
                {
                    var saved = GetRect(item.Column.Value, item.Row.Value, size.ColumnSpan, size.RowSpan);
                    if (CanPlace(plan, null, saved, availableColumns))
                    {
                        plan.Add(item.Identity, saved);
                        continue;
                    }
                }
                pending.Add(item);
            }

            // Reserve saved positions before filling legacy tiles, so rebuilding cannot displace an unrelated tile.
            long cursor = 0;
            foreach (var item in pending)
            {
                var size = Normalize(item.ColumnSpan, item.RowSpan, availableColumns);
                var cell = cursor;
                var rect = GetRect((int)(cell % availableColumns), (int)(cell / availableColumns), size.ColumnSpan, size.RowSpan);
                while (!CanPlace(plan, null, rect, availableColumns))
                {
                    cell++;
                    rect = GetRect((int)(cell % availableColumns), (int)(cell / availableColumns), size.ColumnSpan, size.RowSpan);
                }
                plan.Add(item.Identity, rect);
                cursor = cell + 1;
            }
            return plan;
        }
    }

    internal struct TileGridSize : IEquatable<TileGridSize>
    {
        public TileGridSize(int columns, int rows)
        {
            ColumnSpan = columns;
            RowSpan = rows;
        }

        public int ColumnSpan { get; private set; }
        public int RowSpan { get; private set; }

        public bool Equals(TileGridSize other) { return ColumnSpan == other.ColumnSpan && RowSpan == other.RowSpan; }
        public override bool Equals(object value) { return value is TileGridSize && Equals((TileGridSize)value); }
        public override int GetHashCode() { return ColumnSpan * 397 ^ RowSpan; }
    }

    internal struct TileLayoutItem
    {
        public TileLayoutItem(object id, int columns, int rows, int? column = null, int? row = null)
        {
            Identity = id;
            ColumnSpan = columns;
            RowSpan = rows;
            Column = column;
            Row = row;
        }

        public object Identity;
        public int ColumnSpan;
        public int RowSpan;
        public int? Column;
        public int? Row;
    }

    internal struct TileGridRect
    {
        public TileGridRect(int column, int row, int columnSpan, int rowSpan, double x, double y, double width, double height)
        {
            Column = column;
            Row = row;
            ColumnSpan = columnSpan;
            RowSpan = rowSpan;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int Column;
        public int Row;
        public int ColumnSpan;
        public int RowSpan;
        public double X;
        public double Y;
        public double Width;
        public double Height;
    }

    internal sealed class TileLayouts : Dictionary<object, TileGridRect>
    {
        public double Height
        {
            get
            {
                double value = 0;

                foreach (var rect in Values)
                {
                    value = Math.Max(value, rect.Y + rect.Height);
                }

                return value;
            }
        }
    }

    //sharedCRUD controller
    internal sealed class TileCRUDController
    {
        readonly CrudAPI api;

        internal TileCRUDController(CrudAPI api) => this.api = api;

        internal TileDataViewModel Load(UserMenuClient menu, TileScope? scope = null, string dashboardTab = null, string profile = null)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            var working = (UserMenuClient)StreamingManager.Clone(menu);
            var definition = TileDefinition.FromUserMenu(working);
            if (menu.RowId == 0 && scope.HasValue)
            {
                definition.Scope = scope.Value;
                definition.Profile = profile;
                definition.DashboardTab = string.IsNullOrWhiteSpace(dashboardTab) ? null : dashboardTab.Trim();
            }
            return new TileDataViewModel(api, this, menu, working, definition);
        }

        internal async Task<Tuple<ErrorCodes, UserMenuClient>> SaveAsync(TileDataViewModel data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var result = await SaveAsync(data.PrepareForSave(), data.Working);
            if (result.Item1 == ErrorCodes.Succes)
                StreamingManager.Copy(result.Item2, data.Original);
            return result;
        }


        internal async Task<Tuple<ErrorCodes, UserMenuClient>> SaveAsync(TileDefinition definition, UserMenuClient existing = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (existing == null || existing.RowId == 0)
                definition.NormalizeNewDashboard();
            definition.Validate();

            if (definition.IsCustomTile)
                TileMetadataService.ResolveStandaloneTileType(definition.TileTypeId);

            var rowType = definition.IsCustomTile || definition.IsDashboardDefinition || definition.Visualization == TileVisualization.HtmlWidget
                ? null : string.IsNullOrWhiteSpace(definition.SourceName)
                    ? UtilDisplay.GetTypeFromTableId(api.CompanyEntity, definition.TableId)
                    : TileDataSource.Create(api, definition).DataType;

            TileTableCapabilities.Validate(rowType, definition);

            bool scopeChanged = existing != null 
                             && existing._IsUserTile != (definition.Scope == TileScope.User);

            // Preserve the original record for deletion,  a scope change requires a fresh record.
            var menu = existing == null || scopeChanged ? new UserMenuClient() : existing;

            definition.ApplyTo(menu);
            menu._Uid = definition.Scope == TileScope.User ? api.session.User.Uid : 0;

            ErrorCodes error;
            if (scopeChanged && existing.RowId > 0)
            {
                error = await api.Delete(existing);

                if (error != ErrorCodes.Succes)
                    return Tuple.Create(error, existing);

                UpdateTileCache(api.CompanyEntity, existing, deleted: true);
                error = await api.Insert(menu);
            }
            else if (menu.RowId == 0)
            {
                error = await api.Insert(menu);
            }
            else
            {
                error = await api.Update(menu);
            }

            if (error == ErrorCodes.Succes)
                UpdateTileCache(api.CompanyEntity, menu);

            return Tuple.Create(error, menu);
        }


        internal async Task<ErrorCodes> DeleteAsync(UserMenuClient menu)
        {
            if (menu == null)
                throw new ArgumentNullException(nameof(menu));
            var error = await api.Delete(menu);
            if (error == ErrorCodes.Succes) UpdateTileCache(api.CompanyEntity, menu, true);
            return error;
        }

        internal static void UpdateTileCache(Company company, UserMenuClient menu, bool deleted = false)
        {
            // GetTiles returns copies; update the company cache as well as the page's row.
            if (company.userMenuDefs == null)
                company.userMenuDefs = new List<UserMenu>();

            var updated = company.userMenuDefs.Where(item => item.RowId != menu.RowId).ToList();
            if (!deleted)
                updated.Add((UserMenu)StreamingManager.Clone(menu));

            company.userMenuDefs = updated;
        }
    }
}
