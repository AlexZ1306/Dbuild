// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Services
{
    using System;
    using System.Text;
    using Notepads.Settings;
    using Notepads.Utilities;
    using Windows.UI;
    using Windows.UI.Text;
    using Windows.UI.Xaml;

    public static class AppSettingsService
    {
        public static event EventHandler<string> OnFontFamilyChanged;
        public static event EventHandler<FontStyle> OnFontStyleChanged;
        public static event EventHandler<FontWeight> OnFontWeightChanged;
        public static event EventHandler<int> OnFontSizeChanged;
        public static event EventHandler<TextWrapping> OnDefaultTextWrappingChanged;
        public static event EventHandler<bool> OnDefaultLineHighlighterViewStateChanged;
        public static event EventHandler<bool> OnDefaultDisplayLineNumbersViewStateChanged;
        public static event EventHandler<LineEnding> OnDefaultLineEndingChanged;
        public static event EventHandler<Encoding> OnDefaultEncodingChanged;
        public static event EventHandler<int> OnDefaultTabIndentsChanged;
        public static event EventHandler<bool> OnStatusBarVisibilityChanged;
        public static event EventHandler<int> OnMiniCurrencyUiScalePercentChanged;
        public static event EventHandler<int> OnMiniCurrencyActiveCardBackgroundOpacityPercentChanged;
        public static event EventHandler<int> OnMiniCurrencyCardBackgroundOpacityPercentChanged;
        public static event EventHandler<int> OnMiniCurrencyValueFontWeightChanged;
        public static event EventHandler<bool> OnMiniCurrencyUseDefaultInactiveCardColorChanged;
        public static event EventHandler<Color> OnMiniCurrencyInactiveCardColorChanged;
        public static event EventHandler<bool> OnMiniCurrencyCalculatorUseWindowsEqualsColorChanged;
        public static event EventHandler<Color> OnMiniCurrencyCalculatorEqualsColorChanged;
        public static event EventHandler<int> OnMiniCurrencyCalculatorEqualsButtonOpacityPercentChanged;
        public static event EventHandler<Color> OnMiniCurrencyCalculatorDigitTextColorChanged;
        public static event EventHandler<Color> OnMiniCurrencyCalculatorOperationTextColorChanged;
        public static event EventHandler<int> OnMiniCurrencyCalculatorButtonsOpacityPercentChanged;
        public static event EventHandler<bool> OnMiniCurrencyShowCurrenciesChanged;
        public static event EventHandler<bool> OnMiniCurrencyShowCalculatorChanged;
        public static event EventHandler<bool> OnSessionBackupAndRestoreOptionChanged;
        public static event EventHandler<bool> OnHighlightMisspelledWordsChanged;

        private static string _editorFontFamily;

        public static string EditorFontFamily
        {
            get => _editorFontFamily;
            set
            {
                _editorFontFamily = value;
                OnFontFamilyChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorFontFamilyStr, value);
            }
        }

        private static int _editorFontSize;

        public static int EditorFontSize
        {
            get => _editorFontSize;
            set
            {
                _editorFontSize = value;
                OnFontSizeChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorFontSizeInt, value);
            }
        }

        private static FontStyle _editorFontStyle;

        public static FontStyle EditorFontStyle
        {
            get => _editorFontStyle;
            set
            {
                _editorFontStyle = value;
                OnFontStyleChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorFontStyleStr, value.ToString());
            }
        }

        private static FontWeight _editorFontWeight;

        public static FontWeight EditorFontWeight
        {
            get => _editorFontWeight;
            set
            {
                _editorFontWeight = value;
                OnFontWeightChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorFontWeightUshort, value.Weight);
            }
        }

        private static TextWrapping _editorDefaultTextWrapping;

        public static TextWrapping EditorDefaultTextWrapping
        {
            get => _editorDefaultTextWrapping;
            set
            {
                _editorDefaultTextWrapping = value;
                OnDefaultTextWrappingChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultTextWrappingStr, value.ToString());
            }
        }

        private static bool _editorDisplayLineHighlighter;

        public static bool EditorDisplayLineHighlighter
        {
            get => _editorDisplayLineHighlighter;
            set
            {
                _editorDisplayLineHighlighter = value;
                OnDefaultLineHighlighterViewStateChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultLineHighlighterViewStateBool, value);
            }
        }

        private static LineEnding _editorDefaultLineEnding;

        public static LineEnding EditorDefaultLineEnding
        {
            get => _editorDefaultLineEnding;
            set
            {
                _editorDefaultLineEnding = value;
                OnDefaultLineEndingChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultLineEndingStr, value.ToString());
            }
        }

        private static Encoding _editorDefaultEncoding;

        public static Encoding EditorDefaultEncoding
        {
            get => _editorDefaultEncoding;
            set
            {
                _editorDefaultEncoding = value;

                if (value is UTF8Encoding)
                {
                    ApplicationSettingsStore.Write(SettingsKey.EditorDefaultUtf8EncoderShouldEmitByteOrderMarkBool,
                        Equals(value, new UTF8Encoding(true)));
                }

                OnDefaultEncodingChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultEncodingCodePageInt, value.CodePage);
            }
        }

        private static Encoding _editorDefaultDecoding;

        public static Encoding EditorDefaultDecoding
        {
            get
            {
                if (_editorDefaultDecoding == null)
                {
                    return null;
                }
                // If it is not UTF-8 meaning user is using ANSI decoding,
                // We should always try get latest system ANSI code page.
                else if (!(_editorDefaultDecoding is UTF8Encoding))
                {
                    if (EncodingUtility.TryGetSystemDefaultANSIEncoding(out var systemDefaultANSIEncoding))
                    {
                        _editorDefaultDecoding = systemDefaultANSIEncoding;
                    }
                    else if (EncodingUtility.TryGetCurrentCultureANSIEncoding(out var currentCultureANSIEncoding))
                    {
                        _editorDefaultDecoding = currentCultureANSIEncoding;
                    }
                    else
                    {
                        _editorDefaultDecoding = new UTF8Encoding(false); // Fall back to UTF-8 (no BOM)
                    }
                }
                return _editorDefaultDecoding;
            }
            set
            {
                _editorDefaultDecoding = value;
                var codePage = value?.CodePage ?? -1;
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultDecodingCodePageInt, codePage);
            }
        }

        private static int _editorDefaultTabIndents;

        public static int EditorDefaultTabIndents
        {
            get => _editorDefaultTabIndents;
            set
            {
                _editorDefaultTabIndents = value;
                OnDefaultTabIndentsChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultTabIndentsInt, value);
            }
        }

        private static SearchEngine _editorDefaultSearchEngine;

        public static SearchEngine EditorDefaultSearchEngine
        {
            get => _editorDefaultSearchEngine;
            set
            {
                _editorDefaultSearchEngine = value;
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultSearchEngineStr, value.ToString());
            }
        }

        private static string _editorCustomMadeSearchUrl;

        public static string EditorCustomMadeSearchUrl
        {
            get => _editorCustomMadeSearchUrl;
            set
            {
                _editorCustomMadeSearchUrl = value;
                ApplicationSettingsStore.Write(SettingsKey.EditorCustomMadeSearchUrlStr, value);
            }
        }

        private static bool _showStatusBar;

        private static int _miniCurrencyUiScalePercent = 50;
        private static int _miniCurrencyActiveCardBackgroundOpacityPercent = 100;
        private static int _miniCurrencyCardBackgroundOpacityPercent = 100;
        private static int _miniCurrencyValueFontWeight = 400;
        private static bool _miniCurrencyUseDefaultInactiveCardColor = true;
        private static Color _miniCurrencyInactiveCardColor = Color.FromArgb(255, 58, 58, 58);
        private static bool _miniCurrencyCalculatorUseWindowsEqualsColor = true;
        private static Color _miniCurrencyCalculatorEqualsColor = Color.FromArgb(255, 28, 156, 203);
        private static int _miniCurrencyCalculatorEqualsButtonOpacityPercent = 100;
        private static Color _miniCurrencyCalculatorDigitTextColor = Color.FromArgb(255, 58, 58, 58);
        private static Color _miniCurrencyCalculatorOperationTextColor = Color.FromArgb(255, 58, 58, 58);
        private static int _miniCurrencyCalculatorButtonsOpacityPercent = 100;
        private static bool _miniCurrencyShowCurrencies = true;
        private static bool _miniCurrencyShowCalculator = true;

        public static bool ShowStatusBar
        {
            get => _showStatusBar;
            set
            {
                _showStatusBar = value;
                OnStatusBarVisibilityChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorShowStatusBarBool, value);
            }
        }

        public static int MiniCurrencyUiScalePercent
        {
            get => _miniCurrencyUiScalePercent;
            set
            {
                var normalized = Math.Max(0, Math.Min(100, value));
                if (_miniCurrencyUiScalePercent == normalized) return;
                _miniCurrencyUiScalePercent = normalized;
                OnMiniCurrencyUiScalePercentChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyUiScalePercentInt, normalized);
            }
        }

        public static int MiniCurrencyCardBackgroundOpacityPercent
        {
            get => _miniCurrencyCardBackgroundOpacityPercent;
            set
            {
                var normalized = Math.Max(0, Math.Min(100, value));
                if (_miniCurrencyCardBackgroundOpacityPercent == normalized) return;
                _miniCurrencyCardBackgroundOpacityPercent = normalized;
                OnMiniCurrencyCardBackgroundOpacityPercentChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCardBackgroundOpacityPercentInt, normalized);
            }
        }

        public static int MiniCurrencyActiveCardBackgroundOpacityPercent
        {
            get => _miniCurrencyActiveCardBackgroundOpacityPercent;
            set
            {
                var normalized = Math.Max(0, Math.Min(100, value));
                if (_miniCurrencyActiveCardBackgroundOpacityPercent == normalized) return;
                _miniCurrencyActiveCardBackgroundOpacityPercent = normalized;
                OnMiniCurrencyActiveCardBackgroundOpacityPercentChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyActiveCardBackgroundOpacityPercentInt, normalized);
            }
        }

        public static int MiniCurrencyValueFontWeight
        {
            get => _miniCurrencyValueFontWeight;
            set
            {
                var normalized = Math.Max(100, Math.Min(900, value));
                normalized = (int)Math.Round(normalized / 100.0) * 100;
                if (_miniCurrencyValueFontWeight == normalized) return;
                _miniCurrencyValueFontWeight = normalized;
                OnMiniCurrencyValueFontWeightChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyValueFontWeightInt, normalized);
            }
        }

        public static bool MiniCurrencyUseDefaultInactiveCardColor
        {
            get => _miniCurrencyUseDefaultInactiveCardColor;
            set
            {
                if (_miniCurrencyUseDefaultInactiveCardColor == value) return;
                _miniCurrencyUseDefaultInactiveCardColor = value;
                OnMiniCurrencyUseDefaultInactiveCardColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyUseDefaultInactiveCardColorBool, value);
            }
        }

        public static Color MiniCurrencyInactiveCardColor
        {
            get => _miniCurrencyInactiveCardColor;
            set
            {
                _miniCurrencyInactiveCardColor = value;
                OnMiniCurrencyInactiveCardColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyInactiveCardColorArgbUInt, PackColor(value));
            }
        }

        public static bool MiniCurrencyCalculatorUseWindowsEqualsColor
        {
            get => _miniCurrencyCalculatorUseWindowsEqualsColor;
            set
            {
                if (_miniCurrencyCalculatorUseWindowsEqualsColor == value) return;
                _miniCurrencyCalculatorUseWindowsEqualsColor = value;
                OnMiniCurrencyCalculatorUseWindowsEqualsColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorUseWindowsEqualsColorBool, value);
            }
        }

        public static Color MiniCurrencyCalculatorEqualsColor
        {
            get => _miniCurrencyCalculatorEqualsColor;
            set
            {
                _miniCurrencyCalculatorEqualsColor = value;
                OnMiniCurrencyCalculatorEqualsColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorEqualsColorArgbUInt, PackColor(value));
            }
        }

        public static int MiniCurrencyCalculatorEqualsButtonOpacityPercent
        {
            get => _miniCurrencyCalculatorEqualsButtonOpacityPercent;
            set
            {
                var normalized = Math.Max(0, Math.Min(100, value));
                if (_miniCurrencyCalculatorEqualsButtonOpacityPercent == normalized) return;
                _miniCurrencyCalculatorEqualsButtonOpacityPercent = normalized;
                OnMiniCurrencyCalculatorEqualsButtonOpacityPercentChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorEqualsButtonOpacityPercentInt, normalized);
            }
        }

        public static Color MiniCurrencyCalculatorDigitTextColor
        {
            get => _miniCurrencyCalculatorDigitTextColor;
            set
            {
                _miniCurrencyCalculatorDigitTextColor = value;
                OnMiniCurrencyCalculatorDigitTextColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorDigitTextColorArgbUInt, PackColor(value));
            }
        }

        public static Color MiniCurrencyCalculatorOperationTextColor
        {
            get => _miniCurrencyCalculatorOperationTextColor;
            set
            {
                _miniCurrencyCalculatorOperationTextColor = value;
                OnMiniCurrencyCalculatorOperationTextColorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorOperationTextColorArgbUInt, PackColor(value));
            }
        }

        public static int MiniCurrencyCalculatorButtonsOpacityPercent
        {
            get => _miniCurrencyCalculatorButtonsOpacityPercent;
            set
            {
                var normalized = Math.Max(0, Math.Min(100, value));
                if (_miniCurrencyCalculatorButtonsOpacityPercent == normalized) return;
                _miniCurrencyCalculatorButtonsOpacityPercent = normalized;
                OnMiniCurrencyCalculatorButtonsOpacityPercentChanged?.Invoke(null, normalized);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyCalculatorButtonsOpacityPercentInt, normalized);
            }
        }

        public static bool MiniCurrencyShowCurrencies
        {
            get => _miniCurrencyShowCurrencies;
            set
            {
                if (_miniCurrencyShowCurrencies == value) return;
                _miniCurrencyShowCurrencies = value;
                OnMiniCurrencyShowCurrenciesChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyShowCurrenciesBool, value);
            }
        }

        public static bool MiniCurrencyShowCalculator
        {
            get => _miniCurrencyShowCalculator;
            set
            {
                if (_miniCurrencyShowCalculator == value) return;
                _miniCurrencyShowCalculator = value;
                OnMiniCurrencyShowCalculatorChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.MiniCurrencyShowCalculatorBool, value);
            }
        }

        private static bool _isSessionSnapshotEnabled;

        public static bool IsSessionSnapshotEnabled
        {
            get => _isSessionSnapshotEnabled;
            set
            {
                _isSessionSnapshotEnabled = value;
                OnSessionBackupAndRestoreOptionChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorEnableSessionBackupAndRestoreBool, value);
            }
        }

        private static bool _isHighlightMisspelledWordsEnabled;

        public static bool IsHighlightMisspelledWordsEnabled
        {
            get => _isHighlightMisspelledWordsEnabled;
            set
            {
                _isHighlightMisspelledWordsEnabled = value;
                OnHighlightMisspelledWordsChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorHighlightMisspelledWordsBool, value);
            }
        }

        private static bool _exitWhenLastTabClosed;

        public static bool ExitWhenLastTabClosed
        {
            get => _exitWhenLastTabClosed;
            set
            {
                _exitWhenLastTabClosed = value;
                ApplicationSettingsStore.Write(SettingsKey.ExitWhenLastTabClosed, value);
            }
        }

        private static bool _alwaysOpenNewWindow;

        public static bool AlwaysOpenNewWindow
        {
            get => _alwaysOpenNewWindow;
            set
            {
                _alwaysOpenNewWindow = value;
                ApplicationSettingsStore.Write(SettingsKey.AlwaysOpenNewWindowBool, value);
            }
        }

        private static bool _displayLineNumbers;

        public static bool EditorDisplayLineNumbers
        {
            get => _displayLineNumbers;
            set
            {
                _displayLineNumbers = value;
                OnDefaultDisplayLineNumbersViewStateChanged?.Invoke(null, value);
                ApplicationSettingsStore.Write(SettingsKey.EditorDefaultDisplayLineNumbersBool, value);
            }
        }

        private static bool _isSmartCopyEnabled;

        public static bool IsSmartCopyEnabled
        {
            get => _isSmartCopyEnabled;
            set
            {
                _isSmartCopyEnabled = value;
                ApplicationSettingsStore.Write(SettingsKey.EditorEnableSmartCopyBool, value);
            }
        }

        public static void Initialize()
        {
            InitializeFontSettings();

            InitializeTextWrappingSettings();

            InitializeSpellingSettings();

            InitializeDisplaySettings();

            InitializeSmartCopySettings();

            InitializeLineEndingSettings();

            InitializeEncodingSettings();

            InitializeDecodingSettings();

            InitializeTabIndentsSettings();

            InitializeSearchEngineSettings();

            InitializeStatusBarSettings();

            InitializeMiniCurrencyUiScaleSettings();
            InitializeMiniCurrencyVisualSettings();

            InitializeSessionSnapshotSettings();

            InitializeAppOpeningPreferencesSettings();

            InitializeAppClosingPreferencesSettings();
        }

        private static void InitializeStatusBarSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorShowStatusBarBool) is bool showStatusBar)
            {
                _showStatusBar = showStatusBar;
            }
            else
            {
                _showStatusBar = true;
            }
        }

        private static void InitializeMiniCurrencyUiScaleSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyUiScalePercentInt) is int miniCurrencyUiScalePercent)
            {
                _miniCurrencyUiScalePercent = Math.Max(0, Math.Min(100, miniCurrencyUiScalePercent));
            }
            else
            {
                _miniCurrencyUiScalePercent = 50;
            }
        }

        private static void InitializeMiniCurrencyVisualSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyActiveCardBackgroundOpacityPercentInt) is int miniCurrencyActiveCardOpacityPercent)
            {
                _miniCurrencyActiveCardBackgroundOpacityPercent = Math.Max(0, Math.Min(100, miniCurrencyActiveCardOpacityPercent));
            }
            else
            {
                _miniCurrencyActiveCardBackgroundOpacityPercent = 100;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCardBackgroundOpacityPercentInt) is int miniCurrencyCardOpacityPercent)
            {
                _miniCurrencyCardBackgroundOpacityPercent = Math.Max(0, Math.Min(100, miniCurrencyCardOpacityPercent));
            }
            else
            {
                _miniCurrencyCardBackgroundOpacityPercent = 100;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyValueFontWeightInt) is int miniCurrencyValueFontWeight)
            {
                var normalized = Math.Max(100, Math.Min(900, miniCurrencyValueFontWeight));
                _miniCurrencyValueFontWeight = (int)Math.Round(normalized / 100.0) * 100;
            }
            else
            {
                _miniCurrencyValueFontWeight = 400;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyUseDefaultInactiveCardColorBool) is bool useDefaultInactiveCardColor)
            {
                _miniCurrencyUseDefaultInactiveCardColor = useDefaultInactiveCardColor;
            }
            else
            {
                _miniCurrencyUseDefaultInactiveCardColor = true;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyInactiveCardColorArgbUInt) is uint inactiveCardColorArgb)
            {
                _miniCurrencyInactiveCardColor = UnpackColor(inactiveCardColorArgb);
            }
            else if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyInactiveCardColorArgbUInt) is int inactiveCardColorArgbInt)
            {
                _miniCurrencyInactiveCardColor = UnpackColor(unchecked((uint)inactiveCardColorArgbInt));
            }
            else
            {
                _miniCurrencyInactiveCardColor = Color.FromArgb(255, 58, 58, 58);
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorUseWindowsEqualsColorBool) is bool useWindowsEqualsColor)
            {
                _miniCurrencyCalculatorUseWindowsEqualsColor = useWindowsEqualsColor;
            }
            else
            {
                _miniCurrencyCalculatorUseWindowsEqualsColor = true;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorEqualsColorArgbUInt) is uint equalsColorArgb)
            {
                _miniCurrencyCalculatorEqualsColor = UnpackColor(equalsColorArgb);
            }
            else if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorEqualsColorArgbUInt) is int equalsColorArgbInt)
            {
                _miniCurrencyCalculatorEqualsColor = UnpackColor(unchecked((uint)equalsColorArgbInt));
            }
            else
            {
                _miniCurrencyCalculatorEqualsColor = ThemeSettingsService.AppAccentColor;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorEqualsButtonOpacityPercentInt) is int equalsOpacityPercent)
            {
                _miniCurrencyCalculatorEqualsButtonOpacityPercent = Math.Max(0, Math.Min(100, equalsOpacityPercent));
            }
            else
            {
                _miniCurrencyCalculatorEqualsButtonOpacityPercent = 100;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorDigitTextColorArgbUInt) is uint digitTextColorArgb)
            {
                _miniCurrencyCalculatorDigitTextColor = UnpackColor(digitTextColorArgb);
            }
            else if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorDigitTextColorArgbUInt) is int digitTextColorArgbInt)
            {
                _miniCurrencyCalculatorDigitTextColor = UnpackColor(unchecked((uint)digitTextColorArgbInt));
            }
            else
            {
                _miniCurrencyCalculatorDigitTextColor = Color.FromArgb(255, 58, 58, 58);
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorOperationTextColorArgbUInt) is uint operationTextColorArgb)
            {
                _miniCurrencyCalculatorOperationTextColor = UnpackColor(operationTextColorArgb);
            }
            else if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorOperationTextColorArgbUInt) is int operationTextColorArgbInt)
            {
                _miniCurrencyCalculatorOperationTextColor = UnpackColor(unchecked((uint)operationTextColorArgbInt));
            }
            else
            {
                _miniCurrencyCalculatorOperationTextColor = Color.FromArgb(255, 58, 58, 58);
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyCalculatorButtonsOpacityPercentInt) is int buttonsOpacityPercent)
            {
                _miniCurrencyCalculatorButtonsOpacityPercent = Math.Max(0, Math.Min(100, buttonsOpacityPercent));
            }
            else
            {
                _miniCurrencyCalculatorButtonsOpacityPercent = 100;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyShowCurrenciesBool) is bool showCurrencies)
            {
                _miniCurrencyShowCurrencies = showCurrencies;
            }
            else
            {
                _miniCurrencyShowCurrencies = true;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.MiniCurrencyShowCalculatorBool) is bool showCalculator)
            {
                _miniCurrencyShowCalculator = showCalculator;
            }
            else
            {
                _miniCurrencyShowCalculator = true;
            }
        }

        private static uint PackColor(Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }

        private static Color UnpackColor(uint argb)
        {
            return Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));
        }

        private static void InitializeSessionSnapshotSettings()
        {
            // We should disable session snapshot feature on multi instances
            if (!App.IsPrimaryInstance)
            {
                _isSessionSnapshotEnabled = false;
            }
            else if (App.IsGameBarWidget)
            {
                _isSessionSnapshotEnabled = true;
            }
            else
            {
                if (ApplicationSettingsStore.Read(SettingsKey.EditorEnableSessionBackupAndRestoreBool) is bool enableSessionBackupAndRestore)
                {
                    _isSessionSnapshotEnabled = enableSessionBackupAndRestore;
                }
                else
                {
                    _isSessionSnapshotEnabled = false;
                }
            }
        }

        private static void InitializeLineEndingSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultLineEndingStr) is string lineEndingStr &&
                Enum.TryParse(typeof(LineEnding), lineEndingStr, out var lineEnding))
            {
                _editorDefaultLineEnding = (LineEnding)lineEnding;
            }
            else
            {
                _editorDefaultLineEnding = LineEnding.Crlf;
            }
        }

        private static void InitializeTextWrappingSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultTextWrappingStr) is string textWrappingStr &&
                Enum.TryParse(typeof(TextWrapping), textWrappingStr, out var textWrapping))
            {
                _editorDefaultTextWrapping = (TextWrapping)textWrapping;
            }
            else
            {
                _editorDefaultTextWrapping = TextWrapping.NoWrap;
            }
        }

        private static void InitializeSpellingSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorHighlightMisspelledWordsBool) is bool highlightMisspelledWords)
            {
                _isHighlightMisspelledWordsEnabled = highlightMisspelledWords;
            }
            else
            {
                _isHighlightMisspelledWordsEnabled = false;
            }
        }

        private static void InitializeDisplaySettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultLineHighlighterViewStateBool) is bool displayLineHighlighter)
            {
                _editorDisplayLineHighlighter = displayLineHighlighter;
            }
            else
            {
                _editorDisplayLineHighlighter = true;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultDisplayLineNumbersBool) is bool displayLineNumbers)
            {
                _displayLineNumbers = displayLineNumbers;
            }
            else
            {
                _displayLineNumbers = true;
            }
        }

        private static void InitializeSmartCopySettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorEnableSmartCopyBool) is bool enableSmartCopy)
            {
                _isSmartCopyEnabled = enableSmartCopy;
            }
            else
            {
                _isSmartCopyEnabled = false;
            }
        }

        private static void InitializeEncodingSettings()
        {
            Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultEncodingCodePageInt) is int encodingCodePage)
            {
                var encoding = Encoding.GetEncoding(encodingCodePage);

                if (encoding is UTF8Encoding)
                {
                    if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultUtf8EncoderShouldEmitByteOrderMarkBool) is bool shouldEmitBom)
                    {
                        encoding = new UTF8Encoding(shouldEmitBom);
                    }
                    else
                    {
                        encoding = new UTF8Encoding(false);
                    }
                }

                _editorDefaultEncoding = encoding;
            }
            else
            {
                _editorDefaultEncoding = new UTF8Encoding(false);
            }
        }

        private static void InitializeDecodingSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultDecodingCodePageInt) is int decodingCodePage)
            {
                try
                {
                    if (decodingCodePage == -1)
                    {
                        _editorDefaultDecoding = null; // Meaning we should guess encoding during runtime
                    }
                    else
                    {
                        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        _editorDefaultDecoding = Encoding.GetEncoding(decodingCodePage);
                        if (_editorDefaultDecoding is UTF8Encoding)
                        {
                            _editorDefaultDecoding = new UTF8Encoding(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.LogError($"[{nameof(AppSettingsService)}] Failed to get encoding, code page: {decodingCodePage}, ex: {ex.Message}");
                    _editorDefaultDecoding = null;
                }
            }
            else
            {
                _editorDefaultDecoding = null; // Default to null
            }
        }

        private static void InitializeTabIndentsSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultTabIndentsInt) is int tabIndents)
            {
                _editorDefaultTabIndents = tabIndents;
            }
            else
            {
                _editorDefaultTabIndents = -1;
            }
        }

        private static void InitializeSearchEngineSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorDefaultSearchEngineStr) is string searchEngineStr &&
                Enum.TryParse(typeof(SearchEngine), searchEngineStr, out var searchEngine))
            {
                _editorDefaultSearchEngine = (SearchEngine)searchEngine;
            }
            else
            {
                _editorDefaultSearchEngine = SearchEngine.Bing;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.EditorCustomMadeSearchUrlStr) is string customMadeSearchUrl)
            {
                _editorCustomMadeSearchUrl = customMadeSearchUrl;
            }
            else
            {
                _editorCustomMadeSearchUrl = string.Empty;
            }
        }

        private static void InitializeFontSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.EditorFontFamilyStr) is string fontFamily)
            {
                _editorFontFamily = fontFamily;
            }
            else
            {
                _editorFontFamily = "Consolas";
            }

            if (ApplicationSettingsStore.Read(SettingsKey.EditorFontSizeInt) is int fontSize)
            {
                _editorFontSize = fontSize;
            }
            else
            {
                _editorFontSize = 14;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.EditorFontStyleStr) is string fontStyleStr &&
                Enum.TryParse(typeof(FontStyle), fontStyleStr, out var fontStyle))
            {
                _editorFontStyle = (FontStyle)fontStyle;
            }
            else
            {
                _editorFontStyle = FontStyle.Normal;
            }

            if (ApplicationSettingsStore.Read(SettingsKey.EditorFontWeightUshort) is ushort fontWeight)
            {
                _editorFontWeight = new FontWeight()
                {
                    Weight = fontWeight
                };
            }
            else
            {
                _editorFontWeight = FontWeights.Normal;
            }
        }

        private static void InitializeAppOpeningPreferencesSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.AlwaysOpenNewWindowBool) is bool alwaysOpenNewWindow)
            {
                _alwaysOpenNewWindow = alwaysOpenNewWindow;
            }
            else
            {
                _alwaysOpenNewWindow = false;
            }
        }

        private static void InitializeAppClosingPreferencesSettings()
        {
            if (ApplicationSettingsStore.Read(SettingsKey.ExitWhenLastTabClosed) is bool exitWhenLastTabClosed)
            {
                _exitWhenLastTabClosed = exitWhenLastTabClosed;
            }
            else
            {
                _exitWhenLastTabClosed = false;
            }
        }
    }
}
