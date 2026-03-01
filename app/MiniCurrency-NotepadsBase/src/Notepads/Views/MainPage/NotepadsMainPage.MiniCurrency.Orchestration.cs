// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using Notepads.Services;
    using System;
    using System.Linq;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public sealed partial class NotepadsMainPage
    {
        private void InitializeMiniCurrencyOverlay()
        {
            if (_miniCurrencyInitialized)
            {
                return;
            }

            _miniCurrencyInputs["RUB"] = InputRUB;
            _miniCurrencyInputs["KZT"] = InputKZT;
            _miniCurrencyInputs["USD"] = InputUSD;
            _miniCurrencyInputs["TRY"] = InputTRY;
            _miniCurrencyInputs["NOK"] = InputNOK;
            _miniCurrencyInputs["AED"] = InputAED;
            _miniCurrencyInputs["EUR"] = InputEUR;
            _miniCurrencyInputs["GBP"] = InputGBP;
            _miniCurrencyInputs["CNY"] = InputCNY;

            _miniCurrencyRows["RUB"] = RowRUB;
            _miniCurrencyRows["KZT"] = RowKZT;
            _miniCurrencyRows["USD"] = RowUSD;
            _miniCurrencyRows["TRY"] = RowTRY;
            _miniCurrencyRows["NOK"] = RowNOK;
            _miniCurrencyRows["AED"] = RowAED;
            _miniCurrencyRows["EUR"] = RowEUR;
            _miniCurrencyRows["GBP"] = RowGBP;
            _miniCurrencyRows["CNY"] = RowCNY;
            AttachMiniCurrencyCodeBlockTapHandlersForAllRows();

            EnsureMiniCurrencyRatesForAllKnownCodes();
            MiniCurrencySettingsSyncService.VisibleCurrenciesChanged -= MiniCurrencySettingsSyncService_VisibleCurrenciesChanged;
            MiniCurrencySettingsSyncService.VisibleCurrenciesChanged += MiniCurrencySettingsSyncService_VisibleCurrenciesChanged;
            AppSettingsService.OnMiniCurrencyUiScalePercentChanged -= MiniCurrencyUiScalePercentChanged;
            AppSettingsService.OnMiniCurrencyUiScalePercentChanged += MiniCurrencyUiScalePercentChanged;
            AppSettingsService.OnMiniCurrencyActiveCardBackgroundOpacityPercentChanged -= MiniCurrencyActiveCardBackgroundOpacityPercentChanged;
            AppSettingsService.OnMiniCurrencyActiveCardBackgroundOpacityPercentChanged += MiniCurrencyActiveCardBackgroundOpacityPercentChanged;
            AppSettingsService.OnMiniCurrencyCardBackgroundOpacityPercentChanged -= MiniCurrencyCardBackgroundOpacityPercentChanged;
            AppSettingsService.OnMiniCurrencyCardBackgroundOpacityPercentChanged += MiniCurrencyCardBackgroundOpacityPercentChanged;
            AppSettingsService.OnMiniCurrencyValueFontWeightChanged -= MiniCurrencyValueFontWeightChanged;
            AppSettingsService.OnMiniCurrencyValueFontWeightChanged += MiniCurrencyValueFontWeightChanged;
            AppSettingsService.OnMiniCurrencyUseDefaultInactiveCardColorChanged -= MiniCurrencyUseDefaultInactiveCardColorChanged;
            AppSettingsService.OnMiniCurrencyUseDefaultInactiveCardColorChanged += MiniCurrencyUseDefaultInactiveCardColorChanged;
            AppSettingsService.OnMiniCurrencyInactiveCardColorChanged -= MiniCurrencyInactiveCardColorChanged;
            AppSettingsService.OnMiniCurrencyInactiveCardColorChanged += MiniCurrencyInactiveCardColorChanged;
            ThemeSettingsService.OnAccentColorChanged -= MiniCurrencyThemeSettingsService_OnAccentColorChanged;
            ThemeSettingsService.OnAccentColorChanged += MiniCurrencyThemeSettingsService_OnAccentColorChanged;

            _miniCurrencyInitialized = true;
            if (CurrencyStatusText != null)
            {
                CurrencyStatusText.Visibility = Visibility.Collapsed;
            }

            InitializeMiniCurrencyFlags();
            ApplyMiniCurrencyUiScale();
            ApplyMiniCurrencyValueFontWeight();
            RestoreMiniCurrencyRowOrder();
            RestoreMiniCurrencyVisibleCurrencies();
            RestoreMiniCurrencyValues();
            RebuildMiniCurrencyAddList();
            SetMiniCurrencyStatus("Загрузка курсов...");
            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
            ConvertFromMiniCurrency(_miniCurrencyActiveCode);
            _ = LoadMiniCurrencyRatesAsync(silent: true);
        }

        private void EnsureMiniCurrencyRatesForAllKnownCodes()
        {
            foreach (var code in _miniCurrencyDisplayNames.Keys)
            {
                if (!_miniCurrencyRates.ContainsKey(code))
                {
                    _miniCurrencyRates[code] = double.NaN;
                }
            }
        }

        private bool EnsureMiniCurrencyRowExists(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            code = code.Trim().ToUpperInvariant();
            if (_miniCurrencyRows.ContainsKey(code) && _miniCurrencyInputs.ContainsKey(code))
            {
                return true;
            }

            if (CurrencyRowsHost == null || !_miniCurrencyDisplayNames.ContainsKey(code))
            {
                return false;
            }

            var row = new Grid
            {
                Tag = code,
                Margin = new Thickness(0, 0, 0, 12),
                Background = new SolidColorBrush(Colors.Transparent),
                Visibility = Visibility.Collapsed
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });

            row.Tapped += CurrencyRow_Tapped;
            row.PointerEntered += CurrencyRow_PointerEntered;
            row.PointerExited += CurrencyRow_PointerExited;
            row.PointerPressed += CurrencyRow_PointerPressed;
            row.PointerMoved += CurrencyRow_PointerMoved;
            row.PointerReleased += CurrencyRow_PointerReleased;
            row.PointerCanceled += CurrencyRow_PointerCanceled;

            var leftBorder = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x3A, 0x3A, 0x3A)),
                Height = 56,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(16, 0, 16, 0)
            };
            Grid.SetColumn(leftBorder, 0);

            var leftStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var flagCircle = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(0, 0, 14, 0),
                Background = new SolidColorBrush(Color.FromArgb(0xFF, 0x1C, 0x9C, 0xCB))
            };
            var flagImage = new Image
            {
                Stretch = Stretch.UniformToFill,
                Width = 30,
                Height = 30
            };
            flagCircle.Child = flagImage;

            var codeText = new TextBlock
            {
                Text = code,
                FontSize = 18,
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            leftStack.Children.Add(flagCircle);
            leftStack.Children.Add(codeText);
            leftBorder.Child = leftStack;
            row.Children.Add(leftBorder);

            var textBox = new TextBox
            {
                Tag = code,
                Height = 56,
                Text = string.Empty,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                TextAlignment = TextAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 24,
                Padding = new Thickness(16, 0, 16, 0),
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x3A, 0x3A, 0x3A)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                IsReadOnly = true,
                IsTabStop = false,
                IsHitTestVisible = false
            };

            textBox.Style = InputUSD?.Style ?? InputRUB?.Style;
            if (textBox.Style == null)
            {
                try
                {
                    if (Resources.TryGetValue("TransparentTextBoxStyle", out var styleObj) && styleObj is Style style)
                    {
                        textBox.Style = style;
                    }
                }
                catch
                {
                    // style is optional for dynamic rows
                }
            }

            textBox.TextChanged += CurrencyInput_TextChanged;
            textBox.GotFocus += CurrencyInput_GotFocus;
            ApplyMiniCurrencyValueFontWeightToInput(textBox);
            Grid.SetColumn(textBox, 2);
            row.Children.Add(textBox);

            CurrencyRowsHost.Children.Add(row);
            _miniCurrencyRows[code] = row;
            _miniCurrencyInputs[code] = textBox;
            if (!_miniCurrencyRates.ContainsKey(code)) _miniCurrencyRates[code] = double.NaN;

            SetMiniCurrencyFlag(flagImage, code);
            AttachMiniCurrencyCodeBlockTapHandler(row, code);
            ApplyMiniCurrencyUiScaleToRow(row);
            return true;
        }

        private void SetMiniCurrencyCurrencyVisibilityWithSmoothInsert(string code, FrameworkElement row)
        {
            if (row == null)
            {
                return;
            }

            var previousTransitions = CurrencyRowsHost?.ChildrenTransitions;
            if (CurrencyRowsHost != null)
            {
                CurrencyRowsHost.ChildrenTransitions = null;
            }

            row.Opacity = 0;
            row.Visibility = Visibility.Visible;
            MoveMiniCurrencyRowToBottom(code);
            SaveMiniCurrencyRowOrder();

            if (CurrencyRowsHost != null)
            {
                CurrencyRowsHost.UpdateLayout();
                CurrencyRowsHost.ChildrenTransitions = previousTransitions;
            }

            var fade = new Windows.UI.Xaml.Media.Animation.DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(140)),
                EnableDependentAnimation = true,
                EasingFunction = new Windows.UI.Xaml.Media.Animation.CubicEase
                {
                    EasingMode = Windows.UI.Xaml.Media.Animation.EasingMode.EaseOut
                }
            };
            var sb = new Windows.UI.Xaml.Media.Animation.Storyboard();
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(fade, row);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fade, "Opacity");
            sb.Children.Add(fade);
            sb.Begin();
        }

        private bool SetMiniCurrencyCurrencyVisibility(string code, bool visible, bool updateActiveSelection)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            code = code.Trim().ToUpperInvariant();
            if (!EnsureMiniCurrencyRowExists(code) || !_miniCurrencyRows.TryGetValue(code, out var row))
            {
                return false;
            }

            if (!visible)
            {
                var visibleCount = _miniCurrencyRows.Count(x => x.Value.Visibility == Visibility.Visible);
                if (visibleCount <= 1)
                {
                    SetMiniCurrencyStatus("Нельзя удалить последнюю валюту");
                    return false;
                }

                row.Visibility = Visibility.Collapsed;
                SetMiniCurrencyStatus($"Валюта {code} скрыта");
                SaveMiniCurrencyVisibleCurrencies();
                RebuildMiniCurrencyAddList();

                if (_miniCurrencyActiveCode == code)
                {
                    var next = _miniCurrencyRows.FirstOrDefault(x => x.Value.Visibility == Visibility.Visible).Key;
                    if (!string.IsNullOrEmpty(next))
                    {
                        _miniCurrencyActiveCode = next;
                        ConvertFromMiniCurrency(next);
                    }
                }

                HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
                return true;
            }

            var previousActiveCode = _miniCurrencyActiveCode;
            SetMiniCurrencyCurrencyVisibilityWithSmoothInsert(code, row);

            if (updateActiveSelection)
            {
                _miniCurrencyActiveCode = code;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);

            SetMiniCurrencyStatus($"Валюта {code} показана");
            SaveMiniCurrencyVisibleCurrencies();
            RebuildMiniCurrencyAddList();

            var convertSourceCode = code;
            if (_miniCurrencyInputs.TryGetValue(code, out var addedInput) && !ParseMiniCurrencyNumber(addedInput.Text).HasValue)
            {
                if (!string.IsNullOrWhiteSpace(previousActiveCode)
                    && previousActiveCode != code
                    && _miniCurrencyInputs.ContainsKey(previousActiveCode)
                    && IsMiniCurrencyVisible(previousActiveCode))
                {
                    convertSourceCode = previousActiveCode;
                }
            }

            ConvertFromMiniCurrency(convertSourceCode);
            return true;
        }

        private void MoveMiniCurrencyRowToBottom(string code)
        {
            if (CurrencyRowsHost == null || string.IsNullOrWhiteSpace(code) || !_miniCurrencyRows.TryGetValue(code, out var row))
            {
                return;
            }

            var currentIndex = CurrencyRowsHost.Children.IndexOf(row);
            if (currentIndex < 0)
            {
                return;
            }

            CurrencyRowsHost.Children.RemoveAt(currentIndex);
            CurrencyRowsHost.Children.Add(row);
        }

        private void RefreshMiniCurrencyCurrencyManagerMenu()
        {
            BuildMiniCurrencyCurrencyManagerMenu();
            ApplyMiniCurrencyMainMenuMode();
        }

        private void CurrencyRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement element) || !(element.Tag is string code))
            {
                return;
            }

            if (SetMiniCurrencyCurrencyVisibility(code, visible: false, updateActiveSelection: false))
            {
                RefreshMiniCurrencyCurrencyManagerMenu();
            }
        }

        private void AddCurrencyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(CurrencySelectBox.SelectedItem is ComboBoxItem selectedItem) || !(selectedItem.Content is string code))
            {
                return;
            }

            if (!_miniCurrencyDisplayNames.ContainsKey(code))
            {
                SetMiniCurrencyStatus($"Валюта {code} пока не добавлена в каталог");
                return;
            }

            if (SetMiniCurrencyCurrencyVisibility(code, visible: true, updateActiveSelection: true))
            {
                RefreshMiniCurrencyCurrencyManagerMenu();
            }
        }

        private async void MiniCurrencySettingsSyncService_VisibleCurrenciesChanged(object sender, System.Collections.Generic.IReadOnlyList<string> e)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInitialized)
            {
                return;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var requestedVisible = new System.Collections.Generic.HashSet<string>((e ?? System.Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant()));

                foreach (var code in _miniCurrencyRows.Keys.ToList())
                {
                    var shouldBeVisible = requestedVisible.Contains(code);
                    var isVisible = IsMiniCurrencyVisible(code);
                    if (isVisible && !shouldBeVisible)
                    {
                        SetMiniCurrencyCurrencyVisibility(code, visible: false, updateActiveSelection: false);
                    }
                }

                if (e != null)
                {
                    foreach (var rawCode in e)
                    {
                        if (string.IsNullOrWhiteSpace(rawCode))
                        {
                            continue;
                        }

                        var code = rawCode.Trim().ToUpperInvariant();
                        if (!requestedVisible.Contains(code) || IsMiniCurrencyVisible(code))
                        {
                            continue;
                        }

                        // Re-adding from Settings should append to the bottom, like a new add.
                        SetMiniCurrencyCurrencyVisibility(code, visible: true, updateActiveSelection: false);
                    }
                }

                RebuildMiniCurrencyAddList();
                HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
                ConvertFromMiniCurrency(_miniCurrencyActiveCode);
            });
        }
        private async void RefreshRatesButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadMiniCurrencyRatesAsync(silent: false);
        }
    }
}







