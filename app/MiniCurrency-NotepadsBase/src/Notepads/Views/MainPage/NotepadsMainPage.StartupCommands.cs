// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Notepads.Commands;
    using Notepads.Services;
    using Notepads.Views.Settings;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media.Animation;

    public sealed partial class NotepadsMainPage
    {
        private void InitializeControls()
        {
            ToolTipService.SetToolTip(ExitCompactOverlayButton, _resourceLoader.GetString("App_ExitCompactOverlayMode_Text"));
            RootSplitView.PaneOpening += RootSplitView_PaneOpening;
            RootSplitView.PaneClosed += RootSplitView_PaneClosed;
            if (IsMiniCurrencyMode && RootSplitView != null)
            {
                _miniCurrencySettingsPaneWidth = RootSplitView.OpenPaneLength;
                _miniCurrencyCurrencyPickerPaneWidth = System.Math.Max(240, _miniCurrencySettingsPaneWidth - MiniCurrencyPickerPaneWidthReduction);
            }

            if (!IsMiniCurrencyMode)
            {
                NewSetButton.Click += delegate { NotepadsCore.OpenNewTextEditor(_defaultNewFileName); };
            }
        }

        private void RootSplitView_PaneOpening(SplitView sender, object args)
        {
            ApplyMiniCurrencyRightPaneWidthForCurrentMode();

            if (_miniCurrencyRightPaneMode == MiniCurrencyRightPaneMode.CurrencyPicker)
            {
                ShowMiniCurrencyCurrencyPickerPaneContent();
                return;
            }

            ShowSettingsPaneContent();
            SettingsFrame.Navigate(typeof(SettingsPage), null, new SuppressNavigationTransitionInfo());
        }

        private void RootSplitView_PaneClosed(SplitView sender, object args)
        {
            _miniCurrencyRightPaneMode = MiniCurrencyRightPaneMode.Settings;
            _miniCurrencyCurrencyPickerSourceCode = null;
            _miniCurrencyCurrencyPickerMode = MiniCurrencyCurrencyPickerMode.ReplaceRowCurrency;
            ShowSettingsPaneContent();
            ApplyMiniCurrencyRightPaneWidthForCurrentMode();

            if (!IsMiniCurrencyMode)
            {
                NotepadsCore.FocusOnSelectedTextEditor();
            }
        }

        private void ShowSettingsPaneContent()
        {
            if (SettingsFrame != null)
            {
                SettingsFrame.Visibility = Visibility.Visible;
            }

            if (MiniCurrencyCurrencyPickerPane != null)
            {
                MiniCurrencyCurrencyPickerPane.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowMiniCurrencyCurrencyPickerPaneContent()
        {
            if (MiniCurrencyCurrencyPickerPane != null)
            {
                MiniCurrencyCurrencyPickerPane.Visibility = Visibility.Visible;
            }

            if (SettingsFrame != null)
            {
                SettingsFrame.Visibility = Visibility.Collapsed;
            }
        }

        private void InitializeKeyboardShortcuts()
        {
            if (IsMiniCurrencyMode)
            {
                _keyboardCommandHandler = new KeyboardCommandHandler(new List<IKeyboardCommand<KeyRoutedEventArgs>>()
                {
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F11, (args) => EnterExitFullScreenMode()),
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F12, (args) => EnterExitCompactOverlayMode()),
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F2, async (args) => await OpenMiniCurrencyCurrencyManagementPaneAsync()),
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F5, async (args) => await LoadMiniCurrencyRatesAsync(silent: false)),
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.Escape, (args) => { if (RootSplitView.IsPaneOpen) RootSplitView.IsPaneOpen = false; }),
                    new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F1, (args) => { if (App.IsPrimaryInstance && !App.IsGameBarWidget) RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen; })
                });
                return;
            }

            _keyboardCommandHandler = new KeyboardCommandHandler(new List<IKeyboardCommand<KeyRoutedEventArgs>>()
            {
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.W, (args) => NotepadsCore.CloseTextEditor(NotepadsCore.GetSelectedTextEditor())),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Tab, (args) => NotepadsCore.SwitchTo(true)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.Tab, (args) => NotepadsCore.SwitchTo(false)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.N, (args) => NotepadsCore.OpenNewTextEditor(_defaultNewFileName)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.T, (args) => NotepadsCore.OpenNewTextEditor(_defaultNewFileName)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.O, async (args) => await OpenNewFilesAsync()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.S, async (args) => await SaveAsync(NotepadsCore.GetSelectedTextEditor(), saveAs: false, ignoreUnmodifiedDocument: true)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.S, async (args) => await SaveAsync(NotepadsCore.GetSelectedTextEditor(), saveAs: true)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.P, async (args) => await PrintAsync(NotepadsCore.GetSelectedTextEditor())),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.P, async (args) => await PrintAllAsync(NotepadsCore.GetAllTextEditors())),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.R, (args) => ReloadFileFromDiskAsync(this, new RoutedEventArgs())),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, true, VirtualKey.N, async (args) => await OpenNewAppInstanceAsync()),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number1, (args) => NotepadsCore.SwitchTo(0)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number2, (args) => NotepadsCore.SwitchTo(1)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number3, (args) => NotepadsCore.SwitchTo(2)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number4, (args) => NotepadsCore.SwitchTo(3)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number5, (args) => NotepadsCore.SwitchTo(4)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number6, (args) => NotepadsCore.SwitchTo(5)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number7, (args) => NotepadsCore.SwitchTo(6)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number8, (args) => NotepadsCore.SwitchTo(7)),
                new KeyboardCommand<KeyRoutedEventArgs>(true, false, false, VirtualKey.Number9, (args) => NotepadsCore.SwitchTo(8)),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F11, (args) => EnterExitFullScreenMode()),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F12, (args) => EnterExitCompactOverlayMode()),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.Escape, (args) => { if (RootSplitView.IsPaneOpen) RootSplitView.IsPaneOpen = false; }),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F1, (args) => { if (App.IsPrimaryInstance && !App.IsGameBarWidget) RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen; }),
                new KeyboardCommand<KeyRoutedEventArgs>(VirtualKey.F2, async (args) => await RenameFileAsync(NotepadsCore.GetSelectedTextEditor())),
                new KeyboardCommand<KeyRoutedEventArgs>(true, true, true, VirtualKey.L, async (args) => { await OpenFileAsync(LoggingService.GetLogFile(), rebuildOpenRecentItems: false); })
            });
        }

        private static async Task OpenNewAppInstanceAsync()
        {
            if (!await NotepadsProtocolService.LaunchProtocolAsync(NotepadsOperationProtocol.OpenNewInstance))
            {
                AnalyticsService.TrackEvent("FailedToOpenNewAppInstance");
            }
        }

    }
}
