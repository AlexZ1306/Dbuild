// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.Settings
{
    using System;
    using Notepads.Services;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;

    public sealed partial class SettingsPanel : Page
    {
        private bool _isPreviewHoldModeActive;
        private double _savedHeaderGridOpacity = 1.0;
        private bool _savedHeaderGridIsHitTestVisible = true;
        private Brush _savedRootBackground;
        private Brush _savedPageBackground;

        public SettingsPanel()
        {
            InitializeComponent();
        }

        public void Show(string title, string tag)
        {
            Type pageType;

            switch (tag)
            {
                case "Personalization":
                    pageType = typeof(PersonalizationSettingsPage);
                    break;
                case "Advanced":
                    pageType = typeof(AdvancedSettingsPage);
                    break;
                case "About":
                    pageType = typeof(AboutPage);
                    break;
                default:
                    pageType = typeof(PersonalizationSettingsPage);
                    break;
            }

            LoggingService.LogInfo($"[{nameof(SettingsPanel)}] Navigating to: {tag} Page", consoleOnly: true);
            TitleTextBlock.Text = title;
            ContentFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
        }

        public void EnterPreviewHoldMode()
        {
            if (_isPreviewHoldModeActive)
            {
                return;
            }

            _isPreviewHoldModeActive = true;
            _savedPageBackground = Background;
            Background = new SolidColorBrush(Colors.Transparent);

            if (SettingsPanelHeaderGrid != null)
            {
                _savedHeaderGridOpacity = SettingsPanelHeaderGrid.Opacity;
                _savedHeaderGridIsHitTestVisible = SettingsPanelHeaderGrid.IsHitTestVisible;
                SettingsPanelHeaderGrid.Opacity = 0;
                SettingsPanelHeaderGrid.IsHitTestVisible = false;
            }

            if (SettingsPanelRootGrid != null)
            {
                _savedRootBackground = SettingsPanelRootGrid.Background;
                SettingsPanelRootGrid.Background = new SolidColorBrush(Colors.Transparent);
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

            if (SettingsPanelHeaderGrid != null)
            {
                SettingsPanelHeaderGrid.Opacity = _savedHeaderGridOpacity;
                SettingsPanelHeaderGrid.IsHitTestVisible = _savedHeaderGridIsHitTestVisible;
            }

            if (SettingsPanelRootGrid != null)
            {
                SettingsPanelRootGrid.Background = _savedRootBackground;
            }
        }
    }
}
