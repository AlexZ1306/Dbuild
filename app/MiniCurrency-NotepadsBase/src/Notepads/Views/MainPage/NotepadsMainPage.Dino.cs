// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System;
    using System.Threading.Tasks;
    using Notepads.Services;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class NotepadsMainPage
    {
        private static readonly Uri MiniCurrencyDinoPageUri = new Uri("ms-appx-web:///Assets/Dino/index.html");

        private bool IsMiniCurrencyDinoModeEnabled()
        {
            return AppSettingsService.MiniCurrencyShowDino;
        }

        private void InitializeMiniCurrencyDinoMode()
        {
            if (DinoGameWebView == null)
            {
                return;
            }

            DinoGameWebView.DefaultBackgroundColor = Colors.Transparent;
            DinoGameWebView.Visibility = Visibility.Visible;
        }

        private async Task EnsureMiniCurrencyDinoLoadedAsync()
        {
            if (DinoGameWebView == null)
            {
                return;
            }

            if (!_miniCurrencyDinoLoaded)
            {
                _miniCurrencyDinoLoaded = true;
                _miniCurrencyDinoNavigationCompleted = false;
                DinoGameWebView.Navigate(MiniCurrencyDinoPageUri);
                return;
            }

            if (_miniCurrencyDinoNavigationCompleted)
            {
                await PrepareMiniCurrencyDinoSurfaceAsync();
                await FocusMiniCurrencyDinoAsync();
            }
        }

        private async void DinoGameWebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                _miniCurrencyDinoNavigationCompleted = false;
                return;
            }

            _miniCurrencyDinoNavigationCompleted = true;
            await PrepareMiniCurrencyDinoSurfaceAsync();

            if (IsMiniCurrencyDinoModeEnabled())
            {
                await FocusMiniCurrencyDinoAsync();
            }
        }

        private async Task PrepareMiniCurrencyDinoSurfaceAsync()
        {
            if (DinoGameWebView == null || !_miniCurrencyDinoNavigationCompleted)
            {
                return;
            }

            const string script = "(function(){try{document.documentElement.style.background='transparent';document.documentElement.style.outline='none';document.documentElement.style.border='0';document.documentElement.style.boxShadow='none';if(document.body){document.body.style.background='transparent';document.body.style.outline='none';document.body.style.border='0';document.body.style.boxShadow='none';document.body.tabIndex=-1;}var selectors=['#main-frame-error','#main-content','.interstitial-wrapper','.runner-container','.runner-canvas','.controller','canvas'];selectors.forEach(function(selector){var nodes=document.querySelectorAll(selector);for(var i=0;i<nodes.length;i++){var node=nodes[i];node.style.background='transparent';node.style.outline='none';node.style.border='0';node.style.boxShadow='none';}});var icon=document.querySelector('.icon-offline');if(icon){icon.style.display='none';}if(window.Runner&&Runner.instance_){if(Runner.instance_.outerContainerEl){Runner.instance_.outerContainerEl.style.background='transparent';Runner.instance_.outerContainerEl.style.outline='none';Runner.instance_.outerContainerEl.style.border='0';Runner.instance_.outerContainerEl.style.boxShadow='none';}if(Runner.instance_.containerEl){Runner.instance_.containerEl.style.background='transparent';Runner.instance_.containerEl.style.outline='none';Runner.instance_.containerEl.style.border='0';Runner.instance_.containerEl.style.boxShadow='none';Runner.instance_.containerEl.tabIndex=-1;}}window.focus();if(document.body){document.body.focus();}}catch(e){}})();";

            try
            {
                await DinoGameWebView.InvokeScriptAsync("eval", new[] { script });
            }
            catch
            {
                // Ignore script injection failures; the page can still run without them.
            }
        }

        private async Task FocusMiniCurrencyDinoAsync()
        {
            if (DinoGameWebView == null || !IsMiniCurrencyDinoModeEnabled())
            {
                return;
            }

            DinoGameWebView.Focus(FocusState.Programmatic);

            if (!_miniCurrencyDinoNavigationCompleted)
            {
                return;
            }

            const string focusScript = "(function(){try{window.focus();if(document.body){document.body.tabIndex=-1;document.body.focus();}var focusTarget=document.querySelector('canvas')||document.querySelector('.runner-container');if(focusTarget){focusTarget.tabIndex=-1;focusTarget.focus&&focusTarget.focus();}}catch(e){}})();";

            try
            {
                await DinoGameWebView.InvokeScriptAsync("eval", new[] { focusScript });
            }
            catch
            {
                // Ignore focus script failures; WebView focus is usually enough.
            }
        }
    }
}
