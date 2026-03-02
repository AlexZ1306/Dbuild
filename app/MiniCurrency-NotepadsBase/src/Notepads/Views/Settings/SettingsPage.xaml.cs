// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.Settings
{
    using Notepads.Extensions;
    using Notepads.Services;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class SettingsPage : Page
    {
        private bool _isPreviewHoldModeActive;
        private Brush _savedNavigationBackground;
        private Brush _savedPageBackground;
        private Grid _cachedPaneContentGrid;
        private Brush _savedPaneContentGridBackground;
        private FrameworkElement _cachedSettingsPaneShadowPanel;
        private double _savedSettingsPaneShadowOpacity = 1.0;
        private bool _savedSettingsPaneShadowIsHitTestVisible = true;
        private FrameworkElement _cachedNavigationPaneToggleButton;
        private double _savedNavigationPaneToggleOpacity = 1.0;
        private bool _savedNavigationPaneToggleIsHitTestVisible = true;
        private SplitView _cachedNavigationSplitView;
        private Brush _savedNavigationSplitPaneBackground;
        private readonly Dictionary<NavigationViewItem, (double Opacity, bool IsHitTestVisible)> _savedNavigationMenuItemsState
            = new Dictionary<NavigationViewItem, (double Opacity, bool IsHitTestVisible)>();

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += SettingsPage_Loaded;
            Unloaded += SettingsPage_Unloaded;

            if (App.IsGameBarWidget)
            {
                ThemeSettingsService.SetRequestedTheme(null, Window.Current.Content, null);
            }
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsGameBarWidget)
            {
                ThemeSettingsService.OnThemeChanged += ThemeSettingsService_OnThemeChanged;
                ThemeSettingsService.OnAccentColorChanged += ThemeSettingsService_OnAccentColorChanged;
            }

            var defaultItem = SettingsNavigationView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x => string.Equals(x.Tag as string, "Personalization"));

            if (defaultItem == null)
            {
                defaultItem = SettingsNavigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
            }

            if (defaultItem != null)
            {
                defaultItem.IsSelected = true;
            }
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (App.IsGameBarWidget)
            {
                ThemeSettingsService.OnThemeChanged -= ThemeSettingsService_OnThemeChanged;
                ThemeSettingsService.OnAccentColorChanged -= ThemeSettingsService_OnAccentColorChanged;
            }
        }

        private async void ThemeSettingsService_OnAccentColorChanged(object sender, Color color)
        {
            await Dispatcher.CallOnUIThreadAsync(ThemeSettingsService.SetRequestedAccentColor);
        }

        private async void ThemeSettingsService_OnThemeChanged(object sender, ElementTheme theme)
        {
            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                ThemeSettingsService.SetRequestedTheme(null, Window.Current.Content, null);
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            switch (e.Parameter)
            {
                case null:
                    return;
            }
        }

        private void SettingsPanel_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            SettingsPanel.Show((args.InvokedItem as string), (args.InvokedItemContainer as NavigationViewItem)?.Tag as string);
        }

        public void EnterPreviewHoldMode()
        {
            if (_isPreviewHoldModeActive)
            {
                return;
            }

            _isPreviewHoldModeActive = true;
            _savedPageBackground = Background;
            _savedNavigationBackground = SettingsNavigationView.Background;
            Background = new SolidColorBrush(Colors.Transparent);
            SettingsNavigationView.Background = new SolidColorBrush(Colors.Transparent);
            SetNavigationMenuItemsPreviewVisibility(visible: false);
            HideNavigationPaneToggleButton();
            MakeNavigationPaneBackgroundTransparent();

            SettingsPanel?.EnterPreviewHoldMode();

            _cachedPaneContentGrid = FindPaneContentGridAncestor();
            if (_cachedPaneContentGrid != null)
            {
                _savedPaneContentGridBackground = _cachedPaneContentGrid.Background;
                _cachedPaneContentGrid.Background = new SolidColorBrush(Colors.Transparent);
            }

            _cachedSettingsPaneShadowPanel = FindSettingsPaneShadowPanel();
            if (_cachedSettingsPaneShadowPanel != null)
            {
                _savedSettingsPaneShadowOpacity = _cachedSettingsPaneShadowPanel.Opacity;
                _savedSettingsPaneShadowIsHitTestVisible = _cachedSettingsPaneShadowPanel.IsHitTestVisible;
                _cachedSettingsPaneShadowPanel.Opacity = 0;
                _cachedSettingsPaneShadowPanel.IsHitTestVisible = false;
            }
        }

        public void ExitPreviewHoldMode()
        {
            if (!_isPreviewHoldModeActive)
            {
                return;
            }

            _isPreviewHoldModeActive = false;
            Background = _savedPageBackground;
            SettingsNavigationView.Background = _savedNavigationBackground;
            SetNavigationMenuItemsPreviewVisibility(visible: true);
            RestoreNavigationPaneToggleButton();
            RestoreNavigationPaneBackground();
            SettingsPanel?.ExitPreviewHoldMode();

            if (_cachedPaneContentGrid != null)
            {
                _cachedPaneContentGrid.Background = _savedPaneContentGridBackground;
            }

            if (_cachedSettingsPaneShadowPanel != null)
            {
                _cachedSettingsPaneShadowPanel.Opacity = _savedSettingsPaneShadowOpacity;
                _cachedSettingsPaneShadowPanel.IsHitTestVisible = _savedSettingsPaneShadowIsHitTestVisible;
            }
        }

        private Grid FindPaneContentGridAncestor()
        {
            var current = this as DependencyObject;
            while (current != null)
            {
                if (current is Grid grid && string.Equals(grid.Name, "PaneContentGrid"))
                {
                    return grid;
                }

                current = GetParentDependencyObject(current);
            }

            if (!(Window.Current?.Content is DependencyObject root))
            {
                return null;
            }

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is Grid namedGrid && string.Equals(namedGrid.Name, "PaneContentGrid"))
                {
                    return namedGrid;
                }

                var childrenCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childrenCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return null;
        }

        private static DependencyObject GetParentDependencyObject(DependencyObject current)
        {
            if (current == null)
            {
                return null;
            }

            var visualParent = VisualTreeHelper.GetParent(current);
            if (visualParent != null)
            {
                return visualParent;
            }

            if (current is FrameworkElement frameworkElement &&
                frameworkElement.Parent is DependencyObject logicalParent)
            {
                return logicalParent;
            }

            if (current is Page page && page.Frame != null)
            {
                return page.Frame;
            }

            if (current is Frame frame && frame.Parent is DependencyObject frameParent)
            {
                return frameParent;
            }

            return null;
        }

        private FrameworkElement FindSettingsPaneShadowPanel()
        {
            if (!(Window.Current?.Content is DependencyObject root))
            {
                return null;
            }

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is FrameworkElement element &&
                    string.Equals(element.Name, "SettingsPaneShadowPanel"))
                {
                    return element;
                }

                var childrenCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childrenCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return null;
        }

        private void SetNavigationMenuItemsPreviewVisibility(bool visible)
        {
            foreach (var menuItem in SettingsNavigationView.MenuItems.OfType<NavigationViewItem>())
            {
                if (visible)
                {
                    if (_savedNavigationMenuItemsState.TryGetValue(menuItem, out var savedState))
                    {
                        menuItem.Opacity = savedState.Opacity;
                        menuItem.IsHitTestVisible = savedState.IsHitTestVisible;
                    }

                    continue;
                }

                if (!_savedNavigationMenuItemsState.ContainsKey(menuItem))
                {
                    _savedNavigationMenuItemsState[menuItem] = (menuItem.Opacity, menuItem.IsHitTestVisible);
                }

                menuItem.Opacity = 0;
                menuItem.IsHitTestVisible = false;
            }

            if (visible)
            {
                _savedNavigationMenuItemsState.Clear();
            }
        }

        private void HideNavigationPaneToggleButton()
        {
            _cachedNavigationPaneToggleButton = FindDescendantByName(SettingsNavigationView, "PaneToggleButton");
            if (_cachedNavigationPaneToggleButton == null)
            {
                return;
            }

            _savedNavigationPaneToggleOpacity = _cachedNavigationPaneToggleButton.Opacity;
            _savedNavigationPaneToggleIsHitTestVisible = _cachedNavigationPaneToggleButton.IsHitTestVisible;
            _cachedNavigationPaneToggleButton.Opacity = 0;
            _cachedNavigationPaneToggleButton.IsHitTestVisible = false;
        }

        private void RestoreNavigationPaneToggleButton()
        {
            if (_cachedNavigationPaneToggleButton == null)
            {
                return;
            }

            _cachedNavigationPaneToggleButton.Opacity = _savedNavigationPaneToggleOpacity;
            _cachedNavigationPaneToggleButton.IsHitTestVisible = _savedNavigationPaneToggleIsHitTestVisible;
        }

        private void MakeNavigationPaneBackgroundTransparent()
        {
            _cachedNavigationSplitView = FindFirstDescendant<SplitView>(SettingsNavigationView);
            if (_cachedNavigationSplitView == null)
            {
                return;
            }

            _savedNavigationSplitPaneBackground = _cachedNavigationSplitView.PaneBackground;
            _cachedNavigationSplitView.PaneBackground = new SolidColorBrush(Colors.Transparent);
        }

        private void RestoreNavigationPaneBackground()
        {
            if (_cachedNavigationSplitView == null)
            {
                return;
            }

            _cachedNavigationSplitView.PaneBackground = _savedNavigationSplitPaneBackground;
        }

        private static FrameworkElement FindDescendantByName(DependencyObject root, string name)
        {
            if (root == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is FrameworkElement element && string.Equals(element.Name, name))
                {
                    return element;
                }

                var childrenCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childrenCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return null;
        }

        private static T FindFirstDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is T target)
                {
                    return target;
                }

                var childrenCount = VisualTreeHelper.GetChildrenCount(node);
                for (var i = 0; i < childrenCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(node, i));
                }
            }

            return null;
        }
    }
}
