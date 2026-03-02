// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using Notepads.Services;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.Data.Json;
    using Windows.Storage;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class NotepadsMainPage
    {
        private const double MiniCurrencyUiScaleMinFactor = 0.5;
        private const double MiniCurrencyUiScaleMaxFactor = 1.5;
        private const double MiniCurrencyBaseRowHeight = 56;
        private const double MiniCurrencyBaseRowSpacing = 12;
        private const double MiniCurrencyBaseLeftColumnWidth = 132;
        private const double MiniCurrencyBaseMiddleGapWidth = 14;
        private const double MiniCurrencyBaseLeftCardCornerRadius = 14;
        private const double MiniCurrencyBaseValueCardCornerRadius = 16;
        private const double MiniCurrencyBaseFlagSize = 30;
        private const double MiniCurrencyBaseFlagCornerRadius = 15;
        private const double MiniCurrencyBaseFlagCodeGap = 14;
        private const double MiniCurrencyBaseCodeFontSize = 18;
        private const double MiniCurrencyBaseValueFontSize = 24;
        private const double MiniCurrencyBaseFieldHorizontalPadding = 16;
        private const double MiniCurrencyBaseRemoveButtonSize = 34;
        private const double MiniCurrencyBaseRemoveButtonLeftMargin = 8;
        private const double MiniCurrencyBaseAddControlsHeight = 42;
        private const double MiniCurrencyBaseAddControlsGap = 10;
        private const double MiniCurrencyBaseStatusFontSize = 12;
        private const double MiniCurrencyBaseStatusMarginTop = 8;
        private const double MiniCurrencyBaseCalculatorButtonFontSize = 22;
        private const double MiniCurrencyBaseCalculatorColumnGap = 7;
        private const double MiniCurrencyBaseCalculatorRowGap = 6;
        private const double MiniCurrencyBaseCalculatorSeparatorPadding = 12;
        private const double MiniCurrencyBaseCalculatorBottomPadding = 16;
        private const double MiniCurrencyBaseHorizontalLayoutMargin = 36;
        private const double MiniCurrencyBaseTopLayoutMargin = 16;
        private const double MiniCurrencyCalculatorHoverDarkenFactor = 0.84;
        private const double MiniCurrencyCalculatorPressedDarkenFactor = 0.72;
        private const double MiniCurrencyCalculatorOperationFontDelta = 3;
        private const double MiniCurrencyCalculatorAcFontDelta = -1;
        private const double MiniCurrencyBaseCalculatorBackspaceIconWidth = 23;
        private const double MiniCurrencyBaseCalculatorBackspaceIconHeight = 17;
        private const double MiniCurrencyBaseCalculatorPercentIconWidth = 17;
        private const double MiniCurrencyBaseCalculatorPercentIconHeight = 17;
        private const double MiniCurrencyBaseCalculatorDivideIconWidth = 17;
        private const double MiniCurrencyBaseCalculatorDivideIconHeight = 17;
        private const double MiniCurrencyBaseCalculatorMultiplyIconWidth = 16;
        private const double MiniCurrencyBaseCalculatorMultiplyIconHeight = 16;
        private const double MiniCurrencyBaseCalculatorMinusIconWidth = 17;
        private const double MiniCurrencyBaseCalculatorMinusIconHeight = 4;
        private const double MiniCurrencyBaseCalculatorPlusIconWidth = 17;
        private const double MiniCurrencyBaseCalculatorPlusIconHeight = 17;
        private const double MiniCurrencyBaseCalculatorEqualsIconWidth = 17;
        private const double MiniCurrencyBaseCalculatorEqualsIconHeight = 14;
        private static readonly Color MiniCurrencyAdaptiveTextDarkColor = Color.FromArgb(255, 0x3E, 0x3E, 0x3E);
        private static readonly Color MiniCurrencyAdaptiveTextLightColor = Color.FromArgb(255, 0xF2, 0xF2, 0xF2);

        private void RestoreMiniCurrencyValues()
        {
            var restoredAny = false;

            try
            {
                if (MiniCurrencySettings.Values.TryGetValue(MiniCurrencyValuesKey, out var rawObj) &&
                    rawObj is string raw &&
                    !string.IsNullOrWhiteSpace(raw) &&
                    JsonObject.TryParse(raw, out var valuesObj))
                {
                    _miniCurrencyIsUpdating = true;
                    try
                    {
                        foreach (var pair in _miniCurrencyInputs)
                        {
                            if (valuesObj.TryGetValue(pair.Key, out var value) && value.ValueType == JsonValueType.String)
                            {
                                pair.Value.Text = value.GetString();
                                restoredAny = true;
                            }
                        }
                    }
                    finally
                    {
                        _miniCurrencyIsUpdating = false;
                    }
                }
            }
            catch
            {
                // ignore and use defaults
            }

            if (!restoredAny)
            {
                _miniCurrencyIsUpdating = true;
                try
                {
                    foreach (var input in _miniCurrencyInputs.Values)
                    {
                        input.Text = "0";
                    }
                }
                finally
                {
                    _miniCurrencyIsUpdating = false;
                }
            }
        }

        private void SaveMiniCurrencyValues()
        {
            try
            {
                var valuesObj = new JsonObject();
                foreach (var pair in _miniCurrencyInputs)
                {
                    valuesObj[pair.Key] = JsonValue.CreateStringValue(pair.Value?.Text ?? string.Empty);
                }

                MiniCurrencySettings.Values[MiniCurrencyValuesKey] = valuesObj.Stringify();
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private void RestoreMiniCurrencyRowOrder()
        {
            if (CurrencyRowsHost == null)
            {
                return;
            }

            try
            {
                if (!(MiniCurrencySettings.Values.TryGetValue(MiniCurrencyRowOrderKey, out var rawObj) &&
                    rawObj is string raw &&
                    !string.IsNullOrWhiteSpace(raw)))
                {
                    return;
                }

                var codes = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().ToUpperInvariant())
                    .Where(x => _miniCurrencyRows.ContainsKey(x))
                    .Distinct()
                    .ToList();

                if (codes.Count == 0)
                {
                    return;
                }

                foreach (var code in _miniCurrencyRows.Keys.Where(x => !codes.Contains(x)))
                {
                    codes.Add(code);
                }

                var orderedRows = codes.Select(code => _miniCurrencyRows[code]).ToList();
                CurrencyRowsHost.Children.Clear();
                foreach (var row in orderedRows)
                {
                    CurrencyRowsHost.Children.Add(row);
                }
            }
            catch
            {
                // ignore and keep default layout order
            }
        }

        private void SaveMiniCurrencyRowOrder()
        {
            if (CurrencyRowsHost == null)
            {
                return;
            }

            try
            {
                var order = CurrencyRowsHost.Children
                    .OfType<FrameworkElement>()
                    .Select(x => x.Tag as string)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                MiniCurrencySettings.Values[MiniCurrencyRowOrderKey] = string.Join(",", order);
            }
            catch
            {
                // ignore persistence failures
            }
        }

        private void SetMiniCurrencyStatus(string text)
        {
            _miniCurrencyLatestStatusText = text ?? string.Empty;

            if (CurrencyStatusText != null)
            {
                CurrencyStatusText.Text = _miniCurrencyLatestStatusText;
            }

            if (EncodingIndicator != null)
            {
                EncodingIndicator.Text = _miniCurrencyLatestStatusText;
            }
        }

        private void SetMiniCurrencyRatesStatus(string text)
        {
            _miniCurrencyRatesStatusText = text ?? string.Empty;
            SetMiniCurrencyStatus(_miniCurrencyRatesStatusText);
        }

        private void RestoreMiniCurrencyRatesStatus()
        {
            var status = string.IsNullOrWhiteSpace(_miniCurrencyRatesStatusText)
                ? "Курсы обновлены"
                : _miniCurrencyRatesStatusText;
            SetMiniCurrencyStatus(status);
        }

        private void InitializeMiniCurrencyFlags()
        {
            // Set SVG sources explicitly for the built-in rows.
            SetMiniCurrencyFlag(FlagRUB, "RUB");
            SetMiniCurrencyFlag(FlagKZT, "KZT");
            SetMiniCurrencyFlag(FlagUSD, "USD");
            SetMiniCurrencyFlag(FlagTRY, "TRY");
            SetMiniCurrencyFlag(FlagNOK, "NOK");
            SetMiniCurrencyFlag(FlagAED, "AED");
            SetMiniCurrencyFlag(FlagEUR, "EUR");
            SetMiniCurrencyFlag(FlagGBP, "GBP");
            SetMiniCurrencyFlag(FlagCNY, "CNY");
        }

        private async void SetMiniCurrencyFlag(Image image, string code)
        {
            if (image == null || string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            PrepareMiniCurrencyFlagImage(image);
            var normalizedCode = code.Trim().ToUpperInvariant();

            try
            {
                var svgSource = new SvgImageSource();
                ConfigureMiniCurrencyFlagSvgSource(svgSource, image);
                svgSource.OpenFailed += (s, e) => image.Source = null;
                svgSource.Opened += (s, e) => MakeMiniCurrencyFlagContainerTransparent(image);
                svgSource.UriSource = new Uri($"ms-appx:///Assets/Flags/{normalizedCode}.svg");
                image.Source = svgSource;
            }
            catch
            {
                var svgSource = new SvgImageSource();
                ConfigureMiniCurrencyFlagSvgSource(svgSource, image);
                svgSource.OpenFailed += (s, e) => image.Source = null;
                svgSource.Opened += (s, e) => MakeMiniCurrencyFlagContainerTransparent(image);
                svgSource.UriSource = new Uri($"ms-appx:///Assets/Flags/{normalizedCode}.svg");
                image.Source = svgSource;
            }
        }

        private static void PrepareMiniCurrencyFlagImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.Stretch = Stretch.Uniform;
            image.HorizontalAlignment = HorizontalAlignment.Center;
            image.VerticalAlignment = VerticalAlignment.Center;
        }

        private static void ConfigureMiniCurrencyFlagSvgSource(SvgImageSource svgSource, Image image)
        {
            if (svgSource == null)
            {
                return;
            }

            // Our current SVG assets declare width/height=512 on the root <svg>.
            // In UWP, requesting a smaller raster can crop the image (top-left corner only).
            // Keep raster equal to intrinsic size to avoid cropping.
            svgSource.RasterizePixelWidth = 512;
            svgSource.RasterizePixelHeight = 512;
        }

        private static void MakeMiniCurrencyFlagContainerTransparent(Image image)
        {
            if (image?.Tag is Border taggedBorder)
            {
                taggedBorder.Background = new SolidColorBrush(Colors.Transparent);
                return;
            }

            if (image?.Parent is Border border)
            {
                border.Background = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void SaveMiniCurrencyVisibleCurrencies()
        {
            try
            {
                var visible = _miniCurrencyRows
                    .Where(x => x.Value.Visibility == Visibility.Visible)
                    .Select(x => x.Key)
                    .ToArray();
                MiniCurrencySettings.Values[MiniCurrencyVisibleCurrenciesKey] = string.Join(",", visible);
            }
            catch
            {
                // ignore persistence errors for now
            }
        }

        private void RestoreMiniCurrencyVisibleCurrencies()
        {
            var visibleCodes = new HashSet<string>(_miniCurrencyDefaultVisible);

            try
            {
                if (MiniCurrencySettings.Values.TryGetValue(MiniCurrencyVisibleCurrenciesKey, out var rawObj) &&
                    rawObj is string raw &&
                    !string.IsNullOrWhiteSpace(raw))
                {
                    var parsed = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToUpperInvariant())
                        .Where(x => _miniCurrencyDisplayNames.ContainsKey(x))
                        .ToHashSet();

                    foreach (var code in parsed)
                    {
                        EnsureMiniCurrencyRowExists(code);
                    }
                    if (parsed.Count > 0)
                    {
                        visibleCodes = parsed;
                    }
                }
            }
            catch
            {
                // ignore and keep defaults
            }

            foreach (var row in _miniCurrencyRows)
            {
                row.Value.Visibility = visibleCodes.Contains(row.Key) ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!visibleCodes.Contains(_miniCurrencyActiveCode))
            {
                _miniCurrencyActiveCode = visibleCodes.FirstOrDefault() ?? _miniCurrencyDefaultVisible.First();
            }
        }

        private void RebuildMiniCurrencyAddList()
        {
            if (CurrencySelectBox == null)
            {
                return;
            }

            CurrencySelectBox.Items.Clear();
            foreach (var code in _miniCurrencyRows.Keys.OrderBy(x => x))
            {
                if (_miniCurrencyRows[code].Visibility == Visibility.Visible)
                {
                    continue;
                }

                CurrencySelectBox.Items.Add(new ComboBoxItem { Content = code });
            }

            CurrencySelectBox.SelectedIndex = CurrencySelectBox.Items.Count > 0 ? 0 : -1;
            AddCurrencyButton.IsEnabled = CurrencySelectBox.Items.Count > 0;
        }

        private void HighlightMiniCurrencyActiveRow(string activeCode)
        {
            var activeBackgroundColor = GetMiniCurrencyActiveInputBackgroundColor();
            var activeBorderColor = GetMiniCurrencyActiveInputBorderColor();
            var inactiveBackgroundColor = GetMiniCurrencyInactiveCardBackgroundColor();
            var inactiveBorderColor = Color.FromArgb(16, 255, 255, 255);
            var effectiveInactiveBackgroundColor = GetMiniCurrencyEffectiveCardColor(inactiveBackgroundColor);
            var effectiveActiveBackgroundColor = GetMiniCurrencyEffectiveCardColor(activeBackgroundColor);
            var inactiveCardTextBrush = new SolidColorBrush(GetMiniCurrencyAdaptiveTextColor(effectiveInactiveBackgroundColor));
            var inactiveValueTextBrush = new SolidColorBrush(GetMiniCurrencyAdaptiveTextColor(effectiveInactiveBackgroundColor));
            var activeValueTextBrush = new SolidColorBrush(GetMiniCurrencyAdaptiveTextColor(effectiveActiveBackgroundColor));

            foreach (var pair in _miniCurrencyInputs)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                var isActive = pair.Key == activeCode;
                if (_miniCurrencyRows.TryGetValue(pair.Key, out var rowElement) && rowElement is Grid row)
                {
                    var leftBorder = row.Children.OfType<Border>().FirstOrDefault(x => Grid.GetColumn(x) == 0);
                    if (leftBorder != null)
                    {
                        leftBorder.Background = new SolidColorBrush(inactiveBackgroundColor);
                        if (leftBorder.Child is StackPanel leftStack)
                        {
                            var codeText = leftStack.Children.OfType<TextBlock>().FirstOrDefault();
                            if (codeText != null)
                            {
                                codeText.Foreground = inactiveCardTextBrush;
                            }
                        }
                    }
                }

                pair.Value.Foreground = isActive ? activeValueTextBrush : inactiveValueTextBrush;
                pair.Value.Background = new SolidColorBrush(isActive ? activeBackgroundColor : inactiveBackgroundColor);
                pair.Value.BorderBrush = new SolidColorBrush(isActive ? activeBorderColor : inactiveBorderColor);
            }
        }

        private async void MiniCurrencyThemeSettingsService_OnAccentColorChanged(object sender, Color color)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
                ApplyMiniCurrencyCalculatorVisualSettings();
                RefreshMiniCurrencyCurrencyPickerRowVisualStates();
            });
        }

        private void MiniCurrencyCardBackgroundOpacityPercentChanged(object sender, int percent)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
        }

        private void MiniCurrencyActiveCardBackgroundOpacityPercentChanged(object sender, int percent)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
        }

        private void MiniCurrencyValueFontWeightChanged(object sender, int fontWeight)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyValueFontWeight();
        }

        private void MiniCurrencyUseDefaultInactiveCardColorChanged(object sender, bool useDefault)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
        }

        private void MiniCurrencyInactiveCardColorChanged(object sender, Color color)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            if (AppSettingsService.MiniCurrencyUseDefaultInactiveCardColor)
            {
                return;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
        }

        private void MiniCurrencyCalculatorUseWindowsEqualsColorChanged(object sender, bool useWindowsColor)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private void MiniCurrencyCalculatorEqualsColorChanged(object sender, Color color)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            if (AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private void MiniCurrencyCalculatorEqualsButtonOpacityPercentChanged(object sender, int percent)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private void MiniCurrencyCalculatorDigitTextColorChanged(object sender, Color color)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private void MiniCurrencyCalculatorOperationTextColorChanged(object sender, Color color)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private void MiniCurrencyCalculatorButtonsOpacityPercentChanged(object sender, int percent)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            ApplyMiniCurrencyCalculatorVisualSettings();
        }

        private Color GetMiniCurrencyInactiveCardBackgroundColor()
        {
            var alpha = GetMiniCurrencyCardBackgroundAlpha();
            var baseColor = GetMiniCurrencyInactiveCardBaseOpaqueColor();
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private Color GetMiniCurrencyActiveInputBackgroundColor()
        {
            var accent = ThemeSettingsService.AppAccentColor;
            return Color.FromArgb(GetMiniCurrencyActiveCardBackgroundAlpha(), accent.R, accent.G, accent.B);
        }

        private Color GetMiniCurrencyActiveInputBorderColor()
        {
            return ThemeSettingsService.AppAccentColor;
        }

        private byte GetMiniCurrencyCardBackgroundAlpha()
        {
            var percent = Math.Max(0, Math.Min(100, AppSettingsService.MiniCurrencyCardBackgroundOpacityPercent));
            return (byte)Math.Round(255 * (percent / 100.0));
        }

        private byte GetMiniCurrencyActiveCardBackgroundAlpha()
        {
            var percent = Math.Max(0, Math.Min(100, AppSettingsService.MiniCurrencyActiveCardBackgroundOpacityPercent));
            return (byte)Math.Round(255 * (percent / 100.0));
        }

        private void ApplyMiniCurrencyValueFontWeight()
        {
            var weight = GetMiniCurrencyValueFontWeight();
            foreach (var input in _miniCurrencyInputs.Values)
            {
                ApplyMiniCurrencyValueFontWeightToInput(input, weight);
            }
        }

        private static void ApplyMiniCurrencyValueFontWeightToInput(TextBox input, Windows.UI.Text.FontWeight? fontWeight = null)
        {
            if (input == null)
            {
                return;
            }

            input.FontWeight = fontWeight ?? GetMiniCurrencyValueFontWeight();
        }

        private static Windows.UI.Text.FontWeight GetMiniCurrencyValueFontWeight()
        {
            var normalized = Math.Max(100, Math.Min(900, AppSettingsService.MiniCurrencyValueFontWeight));
            var rounded = (int)Math.Round(normalized / 100.0) * 100;
            return new Windows.UI.Text.FontWeight { Weight = (ushort)rounded };
        }

        private Color GetMiniCurrencyInactiveCardBaseOpaqueColor()
        {
            if (AppSettingsService.MiniCurrencyUseDefaultInactiveCardColor)
            {
                return Color.FromArgb(255, 58, 58, 58);
            }

            var custom = AppSettingsService.MiniCurrencyInactiveCardColor;
            return Color.FromArgb(255, custom.R, custom.G, custom.B);
        }

        private static Color GetMiniCurrencyAdaptiveTextColor(Color backgroundColor)
        {
            // WCAG relative luminance / contrast ratio for sRGB.
            var darkContrast = GetMiniCurrencyContrastRatio(backgroundColor, MiniCurrencyAdaptiveTextDarkColor);
            var lightContrast = GetMiniCurrencyContrastRatio(backgroundColor, MiniCurrencyAdaptiveTextLightColor);
            return lightContrast >= darkContrast ? MiniCurrencyAdaptiveTextLightColor : MiniCurrencyAdaptiveTextDarkColor;
        }

        private void ApplyMiniCurrencyCalculatorVisualSettings()
        {
            if (MiniCurrencyCalculatorHost == null)
            {
                return;
            }

            var digitBackground = GetMiniCurrencyCalculatorDigitButtonsBackgroundColor();
            var operatorBackground = GetMiniCurrencyCalculatorOperatorButtonsBackgroundColor();
            var equalsBackground = GetMiniCurrencyCalculatorEqualsButtonBackgroundColor();

            foreach (var button in GetMiniCurrencyCalculatorDigitButtons())
            {
                ApplyMiniCurrencyCalculatorButtonPalette(button, digitBackground);
            }

            foreach (var button in GetMiniCurrencyCalculatorOperatorButtons())
            {
                ApplyMiniCurrencyCalculatorButtonPalette(button, operatorBackground);
            }

            ApplyMiniCurrencyCalculatorButtonPalette(MiniCurrencyCalcEqualsButton, equalsBackground, keepForegroundOnInteraction: true);
        }

        private void ApplyMiniCurrencyCalculatorButtonPalette(Button button, Color baseBackground, bool keepForegroundOnInteraction = false)
        {
            if (button == null)
            {
                return;
            }

            var hoverBackground = DarkenMiniCurrencyColor(baseBackground, MiniCurrencyCalculatorHoverDarkenFactor);
            var pressedBackground = DarkenMiniCurrencyColor(baseBackground, MiniCurrencyCalculatorPressedDarkenFactor);

            var baseForeground = GetMiniCurrencyAdaptiveTextColor(GetMiniCurrencyEffectiveCardColor(baseBackground));
            var hoverForeground = GetMiniCurrencyAdaptiveTextColor(GetMiniCurrencyEffectiveCardColor(hoverBackground));
            var pressedForeground = GetMiniCurrencyAdaptiveTextColor(GetMiniCurrencyEffectiveCardColor(pressedBackground));
            var interactiveForeground = keepForegroundOnInteraction ? baseForeground : hoverForeground;
            var pressedStateForeground = keepForegroundOnInteraction ? baseForeground : pressedForeground;

            button.Background = new SolidColorBrush(baseBackground);
            button.Foreground = new SolidColorBrush(baseForeground);

            SetMiniCurrencyButtonStateBrush(button, "ButtonBackground", baseBackground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonBackgroundPointerOver", hoverBackground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonBackgroundPressed", pressedBackground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonForeground", baseForeground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonForegroundPointerOver", interactiveForeground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonForegroundPressed", pressedStateForeground);
            SetMiniCurrencyButtonStateBrush(button, "ButtonBorderBrush", Colors.Transparent);
            SetMiniCurrencyButtonStateBrush(button, "ButtonBorderBrushPointerOver", Colors.Transparent);
            SetMiniCurrencyButtonStateBrush(button, "ButtonBorderBrushPressed", Colors.Transparent);
        }

        private static void SetMiniCurrencyButtonStateBrush(Button button, string key, Color color)
        {
            if (button == null || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            button.Resources[key] = new SolidColorBrush(color);
        }

        private static Color DarkenMiniCurrencyColor(Color color, double factor)
        {
            var normalizedFactor = Math.Max(0, Math.Min(1, factor));
            return Color.FromArgb(
                color.A,
                (byte)Math.Round(color.R * normalizedFactor),
                (byte)Math.Round(color.G * normalizedFactor),
                (byte)Math.Round(color.B * normalizedFactor));
        }

        private Color GetMiniCurrencyCalculatorDigitButtonsBackgroundColor()
        {
            var alpha = GetMiniCurrencyCalculatorButtonsAlpha();
            var baseColor = AppSettingsService.MiniCurrencyCalculatorDigitTextColor;
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private Color GetMiniCurrencyCalculatorOperatorButtonsBackgroundColor()
        {
            var alpha = GetMiniCurrencyCalculatorButtonsAlpha();
            var baseColor = AppSettingsService.MiniCurrencyCalculatorOperationTextColor;
            return Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);
        }

        private Color GetMiniCurrencyCalculatorEqualsButtonBackgroundColor()
        {
            var baseColor = AppSettingsService.MiniCurrencyCalculatorUseWindowsEqualsColor
                ? ThemeSettingsService.AppAccentColor
                : AppSettingsService.MiniCurrencyCalculatorEqualsColor;
            return Color.FromArgb(GetMiniCurrencyCalculatorEqualsButtonAlpha(), baseColor.R, baseColor.G, baseColor.B);
        }

        private static byte GetMiniCurrencyCalculatorButtonsAlpha()
        {
            var percent = Math.Max(0, Math.Min(100, AppSettingsService.MiniCurrencyCalculatorButtonsOpacityPercent));
            return (byte)Math.Round(255 * (percent / 100.0));
        }

        private static byte GetMiniCurrencyCalculatorEqualsButtonAlpha()
        {
            var percent = Math.Max(0, Math.Min(100, AppSettingsService.MiniCurrencyCalculatorEqualsButtonOpacityPercent));
            return (byte)Math.Round(255 * (percent / 100.0));
        }

        private IEnumerable<Button> GetMiniCurrencyCalculatorDigitButtons()
        {
            return new[]
            {
                MiniCurrencyCalcDigit0Button,
                MiniCurrencyCalcDigit1Button,
                MiniCurrencyCalcDigit2Button,
                MiniCurrencyCalcDigit3Button,
                MiniCurrencyCalcDigit4Button,
                MiniCurrencyCalcDigit5Button,
                MiniCurrencyCalcDigit6Button,
                MiniCurrencyCalcDigit7Button,
                MiniCurrencyCalcDigit8Button,
                MiniCurrencyCalcDigit9Button,
                MiniCurrencyCalcCommaButton
            };
        }

        private IEnumerable<Button> GetMiniCurrencyCalculatorOperatorButtons()
        {
            return new[]
            {
                MiniCurrencyCalcBackspaceButton,
                MiniCurrencyCalcAcButton,
                MiniCurrencyCalcPercentButton,
                MiniCurrencyCalcDivideButton,
                MiniCurrencyCalcMultiplyButton,
                MiniCurrencyCalcMinusButton,
                MiniCurrencyCalcPlusButton
            };
        }

        private Color GetMiniCurrencyEffectiveCardColor(Color cardColor)
        {
            if (cardColor.A >= 255)
            {
                return cardColor;
            }

            var sceneBackground = GetMiniCurrencyEstimatedSceneBackgroundColor();
            return BlendMiniCurrencyColorOverBackground(cardColor, sceneBackground);
        }

        private Color GetMiniCurrencyEstimatedSceneBackgroundColor()
        {
            if (MiniCurrencyOverlay?.Background is SolidColorBrush overlayBrush && overlayBrush.Color.A > 0)
            {
                return overlayBrush.Color;
            }

            if (RootGrid?.Background is SolidColorBrush rootBrush && rootBrush.Color.A > 0)
            {
                return rootBrush.Color;
            }

            if (Background is SolidColorBrush pageBrush && pageBrush.Color.A > 0)
            {
                return pageBrush.Color;
            }

            return Color.FromArgb(255, 20, 20, 24);
        }

        private static double GetMiniCurrencyContrastRatio(Color backgroundColor, Color foregroundColor)
        {
            var bgLuminance = GetMiniCurrencyRelativeLuminance(backgroundColor);
            var fgLuminance = GetMiniCurrencyRelativeLuminance(foregroundColor);
            var lighter = Math.Max(bgLuminance, fgLuminance);
            var darker = Math.Min(bgLuminance, fgLuminance);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double GetMiniCurrencyRelativeLuminance(Color color)
        {
            var r = GetMiniCurrencyLinearChannel(color.R / 255.0);
            var g = GetMiniCurrencyLinearChannel(color.G / 255.0);
            var b = GetMiniCurrencyLinearChannel(color.B / 255.0);
            return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
        }

        private static double GetMiniCurrencyLinearChannel(double srgb)
        {
            return srgb <= 0.04045
                ? srgb / 12.92
                : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        private void MiniCurrencyUiScalePercentChanged(object sender, int percent)
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            QueueMiniCurrencyUiScaleApply();
        }

        private void QueueMiniCurrencyUiScaleApply()
        {
            if (_miniCurrencyUiScaleApplyTimer == null)
            {
                _miniCurrencyUiScaleApplyTimer = new DispatcherTimer
                {
                    // Coalesce slider updates while dragging to reduce layout thrashing.
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _miniCurrencyUiScaleApplyTimer.Tick += MiniCurrencyUiScaleApplyTimer_Tick;
            }

            _miniCurrencyUiScaleApplyTimer.Stop();
            _miniCurrencyUiScaleApplyTimer.Start();
        }

        private void MiniCurrencyUiScaleApplyTimer_Tick(object sender, object e)
        {
            _miniCurrencyUiScaleApplyTimer?.Stop();

            if (!IsMiniCurrencyMode)
            {
                return;
            }

            ApplyMiniCurrencyUiScale();
        }

        private void ApplyMiniCurrencyUiScale()
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            var factor = GetMiniCurrencyUiScaleFactor(AppSettingsService.MiniCurrencyUiScalePercent);
            var horizontalMargin = ScaleMetric(MiniCurrencyBaseHorizontalLayoutMargin, factor);

            foreach (var row in _miniCurrencyRows.Values)
            {
                ApplyMiniCurrencyUiScaleToRow(row, factor);
            }

            if (MiniCurrencyMainLayoutGrid != null)
            {
                MiniCurrencyMainLayoutGrid.Margin = new Thickness(
                    horizontalMargin,
                    ScaleMetric(MiniCurrencyBaseTopLayoutMargin, factor),
                    horizontalMargin,
                    0);
            }

            ApplyMiniCurrencyUiScaleToExtraControls(factor);
            UpdateMiniCurrencyRowWidths();
        }

        private void ApplyMiniCurrencyUiScaleToRow(FrameworkElement rowElement)
        {
            var factor = GetMiniCurrencyUiScaleFactor(AppSettingsService.MiniCurrencyUiScalePercent);
            ApplyMiniCurrencyUiScaleToRow(rowElement, factor);
        }

        private void ApplyMiniCurrencyUiScaleToRow(FrameworkElement rowElement, double factor)
        {
            if (!(rowElement is Grid row))
            {
                return;
            }

            row.Margin = ScaleThickness(0, 0, 0, MiniCurrencyBaseRowSpacing, factor);

            if (row.ColumnDefinitions.Count >= 2)
            {
                row.ColumnDefinitions[0].Width = new GridLength(ScaleMetric(MiniCurrencyBaseLeftColumnWidth, factor));
                row.ColumnDefinitions[1].Width = new GridLength(ScaleMetric(MiniCurrencyBaseMiddleGapWidth, factor));
            }

            var leftBorder = row.Children.Count > 0
                ? row.Children[0] as Border
                : null;

            if (leftBorder == null)
            {
                leftBorder = row.Children
                    .OfType<Border>()
                    .FirstOrDefault(x => Grid.GetColumn(x) == 0);
            }

            if (leftBorder != null)
            {
                leftBorder.Height = ScaleMetric(MiniCurrencyBaseRowHeight, factor);
                leftBorder.CornerRadius = new CornerRadius(ScaleMetric(MiniCurrencyBaseLeftCardCornerRadius, factor));
                leftBorder.Padding = ScaleThickness(
                    MiniCurrencyBaseFieldHorizontalPadding, 0,
                    MiniCurrencyBaseFieldHorizontalPadding, 0, factor);

                if (leftBorder.Child is StackPanel leftStack)
                {
                    leftStack.HorizontalAlignment = HorizontalAlignment.Left;

                    if (leftStack.Children.Count > 0 && leftStack.Children[0] is Border flagCircle)
                    {
                        var flagSize = ScaleMetric(MiniCurrencyBaseFlagSize, factor);
                        flagCircle.Width = flagSize;
                        flagCircle.Height = flagSize;
                        flagCircle.CornerRadius = new CornerRadius(ScaleMetric(MiniCurrencyBaseFlagCornerRadius, factor));
                        flagCircle.Margin = new Thickness(0, 0, ScaleMetric(GetMiniCurrencyFlagCodeGapBase(), factor), 0);

                        if (flagCircle.Child is Image flagImage)
                        {
                            flagImage.Width = flagSize;
                            flagImage.Height = flagSize;
                        }

                    }

                    var codeText = leftStack.Children.Count > 1
                        ? leftStack.Children[1] as TextBlock
                        : null;

                    if (codeText == null)
                    {
                        codeText = leftStack.Children.OfType<TextBlock>().FirstOrDefault();
                    }
                    if (codeText != null)
                    {
                        codeText.FontSize = ScaleMetric(MiniCurrencyBaseCodeFontSize, factor);
                    }
                }
            }

            var input = row.Children.Count > 2
                ? row.Children[2] as TextBox
                : null;

            if (input == null)
            {
                input = row.Children.OfType<TextBox>().FirstOrDefault();
            }
            if (input != null)
            {
                input.MinWidth = 0;
                input.Height = ScaleMetric(MiniCurrencyBaseRowHeight, factor);
                input.FontSize = ScaleMetric(MiniCurrencyBaseValueFontSize, factor);
                input.Padding = ScaleThickness(
                    MiniCurrencyBaseFieldHorizontalPadding, 0,
                    MiniCurrencyBaseFieldHorizontalPadding, 0, factor);
                input.CornerRadius = new CornerRadius(ScaleMetric(MiniCurrencyBaseValueCardCornerRadius, factor));
            }

            var removeButton = row.Children.Count > 3
                ? row.Children[3] as Button
                : null;

            if (removeButton == null)
            {
                removeButton = row.Children
                    .OfType<Button>()
                    .FirstOrDefault(x => Grid.GetColumn(x) == 3);
            }

            if (removeButton != null)
            {
                var removeButtonSize = ScaleMetric(MiniCurrencyBaseRemoveButtonSize, factor);
                removeButton.Height = removeButtonSize;
                removeButton.Width = removeButtonSize;
                removeButton.Margin = new Thickness(ScaleMetric(MiniCurrencyBaseRemoveButtonLeftMargin, factor), 0, 0, 0);
            }
        }

        private void ApplyMiniCurrencyUiScaleToExtraControls(double factor)
        {
            if (CurrencySelectBox != null)
            {
                CurrencySelectBox.Width = ScaleMetric(MiniCurrencyBaseLeftColumnWidth, factor);
                CurrencySelectBox.Height = ScaleMetric(MiniCurrencyBaseAddControlsHeight, factor);
            }

            if (AddCurrencyButton != null)
            {
                AddCurrencyButton.Height = ScaleMetric(MiniCurrencyBaseAddControlsHeight, factor);
                AddCurrencyButton.Margin = new Thickness(ScaleMetric(MiniCurrencyBaseAddControlsGap, factor), 0, 0, 0);
                AddCurrencyButton.Padding = ScaleThickness(16, 0, 16, 0, factor);
            }

            if (RefreshRatesButton != null)
            {
                RefreshRatesButton.Height = ScaleMetric(MiniCurrencyBaseAddControlsHeight, factor);
                RefreshRatesButton.Margin = new Thickness(ScaleMetric(MiniCurrencyBaseAddControlsGap, factor), 0, 0, 0);
                RefreshRatesButton.Padding = ScaleThickness(16, 0, 16, 0, factor);
            }

            if (CurrencyStatusText != null)
            {
                CurrencyStatusText.Margin = new Thickness(0, ScaleMetric(MiniCurrencyBaseStatusMarginTop, factor), 0, 0);
                CurrencyStatusText.FontSize = ScaleMetric(MiniCurrencyBaseStatusFontSize, factor);
            }

            ApplyMiniCurrencyUiScaleToCalculator(factor);
            UpdateMiniCurrencyCurrencyRowsBottomPadding();
        }

        private void MiniCurrencyCalculatorHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMiniCurrencyCurrencyRowsBottomPadding();
            UpdateMiniCurrencyCalculatorRowWidths();
        }

        private void MiniCurrencyMainLayoutGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMiniCurrencyRowWidths();
            UpdateMiniCurrencyCalculatorRowWidths();
        }

        private void CurrencyRowsViewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateMiniCurrencyRowWidths();
        }

        private void UpdateMiniCurrencyCurrencyRowsBottomPadding()
        {
            if (CurrencyRowsHost == null)
            {
                return;
            }

            var current = CurrencyRowsHost.Margin;
            CurrencyRowsHost.Margin = new Thickness(current.Left, current.Top, current.Right, 0);
        }

        private void UpdateMiniCurrencyRowWidths()
        {
            if (_miniCurrencyRows == null)
            {
                return;
            }

            var factor = GetMiniCurrencyUiScaleFactor(AppSettingsService.MiniCurrencyUiScalePercent);
            var width = GetMiniCurrencyContentWidth(factor);
            if (width <= 0)
            {
                return;
            }

            if (CurrencyRowsScrollViewer != null)
            {
                CurrencyRowsScrollViewer.Width = width;
                CurrencyRowsScrollViewer.HorizontalAlignment = HorizontalAlignment.Left;
            }

            if (CurrencyRowsHost != null)
            {
                CurrencyRowsHost.Width = width;
                CurrencyRowsHost.HorizontalAlignment = HorizontalAlignment.Left;
            }

            if (CurrencyRowsViewport != null)
            {
                CurrencyRowsViewport.Width = width;
                CurrencyRowsViewport.HorizontalAlignment = HorizontalAlignment.Left;
            }

            foreach (var row in _miniCurrencyRows.Values)
            {
                if (row != null)
                {
                    row.Width = width;
                    row.HorizontalAlignment = HorizontalAlignment.Left;
                }
            }
        }

        private void ApplyMiniCurrencyUiScaleToCalculator(double factor)
        {
            if (MiniCurrencyCalculatorHost == null)
            {
                return;
            }

            var buttonHeight = ScaleMetric(MiniCurrencyBaseRowHeight, factor);
            var gapWidth = ScaleMetric(MiniCurrencyBaseCalculatorColumnGap, factor);
            var rowGap = ScaleMetric(MiniCurrencyBaseCalculatorRowGap, factor);
            var cornerRadius = new CornerRadius(ScaleMetric(MiniCurrencyBaseLeftCardCornerRadius, factor));
            var buttonFontSize = ScaleMetric(MiniCurrencyBaseCalculatorButtonFontSize, factor);

            if (MiniCurrencyCalculatorHost.RowDefinitions.Count >= 5)
            {
                MiniCurrencyCalculatorHost.RowDefinitions[0].Height = new GridLength(ScaleMetric(MiniCurrencyBaseCalculatorSeparatorPadding, factor));
                MiniCurrencyCalculatorHost.RowDefinitions[2].Height = new GridLength(ScaleMetric(MiniCurrencyBaseCalculatorSeparatorPadding, factor));
                MiniCurrencyCalculatorHost.RowDefinitions[4].Height = new GridLength(ScaleMetric(MiniCurrencyBaseCalculatorBottomPadding, factor));
            }

            ApplyMiniCurrencyCalculatorStandardRowLayout(MiniCurrencyCalculatorRow1Grid, gapWidth, rowGap);
            ApplyMiniCurrencyCalculatorStandardRowLayout(MiniCurrencyCalculatorRow2Grid, gapWidth, rowGap);
            ApplyMiniCurrencyCalculatorStandardRowLayout(MiniCurrencyCalculatorRow3Grid, gapWidth, rowGap);
            ApplyMiniCurrencyCalculatorStandardRowLayout(MiniCurrencyCalculatorRow4Grid, gapWidth, rowGap);

            if (MiniCurrencyCalculatorRow5Grid != null)
            {
                MiniCurrencyCalculatorRow5Grid.Margin = new Thickness(0);
                if (MiniCurrencyCalculatorRow5Grid.ColumnDefinitions.Count >= 7)
                {
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[1].Width = new GridLength(gapWidth);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[3].Width = new GridLength(gapWidth);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[5].Width = new GridLength(gapWidth);
                    MiniCurrencyCalculatorRow5Grid.ColumnDefinitions[6].Width = new GridLength(1, GridUnitType.Star);
                }
            }

            var calculatorButtons = new[]
            {
                MiniCurrencyCalcBackspaceButton,
                MiniCurrencyCalcAcButton,
                MiniCurrencyCalcPercentButton,
                MiniCurrencyCalcDivideButton,
                MiniCurrencyCalcDigit7Button,
                MiniCurrencyCalcDigit8Button,
                MiniCurrencyCalcDigit9Button,
                MiniCurrencyCalcMultiplyButton,
                MiniCurrencyCalcDigit4Button,
                MiniCurrencyCalcDigit5Button,
                MiniCurrencyCalcDigit6Button,
                MiniCurrencyCalcMinusButton,
                MiniCurrencyCalcDigit1Button,
                MiniCurrencyCalcDigit2Button,
                MiniCurrencyCalcDigit3Button,
                MiniCurrencyCalcPlusButton,
                MiniCurrencyCalcDigit0Button,
                MiniCurrencyCalcCommaButton,
                MiniCurrencyCalcEqualsButton
            };

            foreach (var button in calculatorButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.Height = buttonHeight;
                button.CornerRadius = cornerRadius;
                button.FontSize = buttonFontSize;
            }

            var operationFontSize = ScaleMetric(MiniCurrencyBaseCalculatorButtonFontSize + MiniCurrencyCalculatorOperationFontDelta, factor);
            ApplyMiniCurrencyCalculatorOperationFontSize(operationFontSize);

            if (MiniCurrencyCalcAcButton != null)
            {
                MiniCurrencyCalcAcButton.FontSize = ScaleMetric(MiniCurrencyBaseCalculatorButtonFontSize + MiniCurrencyCalculatorAcFontDelta, factor);
            }

            ApplyMiniCurrencyCalculatorIconScale(factor);
            ApplyMiniCurrencyCalculatorVisualSettings();

            UpdateMiniCurrencyCalculatorRowWidths();
        }

        private void ApplyMiniCurrencyCalculatorIconScale(double factor)
        {
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcBackspaceIconGrid,
                MiniCurrencyBaseCalculatorBackspaceIconWidth,
                MiniCurrencyBaseCalculatorBackspaceIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcPercentIconGrid,
                MiniCurrencyBaseCalculatorPercentIconWidth,
                MiniCurrencyBaseCalculatorPercentIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcDivideIconGrid,
                MiniCurrencyBaseCalculatorDivideIconWidth,
                MiniCurrencyBaseCalculatorDivideIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcMultiplyIconGrid,
                MiniCurrencyBaseCalculatorMultiplyIconWidth,
                MiniCurrencyBaseCalculatorMultiplyIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcMinusIconGrid,
                MiniCurrencyBaseCalculatorMinusIconWidth,
                MiniCurrencyBaseCalculatorMinusIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcPlusIconGrid,
                MiniCurrencyBaseCalculatorPlusIconWidth,
                MiniCurrencyBaseCalculatorPlusIconHeight,
                factor);
            ApplyMiniCurrencyCalculatorIconSize(
                MiniCurrencyCalcEqualsIconGrid,
                MiniCurrencyBaseCalculatorEqualsIconWidth,
                MiniCurrencyBaseCalculatorEqualsIconHeight,
                factor);
        }

        private static void ApplyMiniCurrencyCalculatorIconSize(FrameworkElement icon, double baseWidth, double baseHeight, double factor)
        {
            if (icon == null)
            {
                return;
            }

            icon.Width = ScaleMetric(baseWidth, factor);
            icon.Height = ScaleMetric(baseHeight, factor);
        }

        private static void ApplyMiniCurrencyCalculatorStandardRowLayout(Grid row, double gapWidth, double rowGap)
        {
            if (row == null)
            {
                return;
            }

            row.Margin = new Thickness(0, 0, 0, rowGap);
            if (row.ColumnDefinitions.Count < 7)
            {
                return;
            }

            row.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            row.ColumnDefinitions[1].Width = new GridLength(gapWidth);
            row.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            row.ColumnDefinitions[3].Width = new GridLength(gapWidth);
            row.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);
            row.ColumnDefinitions[5].Width = new GridLength(gapWidth);
            row.ColumnDefinitions[6].Width = new GridLength(1, GridUnitType.Star);
        }

        private void ApplyMiniCurrencyCalculatorOperationFontSize(double fontSize)
        {
            var operationButtons = new[]
            {
                MiniCurrencyCalcDivideButton,
                MiniCurrencyCalcMultiplyButton,
                MiniCurrencyCalcMinusButton,
                MiniCurrencyCalcPlusButton,
                MiniCurrencyCalcEqualsButton
            };

            foreach (var button in operationButtons)
            {
                if (button != null)
                {
                    button.FontSize = fontSize;
                }
            }
        }

        private void UpdateMiniCurrencyCalculatorRowWidths()
        {
            if (MiniCurrencyCalculatorHost == null)
            {
                return;
            }

            var factor = GetMiniCurrencyUiScaleFactor(AppSettingsService.MiniCurrencyUiScalePercent);
            var width = GetMiniCurrencyContentWidth(factor);
            if (width <= 0)
            {
                return;
            }

            MiniCurrencyCalculatorHost.Width = width;
            MiniCurrencyCalculatorHost.HorizontalAlignment = HorizontalAlignment.Left;

            var rows = new[]
            {
                MiniCurrencyCalculatorRow1Grid,
                MiniCurrencyCalculatorRow2Grid,
                MiniCurrencyCalculatorRow3Grid,
                MiniCurrencyCalculatorRow4Grid,
                MiniCurrencyCalculatorRow5Grid
            };

            foreach (var row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                row.Width = width;
                row.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private double GetMiniCurrencyContentWidth(double factor)
        {
            if (Window.Current == null)
            {
                return 0;
            }

            var horizontalMargin = ScaleMetric(MiniCurrencyBaseHorizontalLayoutMargin, factor);
            return Math.Max(1, Window.Current.Bounds.Width - (horizontalMargin * 2));
        }

        private static double GetMiniCurrencyUiScaleFactor(int percent)
        {
            var normalizedPercent = Math.Max(0, Math.Min(100, percent));
            return MiniCurrencyUiScaleMinFactor +
                (MiniCurrencyUiScaleMaxFactor - MiniCurrencyUiScaleMinFactor) * normalizedPercent / 100.0;
        }

        private double GetMiniCurrencyFlagCodeGapBase()
        {
            try
            {
                if (MiniCurrencyOverlay?.Resources != null &&
                    MiniCurrencyOverlay.Resources.ContainsKey("MiniCurrencyFlagCodeGapMargin") &&
                    MiniCurrencyOverlay.Resources["MiniCurrencyFlagCodeGapMargin"] is Thickness gap)
                {
                    return Math.Max(0, gap.Right);
                }
            }
            catch
            {
                // Fallback to the default spacing if resource lookup fails.
            }

            return MiniCurrencyBaseFlagCodeGap;
        }

        private static double ScaleMetric(double baseValue, double factor)
        {
            return Math.Round(baseValue * factor, 2);
        }

        private static Thickness ScaleThickness(double left, double top, double right, double bottom, double factor)
        {
            return new Thickness(
                ScaleMetric(left, factor),
                ScaleMetric(top, factor),
                ScaleMetric(right, factor),
                ScaleMetric(bottom, factor));
        }
    }
}

