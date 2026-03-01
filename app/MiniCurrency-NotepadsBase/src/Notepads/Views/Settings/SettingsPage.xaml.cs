// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.Settings
{
    using Notepads.Extensions;
    using Notepads.Services;
    using System.Linq;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    public sealed partial class SettingsPage : Page
    {
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
    }
}
