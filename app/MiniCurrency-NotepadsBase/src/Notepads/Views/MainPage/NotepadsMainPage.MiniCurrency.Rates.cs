// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Windows.Data.Json;

    public sealed partial class NotepadsMainPage
    {
        private const string MiniCurrencySupplementalRatesApiUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/usd.json";

        private static readonly Dictionary<string, double> MiniCurrencyManualFallbackRates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "PRB", 16.10 }
        };

        private static readonly Dictionary<string, string> MiniCurrencyCryptoCoinGeckoIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ADA", "cardano" },
            { "APT", "aptos" },
            { "ATOM", "cosmos" },
            { "AVAX", "avalanche-2" },
            { "BCH", "bitcoin-cash" },
            { "BNB", "binancecoin" },
            { "BTC", "bitcoin" },
            { "CRO", "cronos" },
            { "DOGE", "dogecoin" },
            { "DOT", "polkadot" },
            { "ETC", "ethereum-classic" },
            { "ETH", "ethereum" },
            { "FIL", "filecoin" },
            { "HBAR", "hedera-hashgraph" },
            { "ICP", "internet-computer" },
            { "LDO", "lido-dao" },
            { "LEO", "leo-token" },
            { "LINK", "chainlink" },
            { "LTC", "litecoin" },
            { "MATIC", "polygon-ecosystem-token" },
            { "NEAR", "near" },
            { "OKB", "okb" },
            { "SHIB", "shiba-inu" },
            { "SOL", "solana" },
            { "TON", "the-open-network" },
            { "TRX", "tron" },
            { "UNI", "uniswap" },
            { "VET", "vechain" },
            { "XLM", "stellar" },
            { "XMR", "monero" },
            { "XRP", "ripple" }
        };

        private async Task LoadMiniCurrencyRatesAsync(bool silent)
        {
            if (_miniCurrencyRatesRefreshInProgress)
            {
                if (!silent)
                {
                    SetMiniCurrencyRatesStatus("Обновление курсов уже выполняется...");
                }
                return;
            }

            _miniCurrencyRatesRefreshInProgress = true;
            try
            {
                if (!silent)
                {
                    SetMiniCurrencyRatesStatus("Обновление курсов...");
                }

                var fiatUpdated = false;
                var cryptoUpdated = 0;
                var supplementalUpdated = 0;
                var manualFallbackApplied = 0;
                string fiatJson = null;

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(12);
                    client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
                    {
                        NoCache = true,
                        NoStore = true
                    };
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MiniCurrency-Notepads/1.0");

                    try
                    {
                        fiatJson = await client.GetStringAsync(MiniCurrencyRatesApiUrl);
                        fiatUpdated = TryApplyMiniCurrencyRatesFromJson(fiatJson);
                        if (fiatUpdated)
                        {
                            CacheMiniCurrencyRatesJson(fiatJson);
                        }
                    }
                    catch
                    {
                        // fiat endpoint failed, try cache below and crypto endpoint separately
                    }

                    try
                    {
                        cryptoUpdated = await TryApplyMiniCurrencyCryptoRatesAsync(client);
                    }
                    catch
                    {
                        // crypto endpoint is best-effort only
                    }

                    try
                    {
                        supplementalUpdated = await TryApplyMiniCurrencySupplementalRatesAsync(client);
                    }
                    catch
                    {
                        // supplemental endpoint is best-effort only
                    }
                }

                manualFallbackApplied = ApplyMiniCurrencyManualFallbackRates();

                if (fiatUpdated || cryptoUpdated > 0 || supplementalUpdated > 0 || manualFallbackApplied > 0)
                {
                    _miniCurrencyLastSuccessfulRatesUpdateLocal = DateTimeOffset.Now;
                    SetMiniCurrencyRatesStatus(
                        FormatMiniCurrencyRatesUpdatedStatus(_miniCurrencyLastSuccessfulRatesUpdateLocal.Value),
                        supportsHoverRefreshPrefix: true);
                    ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                    return;
                }

                if (!TryRestoreMiniCurrencyRatesFromCache())
                {
                    manualFallbackApplied = ApplyMiniCurrencyManualFallbackRates();
                    if (manualFallbackApplied > 0)
                    {
                        SetMiniCurrencyRatesStatus("Курсы частично обновлены");
                        ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                    }
                    else
                    {
                        SetMiniCurrencyRatesStatus("Не удалось загрузить курсы");
                    }
                }
                else
                {
                    ApplyMiniCurrencyManualFallbackRates();
                    ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                }
            }
            catch
            {
                if (!TryRestoreMiniCurrencyRatesFromCache())
                {
                    var manualOnlyApplied = ApplyMiniCurrencyManualFallbackRates();
                    if (manualOnlyApplied > 0)
                    {
                        SetMiniCurrencyRatesStatus("Оффлайн: курсы частично доступны");
                        ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                    }
                    else
                    {
                        SetMiniCurrencyRatesStatus("Нет интернета и нет кеша курсов");
                    }
                }
                else
                {
                    ApplyMiniCurrencyManualFallbackRates();
                    ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                }
            }
            finally
            {
                _miniCurrencyRatesRefreshInProgress = false;
            }
        }

        private async Task<int> TryApplyMiniCurrencyCryptoRatesAsync(HttpClient client)
        {
            if (client == null)
            {
                return 0;
            }

            var pairs = _miniCurrencyRates.Keys
                .Where(code => MiniCurrencyCryptoCoinGeckoIds.ContainsKey(code))
                .Select(code => new KeyValuePair<string, string>(code, MiniCurrencyCryptoCoinGeckoIds[code]))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .ToList();

            if (pairs.Count == 0)
            {
                return 0;
            }

            var ids = string.Join(",", pairs.Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase));
            var url = "https://api.coingecko.com/api/v3/simple/price?ids=" + Uri.EscapeDataString(ids) + "&vs_currencies=usd&precision=full";
            var json = await client.GetStringAsync(url);
            return TryApplyMiniCurrencyCryptoRatesFromJson(json, pairs);
        }

        private async Task<int> TryApplyMiniCurrencySupplementalRatesAsync(HttpClient client)
        {
            if (client == null)
            {
                return 0;
            }

            var json = await client.GetStringAsync(MiniCurrencySupplementalRatesApiUrl);
            return TryApplyMiniCurrencySupplementalRatesFromJson(json);
        }

        private int TryApplyMiniCurrencySupplementalRatesFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return 0;
            }

            if (!JsonObject.TryParse(json, out JsonObject root))
            {
                return 0;
            }

            if (!root.TryGetValue("usd", out var usdValue) || usdValue.ValueType != JsonValueType.Object)
            {
                return 0;
            }

            var usdRates = usdValue.GetObject();
            var updated = 0;
            updated += TryApplyMiniCurrencyRateFromUsdObject(usdRates, "KPW", "kpw");
            updated += TryApplyMiniCurrencyRateFromUsdObject(usdRates, "XAU", "xau");
            updated += TryApplyMiniCurrencyRateFromUsdObject(usdRates, "MATIC", "pol");
            return updated;
        }

        private int TryApplyMiniCurrencyRateFromUsdObject(JsonObject usdRates, string currencyCode, string sourceRateCode)
        {
            if (usdRates == null || string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(sourceRateCode))
            {
                return 0;
            }

            if (!_miniCurrencyRates.TryGetValue(currencyCode, out var currentRate) || IsMiniCurrencyRateValid(currentRate))
            {
                return 0;
            }

            if (!usdRates.TryGetValue(sourceRateCode, out var value) || value.ValueType != JsonValueType.Number)
            {
                return 0;
            }

            var rate = value.GetNumber();
            if (!IsMiniCurrencyRateValid(rate))
            {
                return 0;
            }

            _miniCurrencyRates[currencyCode] = rate;
            return 1;
        }

        private int TryApplyMiniCurrencyCryptoRatesFromJson(string json, IReadOnlyList<KeyValuePair<string, string>> codeIdPairs)
        {
            if (string.IsNullOrWhiteSpace(json) || codeIdPairs == null || codeIdPairs.Count == 0)
            {
                return 0;
            }

            if (!JsonObject.TryParse(json, out JsonObject root))
            {
                return 0;
            }

            var updated = 0;
            foreach (var pair in codeIdPairs)
            {
                var code = pair.Key?.Trim().ToUpperInvariant();
                var id = pair.Value?.Trim();
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!root.TryGetValue(id, out var coinValue) || coinValue.ValueType != JsonValueType.Object)
                {
                    continue;
                }

                var coinObj = coinValue.GetObject();
                if (!coinObj.TryGetValue("usd", out var usdValue) || usdValue.ValueType != JsonValueType.Number)
                {
                    continue;
                }

                var usdPricePerCoin = usdValue.GetNumber();
                if (double.IsNaN(usdPricePerCoin) || double.IsInfinity(usdPricePerCoin) || usdPricePerCoin <= 0)
                {
                    continue;
                }

                _miniCurrencyRates[code] = 1.0 / usdPricePerCoin;
                updated++;
            }

            return updated;
        }

        private int ApplyMiniCurrencyManualFallbackRates()
        {
            var updated = 0;
            foreach (var pair in MiniCurrencyManualFallbackRates)
            {
                if (_miniCurrencyRates.TryGetValue(pair.Key, out var existingRate) && IsMiniCurrencyRateValid(existingRate))
                {
                    continue;
                }

                _miniCurrencyRates[pair.Key] = pair.Value;
                updated++;
            }

            return updated;
        }

        private static bool IsMiniCurrencyRateValid(double rate)
        {
            return !(double.IsNaN(rate) || double.IsInfinity(rate) || rate <= 0);
        }

        private bool TryApplyMiniCurrencyRatesFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            JsonObject root;
            if (!JsonObject.TryParse(json, out root))
            {
                return false;
            }

            string result;
            if (!root.TryGetValue("result", out var resultValue))
            {
                return false;
            }
            result = resultValue.GetString();
            if (!string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!root.TryGetValue("rates", out var ratesValue) || ratesValue.ValueType != JsonValueType.Object)
            {
                return false;
            }

            var ratesObj = ratesValue.GetObject();
            var updatedCount = 0;
            foreach (var code in _miniCurrencyRates.Keys.ToList())
            {
                if (ratesObj.TryGetValue(code, out var value) &&
                    (value.ValueType == JsonValueType.Number))
                {
                    _miniCurrencyRates[code] = value.GetNumber();
                    updatedCount++;
                }
            }

            return updatedCount > 0;
        }

        private void CacheMiniCurrencyRatesJson(string json)
        {
            try
            {
                MiniCurrencySettings.Values[MiniCurrencyRatesCacheKey] = json;
                MiniCurrencySettings.Values[MiniCurrencyRatesCacheTsKey] = DateTimeOffset.Now.ToString("O");
            }
            catch
            {
                // ignore cache failures
            }
        }

        private bool TryRestoreMiniCurrencyRatesFromCache()
        {
            try
            {
                if (MiniCurrencySettings.Values.TryGetValue(MiniCurrencyRatesCacheKey, out var cachedObj) &&
                    cachedObj is string cachedJson &&
                    TryApplyMiniCurrencyRatesFromJson(cachedJson))
                {
                    string tsText = null;
                    if (MiniCurrencySettings.Values.TryGetValue(MiniCurrencyRatesCacheTsKey, out var tsObj) && tsObj is string s)
                    {
                        tsText = s;
                    }

                    if (DateTimeOffset.TryParse(tsText, out var ts))
                    {
                        SetMiniCurrencyRatesStatus($"Оффлайн: курсы из кеша ({ts.LocalDateTime:dd.MM.yyyy HH:mm})");
                    }
                    else
                    {
                        SetMiniCurrencyRatesStatus("Оффлайн: курсы из кеша");
                    }

                    return true;
                }
            }
            catch
            {
                // ignore cache read errors
            }

            return false;
        }
    }
}



