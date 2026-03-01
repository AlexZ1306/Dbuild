// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.Storage;

    public static class MiniCurrencySettingsSyncService
    {
        public const string VisibleCurrenciesKey = "MiniCurrency.VisibleCurrencies";
        public static readonly IReadOnlyList<string> DefaultVisibleCurrencies = new[] { "RUB", "KZT", "USD", "TRY", "NOK" };

        public static event EventHandler<IReadOnlyList<string>> VisibleCurrenciesChanged;

        public static IReadOnlyList<string> GetVisibleCurrencies(IEnumerable<string> allowedCodes = null)
        {
            var allowed = allowedCodes != null
                ? new HashSet<string>(allowedCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToUpperInvariant()))
                : null;

            var fallback = DefaultVisibleCurrencies.Where(x => allowed == null || allowed.Contains(x)).ToList();

            try
            {
                var values = ApplicationData.Current.LocalSettings.Values;
                if (values.TryGetValue(VisibleCurrenciesKey, out var rawObj) && rawObj is string raw && !string.IsNullOrWhiteSpace(raw))
                {
                    var parsed = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim().ToUpperInvariant())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Where(x => allowed == null || allowed.Contains(x))
                        .Distinct()
                        .ToList();

                    if (parsed.Count > 0)
                    {
                        return parsed;
                    }
                }
            }
            catch
            {
            }

            return fallback;
        }

        public static void SaveVisibleCurrencies(IEnumerable<string> visibleCodes)
        {
            var normalized = (visibleCodes ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (normalized.Count == 0)
            {
                normalized = DefaultVisibleCurrencies.ToList();
            }

            ApplicationData.Current.LocalSettings.Values[VisibleCurrenciesKey] = string.Join(",", normalized);
            VisibleCurrenciesChanged?.Invoke(null, normalized);
        }
    }
}
