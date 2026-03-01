// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class NotepadsMainPage
    {
        private enum MiniCurrencyRightPaneMode
        {
            Settings = 0,
            CurrencyPicker = 1
        }

        private enum MiniCurrencyCurrencyPickerMode
        {
            ReplaceRowCurrency = 0,
            ToggleMainVisibility = 1
        }

        private readonly CultureInfo _miniCurrencyCulture = CultureInfo.GetCultureInfo("ru-RU");
        private const string MiniCurrencyRatesApiUrl = "https://open.er-api.com/v6/latest/USD";
        private const string MiniCurrencyVisibleCurrenciesKey = "MiniCurrency.VisibleCurrencies";
        private const string MiniCurrencyRowOrderKey = "MiniCurrency.RowOrder";
        private const string MiniCurrencyValuesKey = "MiniCurrency.Values";
        private const string MiniCurrencyRatesCacheKey = "MiniCurrency.RatesCacheJson";
        private const string MiniCurrencyRatesCacheTsKey = "MiniCurrency.RatesCacheTs";
        private const int MiniCurrencyCalculatorMaxDigitsPerNumber = 16;
        private const int MiniCurrencyCalculatorMaxExpressionLength = 64;
        private const double MiniCurrencyCalculatorMaxAbsoluteValue = 9_999_999_999_999_999d;

        private readonly Dictionary<string, double> _miniCurrencyRates = new Dictionary<string, double>()
        {
            { "USD", 1.0 },
            { "RUB", 76.97 },
            { "KZT", 497.75 },
            { "TRY", 43.87 },
            { "NOK", 9.57 },
            { "AED", 3.6725 },
            { "EUR", 0.92 },
            { "GBP", 0.79 },
            { "CNY", 7.19 }
        };

        private readonly Dictionary<string, TextBox> _miniCurrencyInputs = new Dictionary<string, TextBox>();
        private readonly Dictionary<string, FrameworkElement> _miniCurrencyRows = new Dictionary<string, FrameworkElement>();
        private readonly List<string> _miniCurrencyDefaultVisible = new List<string> { "RUB", "KZT", "USD", "TRY", "NOK" };

        private bool _miniCurrencyInitialized;
        private bool _miniCurrencyIsUpdating;
        private bool _miniCurrencyRatesRefreshInProgress;
        private bool _miniCurrencyReplaceOnNextInput = true;
        private bool _miniCurrencyDeferredExpressionActive;
        private string _miniCurrencyDeferredExpressionPrefix = string.Empty;
        private string _miniCurrencyRatesStatusText = "Загрузка курсов...";
        private FrameworkElement _miniCurrencyPressedRow;
        private FrameworkElement _miniCurrencyDraggingRow;
        private Border _miniCurrencyDragPlaceholder;
        private DispatcherTimer _miniCurrencyUiScaleApplyTimer;
        private const double MiniCurrencyPickerPaneWidthReduction = 135;
        private double _miniCurrencySettingsPaneWidth = 470;
        private double _miniCurrencyCurrencyPickerPaneWidth = 335;
        private MiniCurrencyRightPaneMode _miniCurrencyRightPaneMode = MiniCurrencyRightPaneMode.Settings;
        private string _miniCurrencyCurrencyPickerSearchQuery = string.Empty;
        private string _miniCurrencyPreferredUiCurrencyCode;
        private bool _miniCurrencyCurrencyPickerLoaded;
        private string _miniCurrencyCurrencyPickerSourceCode;
        private MiniCurrencyCurrencyPickerMode _miniCurrencyCurrencyPickerMode = MiniCurrencyCurrencyPickerMode.ReplaceRowCurrency;
        private readonly List<MiniCurrencyCatalogListItem> _miniCurrencyCurrencyPickerCatalog = new List<MiniCurrencyCatalogListItem>();
        private readonly HashSet<string> _miniCurrencyCurrencyPickerCryptoCodes = new HashSet<string>
        {
            "ADA", "APT", "ATOM", "AVAX", "BCH", "BNB", "BTC", "CRO", "DOGE", "DOT",
            "ETC", "ETH", "FIL", "HBAR", "ICP", "LDO", "LEO", "LINK", "LTC", "MATIC",
            "NEAR", "OKB", "SHIB", "SOL", "TON", "TRX", "UNI", "VET", "XLM", "XMR", "XRP"
        };
        private uint _miniCurrencyDragPointerId;
        private bool _miniCurrencyRowDragStarted;
        private bool _miniCurrencySuppressNextRowTap;
        private double _miniCurrencyDragPointerOffsetY;
        private double _miniCurrencyDragLastPointerYInViewport;
        private bool _miniCurrencyDragHasLastPointerYInViewport;
        private DateTimeOffset _miniCurrencyIgnoreRowTapUntilUtc = DateTimeOffset.MinValue;
        private string _miniCurrencyActiveCode = "KZT";
        private string _miniCurrencyLatestStatusText = "Загрузка курсов...";
        private MenuFlyoutItem _miniCurrencyRefreshRatesMenuItem;
        private readonly Dictionary<string, string> _miniCurrencyDisplayNames = new Dictionary<string, string>()
        {
            { "ADA", "Cardano" },
            { "AED", "Дирхам ОАЭ" },
            { "AFN", "Афгани" },
            { "ALL", "Албанский лек" },
            { "AMD", "Армянский драм" },
            { "ANG", "Антильский гульден" },
            { "AOA", "Ангольская кванза" },
            { "APT", "Aptos" },
            { "ARS", "Аргентинское песо" },
            { "ATOM", "Cosmos" },
            { "AUD", "Австралийский доллар" },
            { "AVAX", "Avalanche" },
            { "AWG", "Арубанский флорин" },
            { "AZN", "Азербайджанский манат" },
            { "BAM", "Конвертируемая марка" },
            { "BBD", "Барбадосский доллар" },
            { "BCH", "Bitcoin Cash" },
            { "BDT", "Бангладешская така" },
            { "BGN", "Болгарский лев" },
            { "BHD", "Бахрейнский динар" },
            { "BIF", "Бурундийский франк" },
            { "BNB", "Binance Coin" },
            { "BND", "Брунейский доллар" },
            { "BOB", "Боливийский боливиано" },
            { "BRL", "Бразильский реал" },
            { "BTC", "Bitcoin" },
            { "BTN", "Бутанский нгултрум" },
            { "BWP", "Ботсванская пула" },
            { "BYN", "Белорусский рубль" },
            { "BZD", "Белизский доллар" },
            { "CAD", "Канадский доллар" },
            { "CDF", "Конголезский франк" },
            { "CHF", "Швейцарский франк" },
            { "CLP", "Чилийское песо" },
            { "CNY", "Китайский юань" },
            { "COP", "Колумбийское песо" },
            { "CRC", "Коста-риканский колон" },
            { "CRO", "Cronos" },
            { "CUP", "Кубинское песо" },
            { "CVE", "Эскудо Кабо-Верде" },
            { "CZK", "Чешская крона" },
            { "DJF", "Франк Джибути" },
            { "DKK", "Датская крона" },
            { "DOGE", "Dogecoin" },
            { "DOP", "Доминиканское песо" },
            { "DOT", "Polkadot" },
            { "DZD", "Алжирский динар" },
            { "EGP", "Египетский фунт" },
            { "ERN", "Накфа" },
            { "ETB", "Эфиопский быр" },
            { "ETC", "Ethereum Classic" },
            { "ETH", "Ethereum" },
            { "EUR", "Евро" },
            { "FIL", "Filecoin" },
            { "FJD", "Доллар Фиджи" },
            { "GBP", "Британский фунт" },
            { "GEL", "Грузинский лари" },
            { "GHS", "Ганский седи" },
            { "GMD", "Гамбийский даласи" },
            { "GNF", "Гвинейский франк" },
            { "GTQ", "Гватемальский кетсаль" },
            { "GYD", "Гайанский доллар" },
            { "HBAR", "Hedera" },
            { "HKD", "Гонконгский доллар" },
            { "HNL", "Гондурасская лемпира" },
            { "HTG", "Гаитянский гурд" },
            { "HUF", "Венгерский форинт" },
            { "ICP", "Internet Computer" },
            { "IDR", "Индонезийская рупия" },
            { "ILS", "Израильский шекель" },
            { "INR", "Индийская рупия" },
            { "IQD", "Иракский динар" },
            { "IRR", "Иранский риал" },
            { "ISK", "Исландская крона" },
            { "JMD", "Ямайский доллар" },
            { "JOD", "Иорданский динар" },
            { "JPY", "Японская иена" },
            { "KES", "Кенийский шиллинг" },
            { "KGS", "Киргизский сом" },
            { "KHR", "Камбоджийский риель" },
            { "KMF", "Франк Коморских островов" },
            { "KPW", "Северокорейская вона" },
            { "KRW", "Южнокорейская вона" },
            { "KWD", "Кувейтский динар" },
            { "KYD", "Доллар Каймановых островов" },
            { "KZT", "Казахстанский тенге" },
            { "LAK", "Лаосский кип" },
            { "LBP", "Ливанский фунт" },
            { "LDO", "Lido Dao" },
            { "LEO", "Unus Sed" },
            { "LINK", "Chainlink" },
            { "LKR", "Шри-ланкийская рупия" },
            { "LRD", "Либерийский доллар" },
            { "LTC", "Litecoin" },
            { "LYD", "Ливийский динар" },
            { "MAD", "Марокканский дирхам" },
            { "MATIC", "Polygon" },
            { "MDL", "Молдавский лей" },
            { "MGA", "Малагасийский ариари" },
            { "MKD", "Македонский денар" },
            { "MMK", "Мьянманский кьят" },
            { "MNT", "Монгольский тугрик" },
            { "MOP", "Патака Макао" },
            { "MRU", "Мавританская угия" },
            { "MUR", "Маврикийская рупия" },
            { "MVR", "Мальдивская руфия" },
            { "MWK", "Малавийская квача" },
            { "MXN", "Мексиканское песо" },
            { "MYR", "Малайзийский ринггит" },
            { "MZN", "Мозамбикский метикал" },
            { "NAD", "Доллар Намибии" },
            { "NEAR", "Near Protocol" },
            { "NGN", "Нигерийская найра" },
            { "NIO", "Никарагуанская кордоба" },
            { "NOK", "Норвежская крона" },
            { "NPR", "Непальская рупия" },
            { "NZD", "Новозеландский доллар" },
            { "OKB", "OKB" },
            { "OMR", "Оманский риал" },
            { "PEN", "Перуанский соль" },
            { "PGK", "Кина Папуа – Новой Гвинеи" },
            { "PHP", "Филиппинское песо" },
            { "PKR", "Пакистанская рупия" },
            { "PLN", "Польский злотый" },
            { "PRB", "Приднестровский рубль" },
            { "PYG", "Парагвайский гуарани" },
            { "QAR", "Катарский риал" },
            { "RON", "Румынский лей" },
            { "RSD", "Сербский динар" },
            { "RUB", "Российский рубль" },
            { "RWF", "Франк Руанды" },
            { "SAR", "Саудовский риял" },
            { "SBD", "Доллар Соломоновых островов" },
            { "SCR", "Сейшельская рупия" },
            { "SDG", "Суданский фунт" },
            { "SEK", "Шведская крона" },
            { "SGD", "Сингапурский доллар" },
            { "SHIB", "Shiba Inu" },
            { "SLE", "Леоне" },
            { "SOL", "Solana" },
            { "SOS", "Сомалийский шиллинг" },
            { "SRD", "Суринамский доллар" },
            { "SSP", "Южносуданский фунт" },
            { "STN", "Добра" },
            { "SYP", "Сирийский фунт" },
            { "SZL", "Свазилендский лилангени" },
            { "THB", "Таиландский бат" },
            { "TJS", "Таджикский сомони" },
            { "TMT", "Туркменский манат" },
            { "TND", "Тунисский динар" },
            { "TON", "Toncoin" },
            { "TOP", "Тонганская паанга" },
            { "TRX", "Tron" },
            { "TRY", "Турецкая лира" },
            { "TTD", "Доллар Тринидада и Тобаго" },
            { "TWD", "Тайваньский доллар" },
            { "TZS", "Танзанийский шиллинг" },
            { "UAH", "Украинская гривна" },
            { "UGX", "Угандийский шиллинг" },
            { "UNI", "Uniswap" },
            { "USD", "Доллар США" },
            { "UYU", "Уругвайское песо" },
            { "UZS", "Узбекский сум" },
            { "VES", "Венесуэльский боливар" },
            { "VET", "VeChain" },
            { "VND", "Вьетнамский донг" },
            { "VUV", "Вату Вануату" },
            { "WST", "Самоанская тала" },
            { "XAF", "Франк КФА BEAC" },
            { "XAU", "Золото" },
            { "XCD", "Восточно-карибский доллар" },
            { "XLM", "Stellar" },
            { "XMR", "Monero" },
            { "XOF", "Франк КФА BCEAO" },
            { "XPF", "Тихоокеанский франк" },
            { "XRP", "Ripple" },
            { "YER", "Йеменский риал" },
            { "ZAR", "Южноафриканский рэнд" },
            { "ZMW", "Замбийская квача" },
        };

        private bool IsMiniCurrencyMode => true;
    }
}







