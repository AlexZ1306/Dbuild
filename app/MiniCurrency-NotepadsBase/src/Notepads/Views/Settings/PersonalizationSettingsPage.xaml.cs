// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.Settings
{
    using System.Collections.Generic;
    using Notepads.Extensions;
    using Notepads.Services;
    using Windows.System.Power;
    using Windows.UI;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Controls.Primitives;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;

    public sealed partial class PersonalizationSettingsPage : Page
    {
        private readonly UISettings UISettings = new UISettings();
        private const double PersonalizationPreviewSliderOpacity = 0.0;
        private const double PersonalizationPreviewColorSpectrumOpacity = 0.05;
        private bool _isPersonalizationPreviewActive;
        private double _activePreviewHostOpacity = PersonalizationPreviewSliderOpacity;
        private FrameworkElement _personalizationPreviewHostElement;
        private SettingsPage _cachedParentSettingsPage;
        private Brush _savedPageBackground;
        private readonly Dictionary<FrameworkElement, (double Opacity, bool IsHitTestVisible)> _personalizationPreviewState
            = new Dictionary<FrameworkElement, (double Opacity, bool IsHitTestVisible)>();

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
            MiniCurrencyCalculatorUseWindowsEqualsColorToggle.IsOn = AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor;
            MiniCurrencyCalculatorEqualsButtonColorPicker.IsEnabled = !AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor;
            MiniCurrencyCalculatorEqualsButtonColorPicker.Color = AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor
                ? ThemeSettingsService.AppAccentColor
                : AppSettingsService.MiniCurrencyCalculatorEqualsColor;
            MiniCurrencyCalculatorEqualsButtonOpacitySlider.Value = AppSettingsService.MiniCurrencyCalculatorEqualsButtonOpacityPercent;
            MiniCurrencyCalculatorDigitTextColorPicker.Color = AppSettingsService.MiniCurrencyCalculatorDigitTextColor;
            MiniCurrencyCalculatorOperatorTextColorPicker.Color = AppSettingsService.MiniCurrencyCalculatorOperationTextColor;
            MiniCurrencyCalculatorButtonsOpacitySlider.Value = AppSettingsService.MiniCurrencyCalculatorButtonsOpacityPercent;
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

                if (MiniCurrencyCalculatorUseWindowsEqualsColorToggle.IsOn)
                {
                    MiniCurrencyCalculatorEqualsButtonColorPicker.ColorChanged -= MiniCurrencyCalculatorEqualsButtonColorPicker_OnColorChanged;
                    MiniCurrencyCalculatorEqualsButtonColorPicker.Color = color;
                    MiniCurrencyCalculatorEqualsButtonColorPicker.ColorChanged += MiniCurrencyCalculatorEqualsButtonColorPicker_OnColorChanged;
                }
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
            MiniCurrencyCalculatorUseWindowsEqualsColorToggle.Toggled += MiniCurrencyCalculatorUseWindowsEqualsColorToggle_OnToggled;
            MiniCurrencyCalculatorEqualsButtonColorPicker.ColorChanged += MiniCurrencyCalculatorEqualsButtonColorPicker_OnColorChanged;
            MiniCurrencyCalculatorEqualsButtonOpacitySlider.ValueChanged += MiniCurrencyCalculatorEqualsButtonOpacitySlider_OnValueChanged;
            MiniCurrencyCalculatorDigitTextColorPicker.ColorChanged += MiniCurrencyCalculatorDigitTextColorPicker_OnColorChanged;
            MiniCurrencyCalculatorOperatorTextColorPicker.ColorChanged += MiniCurrencyCalculatorOperatorTextColorPicker_OnColorChanged;
            MiniCurrencyCalculatorButtonsOpacitySlider.ValueChanged += MiniCurrencyCalculatorButtonsOpacitySlider_OnValueChanged;
            AccentColorToggle.Toggled += WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged += AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged += ThemeSettingsService_OnAccentColorChanged;
            RegisterPersonalizationPreviewHoldGestureHandlers();
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
            MiniCurrencyCalculatorUseWindowsEqualsColorToggle.Toggled -= MiniCurrencyCalculatorUseWindowsEqualsColorToggle_OnToggled;
            MiniCurrencyCalculatorEqualsButtonColorPicker.ColorChanged -= MiniCurrencyCalculatorEqualsButtonColorPicker_OnColorChanged;
            MiniCurrencyCalculatorEqualsButtonOpacitySlider.ValueChanged -= MiniCurrencyCalculatorEqualsButtonOpacitySlider_OnValueChanged;
            MiniCurrencyCalculatorDigitTextColorPicker.ColorChanged -= MiniCurrencyCalculatorDigitTextColorPicker_OnColorChanged;
            MiniCurrencyCalculatorOperatorTextColorPicker.ColorChanged -= MiniCurrencyCalculatorOperatorTextColorPicker_OnColorChanged;
            MiniCurrencyCalculatorButtonsOpacitySlider.ValueChanged -= MiniCurrencyCalculatorButtonsOpacitySlider_OnValueChanged;
            AccentColorToggle.Toggled -= WindowsAccentColorToggle_OnToggled;
            AccentColorPicker.ColorChanged -= AccentColorPicker_OnColorChanged;
            ThemeSettingsService.OnAccentColorChanged -= ThemeSettingsService_OnAccentColorChanged;
            UnregisterPersonalizationPreviewHoldGestureHandlers();
            ExitPersonalizationPreviewHoldMode();
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

        private void MiniCurrencyCalculatorUseWindowsEqualsColorToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            var useWindowsColor = MiniCurrencyCalculatorUseWindowsEqualsColorToggle.IsOn;
            MiniCurrencyCalculatorEqualsButtonColorPicker.IsEnabled = !useWindowsColor;
            AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor = useWindowsColor;
            if (useWindowsColor)
            {
                MiniCurrencyCalculatorEqualsButtonColorPicker.Color = ThemeSettingsService.AppAccentColor;
            }
            else
            {
                MiniCurrencyCalculatorEqualsButtonColorPicker.Color = AppSettingsService.MiniCurrencyCalculatorEqualsColor;
            }
        }

        private void MiniCurrencyCalculatorEqualsButtonColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (MiniCurrencyCalculatorEqualsButtonColorPicker.IsEnabled)
            {
                AppSettingsService.MiniCurrencyCalculatorEqualsColor = args.NewColor;
            }
        }

        private void MiniCurrencyCalculatorEqualsButtonOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            AppSettingsService.MiniCurrencyCalculatorEqualsButtonOpacityPercent = (int)System.Math.Round(e.NewValue);
        }

        private void MiniCurrencyCalculatorDigitTextColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            AppSettingsService.MiniCurrencyCalculatorDigitTextColor = args.NewColor;
        }

        private void MiniCurrencyCalculatorOperatorTextColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            AppSettingsService.MiniCurrencyCalculatorOperationTextColor = args.NewColor;
        }

        private void MiniCurrencyCalculatorButtonsOpacitySlider_OnValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            AppSettingsService.MiniCurrencyCalculatorButtonsOpacityPercent = (int)System.Math.Round(e.NewValue);
        }

        private void WindowsAccentColorToggle_OnToggled(object sender, RoutedEventArgs e)
        {
            AccentColorPicker.IsEnabled = !AccentColorToggle.IsOn;
            ThemeSettingsService.UseWindowsAccentColor = AccentColorToggle.IsOn;
            AccentColorPicker.Color = AccentColorToggle.IsOn ? ThemeSettingsService.AppAccentColor : ThemeSettingsService.CustomAccentColor;
        }

        private void RegisterPersonalizationPreviewHoldGestureHandlers()
        {
            RegisterPreviewPointerHandlers(BackgroundTintOpacitySlider);
            RegisterPreviewPointerHandlers(MiniCurrencyUiScaleSlider);
            RegisterPreviewPointerHandlers(MiniCurrencyActiveCardOpacitySlider);
            RegisterPreviewPointerHandlers(MiniCurrencyCardOpacitySlider);
            RegisterPreviewPointerHandlers(MiniCurrencyValueFontWeightSlider);
            RegisterPreviewPointerHandlers(MiniCurrencyCalculatorEqualsButtonOpacitySlider);
            RegisterPreviewPointerHandlers(MiniCurrencyCalculatorButtonsOpacitySlider);

            RegisterPreviewPointerHandlers(AccentColorPicker);
            RegisterPreviewPointerHandlers(MiniCurrencyInactiveCardColorPicker);
            RegisterPreviewPointerHandlers(MiniCurrencyCalculatorEqualsButtonColorPicker);
            RegisterPreviewPointerHandlers(MiniCurrencyCalculatorDigitTextColorPicker);
            RegisterPreviewPointerHandlers(MiniCurrencyCalculatorOperatorTextColorPicker);
        }

        private void UnregisterPersonalizationPreviewHoldGestureHandlers()
        {
            UnregisterPreviewPointerHandlers(BackgroundTintOpacitySlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyUiScaleSlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyActiveCardOpacitySlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyCardOpacitySlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyValueFontWeightSlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyCalculatorEqualsButtonOpacitySlider);
            UnregisterPreviewPointerHandlers(MiniCurrencyCalculatorButtonsOpacitySlider);

            UnregisterPreviewPointerHandlers(AccentColorPicker);
            UnregisterPreviewPointerHandlers(MiniCurrencyInactiveCardColorPicker);
            UnregisterPreviewPointerHandlers(MiniCurrencyCalculatorEqualsButtonColorPicker);
            UnregisterPreviewPointerHandlers(MiniCurrencyCalculatorDigitTextColorPicker);
            UnregisterPreviewPointerHandlers(MiniCurrencyCalculatorOperatorTextColorPicker);
        }

        private void RegisterPreviewPointerHandlers(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            element.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PersonalizationPreview_PointerPressed), true);
            element.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(PersonalizationPreview_PointerReleased), true);
            element.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(PersonalizationPreview_PointerCanceled), true);
            element.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(PersonalizationPreview_PointerCaptureLost), true);
            element.Unloaded += PersonalizationPreview_TrackedElement_Unloaded;
        }

        private void UnregisterPreviewPointerHandlers(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }

            element.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PersonalizationPreview_PointerPressed));
            element.RemoveHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(PersonalizationPreview_PointerReleased));
            element.RemoveHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(PersonalizationPreview_PointerCanceled));
            element.RemoveHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(PersonalizationPreview_PointerCaptureLost));
            element.Unloaded -= PersonalizationPreview_TrackedElement_Unloaded;
        }

        private void PersonalizationPreview_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var sourceControl = sender as Control;
            if (sourceControl == null || !sourceControl.IsEnabled)
            {
                return;
            }

            if (!e.GetCurrentPoint(sourceControl).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (!TryResolvePreviewHostAndOpacity(sourceControl, e.OriginalSource as DependencyObject, out var previewHost, out var previewOpacity))
            {
                return;
            }

            EnterPersonalizationPreviewHoldMode(previewHost, previewOpacity);
        }

        private void PersonalizationPreview_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ExitPersonalizationPreviewHoldMode();
        }

        private void PersonalizationPreview_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            ExitPersonalizationPreviewHoldMode();
        }

        private void PersonalizationPreview_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            ExitPersonalizationPreviewHoldMode();
        }

        private void PersonalizationPreview_TrackedElement_Unloaded(object sender, RoutedEventArgs e)
        {
            ExitPersonalizationPreviewHoldMode();
        }

        private void EnterPersonalizationPreviewHoldMode(FrameworkElement previewHost, double previewHostOpacity)
        {
            if (PersonalizationSettingsRootStack == null)
            {
                return;
            }

            if (previewHost == null)
            {
                return;
            }

            if (_isPersonalizationPreviewActive && ReferenceEquals(_personalizationPreviewHostElement, previewHost))
            {
                return;
            }

            ExitPersonalizationPreviewHoldMode();

            _isPersonalizationPreviewActive = true;
            _personalizationPreviewHostElement = previewHost;
            _activePreviewHostOpacity = previewHostOpacity;
            _savedPageBackground = Background;
            Background = new SolidColorBrush(Colors.Transparent);
            GetParentSettingsPage()?.EnterPreviewHoldMode();

            var topLevelContainer = FindPersonalizationTopLevelContainer(previewHost);
            if (topLevelContainer == null)
            {
                return;
            }

            foreach (var child in PersonalizationSettingsRootStack.Children)
            {
                if (!(child is FrameworkElement topLevelElement))
                {
                    continue;
                }

                if (ReferenceEquals(topLevelElement, topLevelContainer))
                {
                    ApplyPersonalizationPreviewInsideTopLevel(topLevelElement, previewHost);
                    continue;
                }

                SaveAndApplyPersonalizationPreviewState(topLevelElement, opacity: 0.0, isHitTestVisible: false);
            }
        }

        private void ExitPersonalizationPreviewHoldMode()
        {
            if (!_isPersonalizationPreviewActive && _personalizationPreviewState.Count == 0)
            {
                return;
            }

            foreach (var state in _personalizationPreviewState)
            {
                if (state.Key == null)
                {
                    continue;
                }

                state.Key.Opacity = state.Value.Opacity;
                state.Key.IsHitTestVisible = state.Value.IsHitTestVisible;
            }

            _personalizationPreviewState.Clear();
            _isPersonalizationPreviewActive = false;
            _personalizationPreviewHostElement = null;
            Background = _savedPageBackground;
            GetParentSettingsPage()?.ExitPreviewHoldMode();
        }

        private void ApplyPersonalizationPreviewInsideTopLevel(FrameworkElement topLevel, FrameworkElement previewHost)
        {
            if (topLevel == null || previewHost == null)
            {
                return;
            }

            foreach (var element in EnumerateSelfAndDescendants(topLevel))
            {
                if (element == null)
                {
                    continue;
                }

                var isOnPreviewPath = IsPersonalizationAncestorOrSelf(element, previewHost) ||
                                      IsPersonalizationAncestorOrSelf(previewHost, element);

                if (ReferenceEquals(element, previewHost))
                {
                    SaveAndApplyPersonalizationPreviewState(element, _activePreviewHostOpacity, isHitTestVisible: true);
                }
                else if (isOnPreviewPath)
                {
                    SaveAndApplyPersonalizationPreviewState(element, 1.0, isHitTestVisible: true);
                }
                else
                {
                    SaveAndApplyPersonalizationPreviewState(element, 0.0, isHitTestVisible: false);
                }
            }
        }

        private FrameworkElement FindPersonalizationTopLevelContainer(FrameworkElement element)
        {
            if (element == null || PersonalizationSettingsRootStack == null)
            {
                return null;
            }

            var current = element as DependencyObject;
            while (current != null)
            {
                var parent = VisualTreeHelper.GetParent(current);
                if (ReferenceEquals(parent, PersonalizationSettingsRootStack))
                {
                    return current as FrameworkElement;
                }

                current = parent;
            }

            return null;
        }

        private FrameworkElement GetPersonalizationPreviewHostElement(FrameworkElement sourceControl)
        {
            if (sourceControl == null)
            {
                return null;
            }

            if (sourceControl is Slider)
            {
                var current = sourceControl as DependencyObject;
                while (current != null)
                {
                    if (current is StackPanel horizontalStack &&
                        horizontalStack.Orientation == Orientation.Horizontal)
                    {
                        return horizontalStack;
                    }

                    current = VisualTreeHelper.GetParent(current);
                }

                return sourceControl;
            }

            return sourceControl;
        }

        private bool TryResolvePreviewHostAndOpacity(Control sourceControl, DependencyObject originalSource, out FrameworkElement previewHost, out double previewOpacity)
        {
            previewHost = null;
            previewOpacity = PersonalizationPreviewSliderOpacity;

            if (sourceControl is Slider)
            {
                previewHost = GetPersonalizationPreviewHostElement(sourceControl);
                previewOpacity = PersonalizationPreviewSliderOpacity;
                return previewHost != null;
            }

            if (sourceControl is ColorPicker colorPicker)
            {
                return TryResolveColorPickerPreviewHost(colorPicker, originalSource, out previewHost, out previewOpacity);
            }

            return false;
        }

        private bool TryResolveColorPickerPreviewHost(ColorPicker colorPicker, DependencyObject originalSource, out FrameworkElement previewHost, out double previewOpacity)
        {
            previewHost = null;
            previewOpacity = PersonalizationPreviewColorSpectrumOpacity;

            var current = originalSource;
            while (current != null && !ReferenceEquals(current, colorPicker))
            {
                if (current is ColorSpectrum colorSpectrum)
                {
                    previewHost = colorSpectrum;
                    previewOpacity = PersonalizationPreviewColorSpectrumOpacity;
                    return true;
                }

                if (current is Slider slider)
                {
                    previewHost = slider;
                    previewOpacity = PersonalizationPreviewSliderOpacity;
                    return true;
                }

                if (current is FrameworkElement element &&
                    element.GetType().Name.IndexOf("Slider", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    previewHost = element;
                    previewOpacity = PersonalizationPreviewSliderOpacity;
                    return true;
                }

                current = GetParentDependencyObject(current);
            }

            return false;
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

        private IEnumerable<FrameworkElement> EnumerateSelfAndDescendants(FrameworkElement root)
        {
            if (root == null)
            {
                yield break;
            }

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is FrameworkElement currentElement)
                {
                    yield return currentElement;
                }

                var childrenCount = VisualTreeHelper.GetChildrenCount(current);
                for (var i = 0; i < childrenCount; i++)
                {
                    queue.Enqueue(VisualTreeHelper.GetChild(current, i));
                }
            }
        }

        private static bool IsPersonalizationAncestorOrSelf(DependencyObject ancestor, DependencyObject node)
        {
            if (ancestor == null || node == null)
            {
                return false;
            }

            var current = node;
            while (current != null)
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void SaveAndApplyPersonalizationPreviewState(FrameworkElement element, double opacity, bool isHitTestVisible)
        {
            if (element == null)
            {
                return;
            }

            if (!_personalizationPreviewState.ContainsKey(element))
            {
                _personalizationPreviewState[element] = (element.Opacity, element.IsHitTestVisible);
            }

            element.Opacity = opacity;
            element.IsHitTestVisible = isHitTestVisible;
        }

        private SettingsPage GetParentSettingsPage()
        {
            if (_cachedParentSettingsPage != null)
            {
                return _cachedParentSettingsPage;
            }

            var current = this as DependencyObject;
            while (current != null)
            {
                if (current is SettingsPage settingsPage)
                {
                    _cachedParentSettingsPage = settingsPage;
                    return _cachedParentSettingsPage;
                }

                current = GetParentDependencyObject(current);
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
    }
}
