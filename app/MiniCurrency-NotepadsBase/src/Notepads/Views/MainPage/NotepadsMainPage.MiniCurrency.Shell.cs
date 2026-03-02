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
    using System.Threading.Tasks;
    using Windows.Foundation;
    using Windows.Data.Json;
    using Windows.Globalization;
    using Windows.Storage;
    using Windows.UI;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Imaging;

    public sealed partial class NotepadsMainPage
    {
        private void ApplyMiniCurrencyShellCustomizations()
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            ApplyMiniCurrencyStatusBarMode();
            ApplyMiniCurrencyMainMenuMode();
        }

        private void ApplyMiniCurrencyStatusBarMode()
        {
            if (!IsMiniCurrencyMode || StatusBar == null)
            {
                return;
            }

            if (FileModificationStateIndicator != null) FileModificationStateIndicator.Visibility = Visibility.Collapsed;
            if (PathIndicator != null) PathIndicator.Visibility = Visibility.Collapsed;
            if (ModificationIndicator != null) ModificationIndicator.Visibility = Visibility.Collapsed;
            if (LineColumnIndicatorButton != null) LineColumnIndicatorButton.Visibility = Visibility.Collapsed;
            if (FontZoomIndicator != null) FontZoomIndicator.Visibility = Visibility.Collapsed;
            if (LineEndingIndicator != null) LineEndingIndicator.Visibility = Visibility.Collapsed;
            if (ShadowWindowIndicator != null) ShadowWindowIndicator.Visibility = Visibility.Collapsed;

            if (EncodingIndicator != null)
            {
                EncodingIndicator.IsTapEnabled = true;
                EncodingIndicator.ContextFlyout = null;
                EncodingIndicator.PointerEntered -= EncodingIndicator_PointerEntered;
                EncodingIndicator.PointerExited -= EncodingIndicator_PointerExited;
                EncodingIndicator.PointerCanceled -= EncodingIndicator_PointerExited;
                EncodingIndicator.PointerCaptureLost -= EncodingIndicator_PointerExited;
                EncodingIndicator.PointerEntered += EncodingIndicator_PointerEntered;
                EncodingIndicator.PointerExited += EncodingIndicator_PointerExited;
                EncodingIndicator.PointerCanceled += EncodingIndicator_PointerExited;
                EncodingIndicator.PointerCaptureLost += EncodingIndicator_PointerExited;
                UpdateMiniCurrencyStatusIndicatorText();
                ToolTipService.SetToolTip(EncodingIndicator, "Обновить курсы валют");
            }
        }

        private void EncodingIndicator_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            _miniCurrencyStatusIndicatorHovered = true;
            UpdateMiniCurrencyStatusIndicatorText();
        }

        private void EncodingIndicator_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            _miniCurrencyStatusIndicatorHovered = false;
            UpdateMiniCurrencyStatusIndicatorText();
        }

        private void BuildMiniCurrencyCurrencyManagerMenu()
        {
            // Managed by the currency picker side panel opened from the main menu.
        }

        private void OpenSettingsPane()
        {
            _miniCurrencyCurrencyPickerMode = MiniCurrencyCurrencyPickerMode.ReplaceRowCurrency;
            _miniCurrencyCurrencyPickerSourceCode = null;
            _miniCurrencyRightPaneMode = MiniCurrencyRightPaneMode.Settings;
            EnsureMiniCurrencyWindowWidthForOpenPane();
            ApplyMiniCurrencyRightPaneWidthForCurrentMode();
            ShowSettingsPaneContent();
            RootSplitView.IsPaneOpen = true;
        }

        private void ApplyMiniCurrencyRightPaneWidthForCurrentMode()
        {
            if (RootSplitView == null)
            {
                return;
            }

            var targetWidth = GetMiniCurrencyDesiredPaneWidthForCurrentMode();
            if (Window.Current != null)
            {
                var maxAllowedPaneWidth = Math.Max(240, Window.Current.Bounds.Width - MiniCurrencyPaneWidthSafetyPadding);
                targetWidth = Math.Min(targetWidth, maxAllowedPaneWidth);
            }

            if (targetWidth > 0)
            {
                RootSplitView.OpenPaneLength = targetWidth;
            }
        }

        private double GetMiniCurrencyDesiredPaneWidthForCurrentMode()
        {
            return _miniCurrencyRightPaneMode == MiniCurrencyRightPaneMode.CurrencyPicker
                ? _miniCurrencyCurrencyPickerPaneWidth
                : _miniCurrencySettingsPaneWidth;
        }

        private string GetMiniCurrencyFlagAssetCode(string code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        private async Task OpenMiniCurrencyCurrencyPickerPaneAsync(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return;
            }

            sourceCode = sourceCode.Trim().ToUpperInvariant();
            if (!_miniCurrencyRows.ContainsKey(sourceCode))
            {
                return;
            }

            _miniCurrencyCurrencyPickerMode = MiniCurrencyCurrencyPickerMode.ReplaceRowCurrency;
            _miniCurrencyCurrencyPickerSourceCode = sourceCode;
            _miniCurrencyRightPaneMode = MiniCurrencyRightPaneMode.CurrencyPicker;
            EnsureMiniCurrencyWindowWidthForOpenPane();
            ApplyMiniCurrencyRightPaneWidthForCurrentMode();

            _miniCurrencyCurrencyPickerSearchQuery = string.Empty;
            if (MiniCurrencyCurrencyPickerSearchTextBox != null)
            {
                MiniCurrencyCurrencyPickerSearchTextBox.Text = string.Empty;
            }

            await EnsureMiniCurrencyCurrencyPickerListLoadedAsync();
            ShowMiniCurrencyCurrencyPickerPaneContent();
            RootSplitView.IsPaneOpen = true;
        }

        private async Task OpenMiniCurrencyCurrencyManagementPaneAsync()
        {
            _miniCurrencyCurrencyPickerMode = MiniCurrencyCurrencyPickerMode.ToggleMainVisibility;
            _miniCurrencyCurrencyPickerSourceCode = null;
            _miniCurrencyRightPaneMode = MiniCurrencyRightPaneMode.CurrencyPicker;
            EnsureMiniCurrencyWindowWidthForOpenPane();
            ApplyMiniCurrencyRightPaneWidthForCurrentMode();

            _miniCurrencyCurrencyPickerSearchQuery = string.Empty;
            if (MiniCurrencyCurrencyPickerSearchTextBox != null)
            {
                MiniCurrencyCurrencyPickerSearchTextBox.Text = string.Empty;
            }

            await EnsureMiniCurrencyCurrencyPickerListLoadedAsync();
            ShowMiniCurrencyCurrencyPickerPaneContent();
            RootSplitView.IsPaneOpen = true;
        }

        private void ApplyMiniCurrencyPreferredMinWindowSize()
        {
            if (!IsMiniCurrencyMode || App.IsGameBarWidget)
            {
                return;
            }

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(MiniCurrencyPreferredMinWindowWidth, 320));
        }

        private void EnsureMiniCurrencyWindowWidthForOpenPane()
        {
            // Keep window width stable; pane width is clamped to available space.
        }

        private void RestoreMiniCurrencyWindowWidthAfterPaneClosed()
        {
            _miniCurrencyWindowWidthBeforePaneOpen = -1;
        }

        private async Task EnsureMiniCurrencyCurrencyPickerListLoadedAsync()
        {
            if (MiniCurrencyCurrencyPickerListView == null)
            {
                return;
            }

            if (!_miniCurrencyCurrencyPickerLoaded)
            {
                var currencies = await LoadMiniCurrencyCurrencyPickerCatalogAsync();
                _miniCurrencyCurrencyPickerCatalog.Clear();
                _miniCurrencyCurrencyPickerCatalog.AddRange(currencies);
                _miniCurrencyPreferredUiCurrencyCode = GetMiniCurrencyPreferredUiCurrencyCode();
                _miniCurrencyCurrencyPickerLoaded = true;
            }

            RefreshMiniCurrencyCurrencyPickerList();
            UpdateMiniCurrencyCurrencyPickerSearchIconVisibility();
        }

        private void RefreshMiniCurrencyCurrencyPickerList()
        {
            if (MiniCurrencyCurrencyPickerListView == null)
            {
                return;
            }

            var query = (_miniCurrencyCurrencyPickerSearchQuery ?? string.Empty).Trim();
            var rows = _miniCurrencyCurrencyPickerCatalog
                .Where(currency => IsMiniCurrencyCurrencyPickerMatch(currency, query))
                .ToList();

            rows.Sort(CompareMiniCurrencyCurrencyPickerItems);

            MiniCurrencyCurrencyPickerListView.Items.Clear();
            foreach (var currency in rows)
            {
                MiniCurrencyCurrencyPickerListView.Items.Add(CreateMiniCurrencyCurrencyPickerListItem(currency));
            }
        }

        private static bool IsMiniCurrencyCurrencyPickerMatch(MiniCurrencyCatalogListItem currency, string query)
        {
            if (currency == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            return (!string.IsNullOrWhiteSpace(currency.Name) &&
                    currency.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (!string.IsNullOrWhiteSpace(currency.Code) &&
                    currency.Code.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private int CompareMiniCurrencyCurrencyPickerItems(MiniCurrencyCatalogListItem left, MiniCurrencyCatalogListItem right)
        {
            var leftCode = left?.Code ?? string.Empty;
            var rightCode = right?.Code ?? string.Empty;
            var leftPreferred = string.Equals(leftCode, _miniCurrencyPreferredUiCurrencyCode, StringComparison.OrdinalIgnoreCase);
            var rightPreferred = string.Equals(rightCode, _miniCurrencyPreferredUiCurrencyCode, StringComparison.OrdinalIgnoreCase);
            if (leftPreferred != rightPreferred)
            {
                return leftPreferred ? -1 : 1;
            }

            var leftCrypto = IsMiniCurrencyCurrencyPickerCrypto(leftCode);
            var rightCrypto = IsMiniCurrencyCurrencyPickerCrypto(rightCode);
            if (leftCrypto != rightCrypto)
            {
                return leftCrypto ? 1 : -1;
            }

            var alphabetNameCompare = string.Compare(left?.Name, right?.Name, StringComparison.CurrentCultureIgnoreCase);
            if (alphabetNameCompare != 0)
            {
                return alphabetNameCompare;
            }

            return string.Compare(leftCode, rightCode, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMiniCurrencyCurrencyPickerCrypto(string code)
        {
            return _miniCurrencyCurrencyPickerCryptoCodes.Contains((code ?? string.Empty).Trim().ToUpperInvariant());
        }

        private static string GetMiniCurrencyPreferredUiCurrencyCode()
        {
            var languageTag = ApplicationLanguages.Languages.FirstOrDefault() ?? string.Empty;
            languageTag = languageTag.Trim().ToLowerInvariant();

            if (languageTag.StartsWith("ru"))
            {
                return "RUB";
            }

            if (languageTag.StartsWith("kk"))
            {
                return "KZT";
            }

            if (languageTag.StartsWith("tr"))
            {
                return "TRY";
            }

            if (languageTag.StartsWith("uk"))
            {
                return "UAH";
            }

            if (languageTag.StartsWith("zh"))
            {
                return "CNY";
            }

            if (languageTag.StartsWith("ja"))
            {
                return "JPY";
            }

            if (languageTag.StartsWith("ko"))
            {
                return "KRW";
            }

            if (languageTag.StartsWith("nb") || languageTag.StartsWith("nn") || languageTag.StartsWith("no"))
            {
                return "NOK";
            }

            if (languageTag.StartsWith("ar"))
            {
                return "AED";
            }

            if (languageTag.StartsWith("pl"))
            {
                return "PLN";
            }

            if (languageTag.StartsWith("en"))
            {
                return "USD";
            }

            if (languageTag.StartsWith("de") || languageTag.StartsWith("fr") || languageTag.StartsWith("es") ||
                languageTag.StartsWith("it") || languageTag.StartsWith("pt") || languageTag.StartsWith("nl"))
            {
                return "EUR";
            }

            return "USD";
        }

        private async Task<IReadOnlyList<MiniCurrencyCatalogListItem>> LoadMiniCurrencyCurrencyPickerCatalogAsync()
        {
            var list = new List<MiniCurrencyCatalogListItem>();
            var knownCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/currencies.json"));
                var text = await FileIO.ReadTextAsync(file);
                var arr = JsonArray.Parse(text);

                foreach (var item in arr)
                {
                    if (item.ValueType != JsonValueType.Object)
                    {
                        continue;
                    }

                    var obj = item.GetObject();
                    if (!obj.TryGetValue("code", out var codeJson) || !obj.TryGetValue("name", out var nameJson))
                    {
                        continue;
                    }

                    var code = codeJson.GetString()?.Trim().ToUpperInvariant();
                    var name = nameJson.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!knownCodes.Add(code))
                    {
                        continue;
                    }

                    list.Add(new MiniCurrencyCatalogListItem
                    {
                        Code = code,
                        Name = name
                    });
                }
            }
            catch
            {
                foreach (var pair in _miniCurrencyDisplayNames)
                {
                    if (!knownCodes.Add(pair.Key))
                    {
                        continue;
                    }

                    list.Add(new MiniCurrencyCatalogListItem
                    {
                        Code = pair.Key,
                        Name = pair.Value
                    });
                }
            }

            return list;
        }

        private FrameworkElement CreateMiniCurrencyCurrencyPickerListItem(MiniCurrencyCatalogListItem currency)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 3, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Tag = currency.Code
            };

            row.Tapped += MiniCurrencyCurrencyPickerRow_Tapped;
            row.PointerEntered += MiniCurrencyCurrencyPickerRow_PointerEntered;
            row.PointerExited += MiniCurrencyCurrencyPickerRow_PointerExited;

            var card = new Border
            {
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(0.8),
                Margin = new Thickness(0, 0, 14, 0),
                Padding = new Thickness(10, 6, 10, 6)
            };
            ApplyMiniCurrencyCurrencyPickerRowVisual(card, currency.Code, hovered: false);

            var label = CreateMiniCurrencyCurrencyPickerLabel(currency);
            card.Child = label;
            row.Children.Add(card);
            return row;
        }

        private FrameworkElement CreateMiniCurrencyCurrencyPickerLabel(MiniCurrencyCatalogListItem currency)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var flagHost = new Grid
            {
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var placeholder = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(Color.FromArgb(255, 39, 167, 229)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            flagHost.Children.Add(placeholder);

            var flagAssetCode = GetMiniCurrencyFlagAssetCode(currency.Code);
            if (!string.IsNullOrWhiteSpace(flagAssetCode))
            {
                var flagImage = new Image
                {
                    Width = 18,
                    Height = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = Stretch.UniformToFill,
                    Tag = new MiniCurrencyCurrencyPickerFlagTag
                    {
                        Placeholder = placeholder,
                        Code = flagAssetCode
                    }
                };

                flagImage.Loaded += MiniCurrencyCurrencyPickerFlagImage_Loaded;
                flagImage.Unloaded += MiniCurrencyCurrencyPickerFlagImage_Unloaded;
                flagHost.Children.Add(flagImage);
            }

            panel.Children.Add(flagHost);
            panel.Children.Add(new TextBlock
            {
                Text = $"{currency.Name} ({currency.Code})",
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords,
                FontSize = 14
            });

            return panel;
        }

        private static Border GetMiniCurrencyCurrencyPickerRowCard(FrameworkElement element)
        {
            if (element is Grid row && row.Children.Count > 0)
            {
                return row.Children[0] as Border;
            }

            return null;
        }

        private void RefreshMiniCurrencyCurrencyPickerRowVisualStates()
        {
            if (MiniCurrencyCurrencyPickerListView == null)
            {
                return;
            }

            foreach (var item in MiniCurrencyCurrencyPickerListView.Items)
            {
                if (!(item is FrameworkElement rowElement) || !(rowElement.Tag is string code))
                {
                    continue;
                }

                var card = GetMiniCurrencyCurrencyPickerRowCard(rowElement);
                if (card != null)
                {
                    ApplyMiniCurrencyCurrencyPickerRowVisual(card, code, hovered: false);
                }
            }
        }

        private void ApplyMiniCurrencyCurrencyPickerRowVisual(Border card, string code, bool hovered)
        {
            if (card == null)
            {
                return;
            }

            var panelBackground = GetMiniCurrencyCurrencyPickerPanelBackgroundColor();

            if (IsMiniCurrencyVisible(code))
            {
                var accent = ThemeSettingsService.AppAccentColor;
                var selectedBackground = Color.FromArgb(122, accent.R, accent.G, accent.B);
                card.Background = new SolidColorBrush(selectedBackground);
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(hovered ? (byte)236 : (byte)214, accent.R, accent.G, accent.B));
                ApplyMiniCurrencyCurrencyPickerRowTextColor(
                    card,
                    BlendMiniCurrencyColorOverBackground(selectedBackground, panelBackground));
                return;
            }

            var defaultBackground = hovered
                ? Color.FromArgb(210, 68, 72, 78)
                : Color.FromArgb(178, 44, 47, 52);
            card.Background = new SolidColorBrush(defaultBackground);
            card.BorderBrush = hovered
                ? new SolidColorBrush(Color.FromArgb(128, 96, 101, 108))
                : new SolidColorBrush(Color.FromArgb(96, 84, 88, 94));
            ApplyMiniCurrencyCurrencyPickerRowTextColor(
                card,
                BlendMiniCurrencyColorOverBackground(defaultBackground, panelBackground));
        }

        private static void ApplyMiniCurrencyCurrencyPickerRowTextColor(Border card, Color backgroundColor)
        {
            if (card?.Child is StackPanel panel)
            {
                var label = panel.Children.OfType<TextBlock>().FirstOrDefault();
                if (label != null)
                {
                    label.Foreground = new SolidColorBrush(GetMiniCurrencyAdaptiveTextColor(backgroundColor));
                }
            }
        }

        private Color GetMiniCurrencyCurrencyPickerPanelBackgroundColor()
        {
            if (MiniCurrencyCurrencyPickerPane?.Background is SolidColorBrush paneBrush)
            {
                return paneBrush.Color;
            }

            if (MiniCurrencyCurrencyPickerListView?.Background is SolidColorBrush listBrush &&
                listBrush.Color.A > 0)
            {
                return listBrush.Color;
            }

            return Color.FromArgb(255, 34, 34, 34);
        }

        private static Color BlendMiniCurrencyColorOverBackground(Color foreground, Color background)
        {
            if (foreground.A >= 255)
            {
                return foreground;
            }

            if (foreground.A <= 0)
            {
                return background;
            }

            var alpha = foreground.A / 255.0;
            var inverse = 1.0 - alpha;

            return Color.FromArgb(
                255,
                (byte)Math.Round((foreground.R * alpha) + (background.R * inverse)),
                (byte)Math.Round((foreground.G * alpha) + (background.G * inverse)),
                (byte)Math.Round((foreground.B * alpha) + (background.B * inverse)));
        }

        private void MiniCurrencyCurrencyPickerRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            if (!(sender is FrameworkElement element) || !(element.Tag is string targetCode))
            {
                return;
            }

            HandleMiniCurrencyCurrencyPickerSelection(targetCode);
        }

        private void MiniCurrencyCurrencyPickerRow_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is FrameworkElement rowElement) || !(rowElement.Tag is string code))
            {
                return;
            }

            var card = GetMiniCurrencyCurrencyPickerRowCard(rowElement);
            if (card != null)
            {
                ApplyMiniCurrencyCurrencyPickerRowVisual(card, code, hovered: true);
            }
        }

        private void MiniCurrencyCurrencyPickerRow_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is FrameworkElement rowElement) || !(rowElement.Tag is string code))
            {
                return;
            }

            var card = GetMiniCurrencyCurrencyPickerRowCard(rowElement);
            if (card != null)
            {
                ApplyMiniCurrencyCurrencyPickerRowVisual(card, code, hovered: false);
            }
        }

        private void MiniCurrencyCurrencyPickerFlagImage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Image image))
            {
                return;
            }

            image.Visibility = Visibility.Visible;
            ResetMiniCurrencyCurrencyPickerFlagPlaceholder(image);

            if (image.Source != null)
            {
                return;
            }

            if (!(image.Tag is MiniCurrencyCurrencyPickerFlagTag tag) || string.IsNullOrWhiteSpace(tag.Code))
            {
                image.Visibility = Visibility.Collapsed;
                return;
            }

            TrySetMiniCurrencyCurrencyPickerFlagSource(image, tag.Code, () =>
            {
                image.Visibility = Visibility.Collapsed;
            });
        }

        private void MiniCurrencyCurrencyPickerFlagImage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is Image image))
            {
                return;
            }

            image.Source = null;
            image.Visibility = Visibility.Visible;
            ResetMiniCurrencyCurrencyPickerFlagPlaceholder(image);
        }

        private static void TrySetMiniCurrencyCurrencyPickerFlagSource(Image image, string normalizedCode, Action onFailed)
        {
            if (image == null || string.IsNullOrWhiteSpace(normalizedCode))
            {
                onFailed?.Invoke();
                return;
            }

            PrepareMiniCurrencyFlagImage(image);
            try
            {
                var svgSource = new SvgImageSource();
                ConfigureMiniCurrencyFlagSvgSource(svgSource, image);
                svgSource.OpenFailed += (s, e) => onFailed?.Invoke();
                svgSource.Opened += (s, e) => MakeMiniCurrencyFlagContainerTransparent(image);
                svgSource.UriSource = new Uri($"ms-appx:///Assets/Flags/{normalizedCode}.svg");
                image.Source = svgSource;
            }
            catch
            {
                onFailed?.Invoke();
            }
        }

        private static void ResetMiniCurrencyCurrencyPickerFlagPlaceholder(Image image)
        {
            var placeholderBrush = new SolidColorBrush(Color.FromArgb(255, 39, 167, 229));

            if (image?.Tag is MiniCurrencyCurrencyPickerFlagTag tag && tag.Placeholder != null)
            {
                tag.Placeholder.Background = placeholderBrush;
            }
        }

        private void MiniCurrencyCurrencyPickerSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox))
            {
                return;
            }

            _miniCurrencyCurrencyPickerSearchQuery = textBox.Text ?? string.Empty;
            RefreshMiniCurrencyCurrencyPickerList();
            UpdateMiniCurrencyCurrencyPickerSearchIconVisibility();
        }

        private void MiniCurrencyCurrencyPickerSearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateMiniCurrencyCurrencyPickerSearchIconVisibility();
        }

        private void MiniCurrencyCurrencyPickerSearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateMiniCurrencyCurrencyPickerSearchIconVisibility();
        }

        private void UpdateMiniCurrencyCurrencyPickerSearchIconVisibility()
        {
            if (MiniCurrencyCurrencyPickerSearchIcon == null || MiniCurrencyCurrencyPickerSearchTextBox == null)
            {
                return;
            }

            var hasFocus = MiniCurrencyCurrencyPickerSearchTextBox.FocusState != FocusState.Unfocused;
            MiniCurrencyCurrencyPickerSearchIcon.Visibility = hasFocus
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void HandleMiniCurrencyCurrencyPickerSelection(string targetCode)
        {
            targetCode = (targetCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(targetCode))
            {
                return;
            }

            if (_miniCurrencyCurrencyPickerMode == MiniCurrencyCurrencyPickerMode.ToggleMainVisibility)
            {
                ToggleMiniCurrencyCurrencyVisibilityFromPicker(targetCode);
                return;
            }

            if (string.IsNullOrWhiteSpace(_miniCurrencyCurrencyPickerSourceCode))
            {
                return;
            }

            if (TryReplaceMiniCurrencyRowCurrency(_miniCurrencyCurrencyPickerSourceCode, targetCode))
            {
                RootSplitView.IsPaneOpen = false;
            }
        }

        private void ToggleMiniCurrencyCurrencyVisibilityFromPicker(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            var shouldBecomeVisible = !IsMiniCurrencyVisible(code);
            if (!SetMiniCurrencyCurrencyVisibility(code, shouldBecomeVisible, updateActiveSelection: false))
            {
                return;
            }

            RefreshMiniCurrencyCurrencyManagerMenu();
            RefreshMiniCurrencyCurrencyPickerRowVisualStates();
        }

        private bool TryReplaceMiniCurrencyRowCurrency(string sourceCode, string targetCode)
        {
            sourceCode = (sourceCode ?? string.Empty).Trim().ToUpperInvariant();
            targetCode = (targetCode ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(sourceCode) || string.IsNullOrWhiteSpace(targetCode))
            {
                return false;
            }

            if (!_miniCurrencyRows.TryGetValue(sourceCode, out var sourceRow) ||
                !_miniCurrencyInputs.TryGetValue(sourceCode, out var sourceInput))
            {
                return false;
            }

            if (sourceCode == targetCode)
            {
                return true;
            }

            if (!_miniCurrencyDisplayNames.ContainsKey(targetCode))
            {
                SetMiniCurrencyStatus($"Валюта {targetCode} отсутствует в каталоге");
                return false;
            }

            if (_miniCurrencyRows.TryGetValue(targetCode, out var existingTargetRow))
            {
                if (existingTargetRow.Visibility == Visibility.Visible)
                {
                    SetMiniCurrencyStatus($"Валюта {targetCode} уже показана");
                    return false;
                }

                var sourceIndex = CurrencyRowsHost?.Children.IndexOf(sourceRow) ?? -1;
                sourceRow.Visibility = Visibility.Collapsed;
                existingTargetRow.Visibility = Visibility.Visible;

                if (_miniCurrencyInputs.TryGetValue(targetCode, out var targetInput))
                {
                    _miniCurrencyIsUpdating = true;
                    try
                    {
                        targetInput.Text = sourceInput.Text;
                    }
                    finally
                    {
                        _miniCurrencyIsUpdating = false;
                    }
                }

                if (CurrencyRowsHost != null && sourceIndex >= 0)
                {
                    var targetIndex = CurrencyRowsHost.Children.IndexOf(existingTargetRow);
                    if (targetIndex >= 0 && targetIndex != sourceIndex)
                    {
                        CurrencyRowsHost.Children.RemoveAt(targetIndex);
                        if (targetIndex < sourceIndex)
                        {
                            sourceIndex--;
                        }

                        sourceIndex = Math.Max(0, Math.Min(sourceIndex, CurrencyRowsHost.Children.Count));
                        CurrencyRowsHost.Children.Insert(sourceIndex, existingTargetRow);
                    }
                }

                if (_miniCurrencyActiveCode == sourceCode)
                {
                    _miniCurrencyActiveCode = targetCode;
                }

                HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
                SetMiniCurrencyStatus($"Валюта {sourceCode} заменена на {targetCode}");
                SaveMiniCurrencyVisibleCurrencies();
                SaveMiniCurrencyRowOrder();
                RebuildMiniCurrencyAddList();
                ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                return true;
            }

            _miniCurrencyRows.Remove(sourceCode);
            _miniCurrencyRows[targetCode] = sourceRow;

            _miniCurrencyInputs.Remove(sourceCode);
            _miniCurrencyInputs[targetCode] = sourceInput;

            if (!_miniCurrencyRates.ContainsKey(targetCode))
            {
                _miniCurrencyRates[targetCode] = double.NaN;
            }

            UpdateMiniCurrencyRowIdentity(sourceRow, sourceInput, targetCode);

            if (_miniCurrencyActiveCode == sourceCode)
            {
                _miniCurrencyActiveCode = targetCode;
            }

            HighlightMiniCurrencyActiveRow(_miniCurrencyActiveCode);
            SetMiniCurrencyStatus($"Валюта {sourceCode} заменена на {targetCode}");
            SaveMiniCurrencyVisibleCurrencies();
            SaveMiniCurrencyRowOrder();
            SaveMiniCurrencyValues();
            RebuildMiniCurrencyAddList();
            ConvertFromMiniCurrency(_miniCurrencyActiveCode);
            return true;
        }

        private void UpdateMiniCurrencyRowIdentity(FrameworkElement rowElement, TextBox input, string code)
        {
            if (rowElement == null || input == null || string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            code = code.Trim().ToUpperInvariant();
            rowElement.Tag = code;
            input.Tag = code;

            if (!(rowElement is Grid row))
            {
                return;
            }

            Border leftBorder = row.Children.Count > 0 ? row.Children[0] as Border : null;
            if (leftBorder == null)
            {
                leftBorder = row.Children.OfType<Border>().FirstOrDefault(x => Grid.GetColumn(x) == 0);
            }

            if (leftBorder?.Child is StackPanel leftStack)
            {
                var codeText = leftStack.Children.OfType<TextBlock>().FirstOrDefault();
                if (codeText != null)
                {
                    codeText.Text = code;
                }

                if (leftStack.Children.Count > 0 &&
                    leftStack.Children[0] is Border flagCircle &&
                    flagCircle.Child is Image flagImage)
                {
                    SetMiniCurrencyFlag(flagImage, code);
                }
            }

            var removeButton = row.Children
                .OfType<Button>()
                .FirstOrDefault(x => Grid.GetColumn(x) == 3);
            if (removeButton != null)
            {
                removeButton.Tag = code;
            }

            AttachMiniCurrencyCodeBlockTapHandler(row, code);
            ApplyMiniCurrencyUiScaleToRow(row);
        }

        private void AttachMiniCurrencyCodeBlockTapHandlersForAllRows()
        {
            foreach (var pair in _miniCurrencyRows)
            {
                AttachMiniCurrencyCodeBlockTapHandler(pair.Value, pair.Key);
            }
        }

        private void AttachMiniCurrencyCodeBlockTapHandler(FrameworkElement rowElement, string code)
        {
            if (!(rowElement is Grid row) || string.IsNullOrWhiteSpace(code))
            {
                return;
            }

            Border leftBorder = row.Children.Count > 0 ? row.Children[0] as Border : null;
            if (leftBorder == null)
            {
                leftBorder = row.Children.OfType<Border>().FirstOrDefault(x => Grid.GetColumn(x) == 0);
            }

            if (leftBorder == null)
            {
                return;
            }

            leftBorder.Tag = code.Trim().ToUpperInvariant();
            leftBorder.Tapped -= MiniCurrencyCodeBlock_Tapped;
            leftBorder.Tapped += MiniCurrencyCodeBlock_Tapped;
        }

        private async void MiniCurrencyCodeBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DateTimeOffset.UtcNow < _miniCurrencyIgnoreRowTapUntilUtc)
            {
                e.Handled = true;
                return;
            }

            e.Handled = true;

            if (!(sender is FrameworkElement element) || !(element.Tag is string code))
            {
                return;
            }

            await OpenMiniCurrencyCurrencyPickerPaneAsync(code);
        }

        private void ApplyMiniCurrencyMainMenuMode()
        {
            if (!IsMiniCurrencyMode || MainMenuButtonFlyout == null)
            {
                return;
            }

            MenuCreateNewButton.Visibility = Visibility.Collapsed;
            MenuCreateNewWindowButton.Visibility = Visibility.Collapsed;
            MenuOpenFileButton.Visibility = Visibility.Collapsed;
            MenuSaveButton.Visibility = Visibility.Collapsed;
            MenuSaveAsButton.Visibility = Visibility.Collapsed;
            MenuSaveAllButton.Visibility = Visibility.Collapsed;
            MenuFindButton.Visibility = Visibility.Collapsed;
            MenuReplaceButton.Visibility = Visibility.Collapsed;
            MenuPrintButton.Visibility = Visibility.Collapsed;
            MenuPrintAllButton.Visibility = Visibility.Collapsed;

            MenuFullScreenSeparator.Visibility = Visibility.Collapsed;
            MenuPrintSeparator.Visibility = Visibility.Collapsed;
            MenuSettingsSeparator.Visibility = Visibility.Collapsed;

            if (App.IsGameBarWidget)
            {
                MenuFullScreenButton.Visibility = Visibility.Collapsed;
                MenuCompactOverlayButton.Visibility = Visibility.Collapsed;
                MenuManageCurrenciesButton.Visibility = Visibility.Collapsed;
                MenuShowCurrenciesButton.Visibility = Visibility.Collapsed;
                MenuShowCalculatorButton.Visibility = Visibility.Collapsed;
                MenuSettingsButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MenuFullScreenButton.Visibility = Visibility.Visible;
                MenuCompactOverlayButton.Visibility = Visibility.Visible;
                MenuManageCurrenciesButton.Visibility = Visibility.Visible;
                MenuShowCurrenciesButton.Visibility = Visibility.Visible;
                MenuShowCalculatorButton.Visibility = Visibility.Visible;
                MenuSettingsButton.Visibility = Visibility.Visible;
            }

            for (var i = MainMenuButtonFlyout.Items.Count - 1; i >= 0; i--)
            {
                if (MainMenuButtonFlyout.Items[i] is MenuFlyoutSubItem subItem && subItem.Name == "MenuOpenRecentlyUsedFileButton")
                {
                    MainMenuButtonFlyout.Items.RemoveAt(i);
                }
            }

            if (_miniCurrencyRefreshRatesMenuItem == null)
            {
                _miniCurrencyRefreshRatesMenuItem = new MenuFlyoutItem
                {
                    Text = "Обновить валюты",
                    Icon = new SymbolIcon(Symbol.Sync)
                };
                _miniCurrencyRefreshRatesMenuItem.KeyboardAccelerators.Add(new Windows.UI.Xaml.Input.KeyboardAccelerator
                {
                    Key = Windows.System.VirtualKey.F5,
                    IsEnabled = false
                });
                _miniCurrencyRefreshRatesMenuItem.Click += async (sender, args) => await LoadMiniCurrencyRatesAsync(silent: false);
            }

            if (!MainMenuButtonFlyout.Items.Contains(_miniCurrencyRefreshRatesMenuItem))
            {
                var insertIndex = MainMenuButtonFlyout.Items.IndexOf(MenuCompactOverlayButton);
                MainMenuButtonFlyout.Items.Insert(insertIndex >= 0 ? insertIndex + 1 : 0, _miniCurrencyRefreshRatesMenuItem);
            }

            foreach (var item in MainMenuButtonFlyout.Items.OfType<MenuFlyoutSeparator>())
            {
                item.Visibility = Visibility.Collapsed;
            }

            UpdateMiniCurrencyVisibilityMenuItemsVisual();
        }

        private void UpdateMiniCurrencyVisibilityMenuItemsVisual()
        {
            if (MenuShowCurrenciesButton != null)
            {
                MenuShowCurrenciesButton.KeyboardAcceleratorTextOverride =
                    AppSettingsService.MiniCurrencyShowCurrencies ? MiniCurrencyMainMenuCheckedMark : string.Empty;
            }

            if (MenuShowCalculatorButton != null)
            {
                MenuShowCalculatorButton.KeyboardAcceleratorTextOverride =
                    AppSettingsService.MiniCurrencyShowCalculator ? MiniCurrencyMainMenuCheckedMark : string.Empty;
            }
        }

        private void MenuShowCurrenciesButton_Click(object sender, RoutedEventArgs e)
        {
            _miniCurrencyKeepMainMenuFlyoutOpenOnce = true;
            AppSettingsService.MiniCurrencyShowCurrencies = !AppSettingsService.MiniCurrencyShowCurrencies;
        }

        private void MenuShowCalculatorButton_Click(object sender, RoutedEventArgs e)
        {
            _miniCurrencyKeepMainMenuFlyoutOpenOnce = true;
            AppSettingsService.MiniCurrencyShowCalculator = !AppSettingsService.MiniCurrencyShowCalculator;
        }

        private void MiniCurrencyShowCurrenciesChanged(object sender, bool show)
        {
            ApplyMiniCurrencyMainContentVisibility();
            UpdateMiniCurrencyVisibilityMenuItemsVisual();
        }

        private void MiniCurrencyShowCalculatorChanged(object sender, bool show)
        {
            ApplyMiniCurrencyMainContentVisibility();
            UpdateMiniCurrencyVisibilityMenuItemsVisual();
        }

        private ApplicationDataContainer MiniCurrencySettings => ApplicationData.Current.LocalSettings;

        private sealed class MiniCurrencyCatalogListItem
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        private sealed class MiniCurrencyCurrencyPickerFlagTag
        {
            public Border Placeholder { get; set; }
            public string Code { get; set; }
        }
    }
}


