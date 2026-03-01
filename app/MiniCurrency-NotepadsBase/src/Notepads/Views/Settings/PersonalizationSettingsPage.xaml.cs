// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.Settings
{
    using Notepads.Extensions;
    using Notepads.Services;
    using Windows.System.Power;
    using Windows.UI;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Media;

    public sealed partial class PersonalizationSettingsPage : Page
    {
        private readonly UISettings UISettings = new UISettings();

        public PersonalizationSettingsPage()
        {
            InitializeComponent();

            if (ThemeSettingsService.UseWindowsTheme)
            {
                ThemeModeDefaultButton.IsChecked = true;
            }
            else
            {
                switch (ThemeSettingsService.ThemeMode)
                {
                    case ElementTheme.Light:
                        ThemeModeLightButton.IsChecked = true;
                        break;
                    case ElementTheme.Dark:
                        ThemeModeDarkButton.IsChecked = true;
                        break;
                }
            }

            AccentColorToggle.IsOn = ThemeSettingsService.UseWindowsAccentColor;
            AccentColorPicker.IsEnabled = !ThemeSettingsService.UseWindowsAccentColor;
            BackgroundTintOpacitySlider.Value = ThemeSettingsService.AppBackgroundPanelTintOpacity * 100;
            MiniCurrencyUiScaleSlider.Value = AppSettingsService.MiniCurrencyUiScalePercent;
            MiniCurrencyActiveCardOpacitySlider.Value = AppSettingsService.MiniCurrencyActiveCardBackgroundOpacityPercent;
            MiniCurrencyCardOpacitySlider.Value = AppSettingsService.MiniCurrencyCardBackgroundOpacityPercent;
            MiniCurrencyValueFontWeightSlider.Value = AppSettingsService.MiniCurrencyValueFontWeight;
            MiniCurrencyUseDefaultInactiveCardColorToggle.IsOn = AppSettingsService.MiniCurrencyUseDefaultInactiveCardColor;
            MiniCurrencyInactiveCardColorPicker.IsEnabled = !AppSettingsService.MiniCurrencyUseDefaultInactiveCardColor;
            MiniCurrencyInactiveCardColorPicker.Color = AppSettingsService.MiniCurrencyInactiveCardColor;
            AccentColorPicker.Color = ThemeSettingsService.AppAccentColor;

            if (App.IsGameBarWidget)
            {
                // Game Bar widgets do not support transparency, disable this setting
                BackgroundTintOpacityTitle.Visibility = Visibility.Collapsed;
                BackgroundTintOpacityControls.Visibility = Visibility.Collapsed;
            }
            else
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            }

            Loaded += PersonalizationSettings_Loaded;
            Unloaded += PersonalizationSettings_Unloaded;
        }

        private async void ThemeSettingsService_OnAccentColorChanged(object sender, Color color)
        {
            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                BackgroundTintOpacitySlider.Foreground = new SolidColorBrush(color);
                AccentColorPicker.ColorChanged -= AccentColorPicker_OnColorChanged;
                AccentColorPicker.Color = color;
                AccentColorPicker.ColorChanged += AccentColorPicker_OnColorChanged;
            });
        }

        private void PersonalizationSettings_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeModeDefaultButton.Checked += ThemeRadioButton_OnChecked;
            ThemeModeLightButton.Checked += ThemeRadioButton_OnChecked;
            ThemeModeDarkButton.Checked += ThemeRadioButton_OnChecked;
            BackgroundTintOpacitySlider.ValueChanged += BackgroundTintOpacitySlider_OnValueChanged;
            MiniCurrencyUiScaleSlider.ValueChanged += MiniCurrencyUiScaleSlider_OnValueChanged;
            MiniCurrencyActiveCardOpacitySlider.ValueChanged += MiniCurrencyActiveCardOpacitySlider_OnValueChanged;
            MiniCurrencyCardOpacitySlider.ValueChanged += MiniCurrencyCardOpacitySlider_OnValueChanged;
            MiniCurrencyValueFontWeightSlider.ValueChanged += MiniCurrencyValueFontWeightSlider_OnValueChanged;
            MiniCurrencyUseDefaultInactiveCardColorToggle.Toggled += MiniCurrencyUseDefaultInactiveCardColorToggle_OnToggled;
            MiniCurrencyInactiveCardColorPicker.ColorChanged += MiniCurrencyInactiveCardColorPicker_OnColorChanged;
            AccentColorToggle.Toggled += WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged += AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged += ThemeSettingsService_OnAccentColorChanged;
            if (!App.IsGameBarWidget)
            {
                UISettings.AdvancedEffectsEnabledChanged += UISettings_AdvancedEffectsEnabledChanged;
                PowerManager.EnergySaverStatusChanged += PowerManager_EnergySaverStatusChanged;
            }
        }

        private void PersonalizationSettings_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeModeDefaultButton.Checked -= ThemeRadioButton_OnChecked;
            ThemeModeLightButton.Checked -= ThemeRadioButton_OnChecked;
            ThemeModeDarkButton.Checked -= ThemeRadioButton_OnChecked;
            BackgroundTintOpacitySlider.ValueChanged -= BackgroundTintOpacitySlider_OnValueChanged;
            MiniCurrencyUiScaleSlider.ValueChanged -= MiniCurrencyUiScaleSlider_OnValueChanged;
            MiniCurrencyActiveCardOpacitySlider.ValueChanged -= MiniCurrencyActiveCardOpacitySlider_OnValueChanged;
            MiniCurrencyCardOpacitySlider.ValueChanged -= MiniCurrencyCardOpacitySlider_OnValueChanged;
            MiniCurrencyValueFontWeightSlider.ValueChanged -= MiniCurrencyValueFontWeightSlider_OnValueChanged;
            MiniCurrencyUseDefaultInactiveCardColorToggle.Toggled -= MiniCurrencyUseDefaultInactiveCardColorToggle_OnToggled;
            MiniCurrencyInactiveCardColorPicker.ColorChanged -= MiniCurrencyInactiveCardColorPicker_OnColorChanged;
            AccentColorToggle.Toggled -= WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged -= AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged -= ThemeSettingsService_OnAccentColorChanged;
            if (!App.IsGameBarWidget)
            {
                UISettings.AdvancedEffectsEnabledChanged -= UISettings_AdvancedEffectsEnabledChanged;
                PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;
            }
        }

        private async void PowerManager_EnergySaverStatusChanged(object sender, object e)
        {
            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            });
        }

        private async void UISettings_AdvancedEffectsEnabledChanged(UISettings sender, object args)
        {
            await Dispatcher.CallOnUIThreadAsync(() =>
            {
                BackgroundTintOpacitySlider.IsEnabled = UISettings.AdvancedEffectsEnabled &&
                                                        PowerManager.EnergySaverStatus != EnergySaverStatus.On;
            });
        }

        private void ThemeRadioButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                switch (radioButton.Tag)
                {
                    case "Light":
                        ThemeSettingsService.UseWindowsTheme = false;
                        ThemeSettingsService.SetTheme(ElementTheme.Light);
                        break;
                    case "Dark":
                        ThemeSettingsService.UseWindowsTheme = false;
                        ThemeSettingsService.SetTheme(ElementTheme.Dark);
                        break;
                    case "Default":
                        ThemeSettingsService.UseWindowsTheme = true;
                        break;
                }
            }
        }

        private void AccentColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (AccentColorPicker.IsEnabled)
            {
                ThemeSettingsService.AppAccentColor = args.NewColor;
                if (!AccentColorToggle.IsOn) ThemeSettingsService.CustomAccentColor = args.NewColor;
            }
        }

        private void BackgroundTintOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            ThemeSettingsService.AppBackgroundPanelTintOpacity = e.NewValue / 100;
        }

        private void MiniCurrencyUiScaleSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            AppSettingsService.MiniCurrencyUiScalePercent = (int)System.Math.Round(e.NewValue);
        }

        private void MiniCurrencyActiveCardOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            AppSettingsService.MiniCurrencyActiveCardBackgroundOpacityPercent = (int)System.Math.Round(e.NewValue);
        }

        private void MiniCurrencyCardOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            AppSettingsService.MiniCurrencyCardBackgroundOpacityPercent = (int)System.Math.Round(e.NewValue);
        }

        private void MiniCurrencyValueFontWeightSlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var raw = (int)System.Math.Round(e.NewValue);
            var normalized = (int)System.Math.Round(raw / 100.0) * 100;
            AppSettingsService.MiniCurrencyValueFontWeight = normalized;
        }

        private void MiniCurrencyUseDefaultInactiveCardColorToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            var useDefault = MiniCurrencyUseDefaultInactiveCardColorToggle.IsOn;
            MiniCurrencyInactiveCardColorPicker.IsEnabled = !useDefault;
            AppSettingsService.MiniCurrencyUseDefaultInactiveCardColor = useDefault;
        }

        private void MiniCurrencyInactiveCardColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (MiniCurrencyInactiveCardColorPicker.IsEnabled)
            {
                AppSettingsService.MiniCurrencyInactiveCardColor = args.NewColor;
            }
        }

        private void WindowsAccentColorToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            AccentColorPicker.IsEnabled = !AccentColorToggle.IsOn;
            ThemeSettingsService.UseWindowsAccentColor = AccentColorToggle.IsOn;
            AccentColorPicker.Color = AccentColorToggle.IsOn ? ThemeSettingsService.AppAccentColor : ThemeSettingsService.CustomAccentColor;
        }
    }
}
