// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2019-2024, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Notepads.Views.MainPage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;

    public sealed partial class NotepadsMainPage
    {
        private void CurrencyInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !(textBox.Tag is string code))
            {
                return;
            }

            if (textBox.IsReadOnly)
            {
                return;
            }

            _miniCurrencyActiveCode = code;
            HighlightMiniCurrencyActiveRow(code);
            _miniCurrencyReplaceOnNextInput = true;
        }

        private void CurrencyRow_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DateTimeOffset.UtcNow < _miniCurrencyIgnoreRowTapUntilUtc)
            {
                e.Handled = true;
                return;
            }

            if (!(sender is FrameworkElement element) || !(element.Tag is string code))
            {
                return;
            }

            ActivateMiniCurrencyInput(code);
            e.Handled = true;
        }

        private void CurrencyRow_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is FrameworkElement row) || CurrencyRowsHost == null || CurrencyRowsViewport == null || GetMiniCurrencyDragLayer() == null)
            {
                return;
            }

            // Recover from any interrupted drag state before starting a new one.
            if (_miniCurrencyPressedRow != null || _miniCurrencyDraggingRow != null || _miniCurrencyDragPlaceholder != null)
            {
                ResetMiniCurrencyRowDragState(restoreRow: true, saveOrder: false);
            }

            if (row.Visibility != Visibility.Visible || e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                return;
            }

            _miniCurrencyPressedRow = row;
            _miniCurrencyDragPointerId = e.Pointer.PointerId;
            _miniCurrencyRowDragStarted = false;
            _miniCurrencyDragPointerOffsetY = e.GetCurrentPoint(row).Position.Y;
            _miniCurrencyIgnoreRowTapUntilUtc = DateTimeOffset.MinValue;
            row.CapturePointer(e.Pointer);
            CurrencyRowsViewport.CapturePointer(e.Pointer);
            MiniCurrencyOverlay?.CapturePointer(e.Pointer);
        }

        private void CurrencyRow_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_miniCurrencyRowDragStarted)
            {
                // While dragging, handle movement only at viewport level to avoid duplicate move processing.
                return;
            }

            HandleMiniCurrencyRowPointerMoved(e);
        }

        private void CurrencyRow_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndMiniCurrencyRowPointerInteraction(sender as FrameworkElement, e.Pointer.PointerId, canceled: false);
        }

        private void CurrencyRow_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            EndMiniCurrencyRowPointerInteraction(sender as FrameworkElement, e.Pointer.PointerId, canceled: true);
        }

        private void CurrencyRowsViewport_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            HandleMiniCurrencyRowPointerMoved(e);
        }

        private void CurrencyRowsViewport_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndMiniCurrencyRowPointerInteraction(_miniCurrencyPressedRow, e.Pointer.PointerId, canceled: false);
        }

        private void CurrencyRowsViewport_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            EndMiniCurrencyRowPointerInteraction(_miniCurrencyPressedRow, e.Pointer.PointerId, canceled: true);
        }

        private void HandleMiniCurrencyRowPointerMoved(PointerRoutedEventArgs e)
        {
            var row = _miniCurrencyPressedRow;
            if (row == null || e.Pointer.PointerId != _miniCurrencyDragPointerId)
            {
                return;
            }

            // If mouse was released outside the app window, PointerReleased may be lost.
            // As soon as we receive the next move, end drag based on actual button state.
            var pointInPressedRow = e.GetCurrentPoint(row);
            if (pointInPressedRow?.Properties != null && !pointInPressedRow.Properties.IsLeftButtonPressed)
            {
                EndMiniCurrencyRowPointerInteraction(row, e.Pointer.PointerId, canceled: false);
                e.Handled = true;
                return;
            }

            var dragRoot = GetMiniCurrencyDragRoot();
            if (CurrencyRowsHost == null || CurrencyRowsViewport == null || dragRoot == null || GetMiniCurrencyDragLayer() == null)
            {
                return;
            }

            var pointerInViewport = e.GetCurrentPoint(CurrencyRowsViewport).Position;
            var pointerInDragRoot = e.GetCurrentPoint(dragRoot).Position;
            var rowTopInDragRoot = row.TransformToVisual(dragRoot).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
            var deltaY = pointerInDragRoot.Y - (rowTopInDragRoot + _miniCurrencyDragPointerOffsetY);

            if (!_miniCurrencyRowDragStarted)
            {
                if (Math.Abs(deltaY) < 6)
                {
                    return;
                }

                BeginMiniCurrencyRowDrag(row, pointerInViewport.Y, pointerInDragRoot.Y);
                _miniCurrencySuppressNextRowTap = true;
            }

            UpdateMiniCurrencyRowDrag(pointerInViewport.Y, pointerInDragRoot.Y);
            e.Handled = true;
        }

        private void BeginMiniCurrencyRowDrag(FrameworkElement row, double pointerYInViewport, double pointerYInDragRoot)
        {
            _miniCurrencyRowDragStarted = true;
            _miniCurrencyDraggingRow = row;
            var dragRoot = GetMiniCurrencyDragRoot();
            var dragLayer = GetMiniCurrencyDragLayer();
            if (dragRoot == null || dragLayer == null)
            {
                return;
            }

            var rowWidth = Math.Max(1, row.ActualWidth);
            var rowHeight = Math.Max(1, row.ActualHeight);
            var rowMargin = row.Margin;
            var rowOrigin = row.TransformToVisual(dragRoot).TransformPoint(new Windows.Foundation.Point(0, 0));
            var rowTop = rowOrigin.Y;
            var rowLeft = rowOrigin.X;

            _miniCurrencyDragPlaceholder = new Border
            {
                Height = rowHeight,
                Margin = rowMargin,
                Background = new SolidColorBrush(Windows.UI.Colors.Transparent),
                CornerRadius = new CornerRadius(14),
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Windows.UI.Colors.Transparent)
            };

            var sourceIndex = CurrencyRowsHost.Children.IndexOf(row);
            if (sourceIndex < 0)
            {
                _miniCurrencyRowDragStarted = false;
                _miniCurrencyDraggingRow = null;
                _miniCurrencyDragPlaceholder = null;
                return;
            }

            CurrencyRowsHost.Children.RemoveAt(sourceIndex);
            CurrencyRowsHost.Children.Insert(sourceIndex, _miniCurrencyDragPlaceholder);
            _miniCurrencyDragLastPointerYInViewport = pointerYInViewport;
            _miniCurrencyDragHasLastPointerYInViewport = true;

            row.Margin = new Thickness(0);
            row.Opacity = 0.96;

            dragLayer.Children.Clear();
            dragLayer.Children.Add(row);
            Canvas.SetLeft(row, rowLeft);
            Canvas.SetTop(row, rowTop);
            row.Width = rowWidth;

            UpdateMiniCurrencyRowDrag(pointerYInViewport, pointerYInDragRoot);
        }

        private void UpdateMiniCurrencyRowDrag(double pointerYInViewport, double pointerYInDragRoot)
        {
            if (!_miniCurrencyRowDragStarted || _miniCurrencyDraggingRow == null || _miniCurrencyDragPlaceholder == null)
            {
                return;
            }

            var dragRoot = GetMiniCurrencyDragRoot();
            if (dragRoot == null)
            {
                return;
            }

            var row = _miniCurrencyDraggingRow;
            var rowHeight = Math.Max(1, row.ActualHeight);
            var dragRootHeight = Math.Max(rowHeight, dragRoot.ActualHeight);

            var top = pointerYInDragRoot - _miniCurrencyDragPointerOffsetY;
            var minTop = -_miniCurrencyDragPointerOffsetY;
            var maxTop = Math.Max(0, dragRootHeight - rowHeight) + Math.Max(0, rowHeight - _miniCurrencyDragPointerOffsetY);
            top = Math.Max(minTop, Math.Min(maxTop, top));
            Canvas.SetTop(row, top);

            var pointerY = pointerYInViewport;
            var pointerDeltaY = _miniCurrencyDragHasLastPointerYInViewport
                ? pointerY - _miniCurrencyDragLastPointerYInViewport
                : 0;
            _miniCurrencyDragLastPointerYInViewport = pointerY;
            _miniCurrencyDragHasLastPointerYInViewport = true;

            var movingUp = pointerDeltaY < -0.5;
            var movingDown = pointerDeltaY > 0.5;
            if (!movingUp && !movingDown)
            {
                return;
            }

            var currentPlaceholderIndex = CurrencyRowsHost.Children.IndexOf(_miniCurrencyDragPlaceholder);
            if (currentPlaceholderIndex < 0)
            {
                return;
            }

            // Stable slot geometry based on logical child order, independent of visual reorder animation.
            var visibleChildIndexes = new List<int>();
            var visibleSlotCenters = new List<double>();
            var slotTop = 0.0;

            for (var i = 0; i < CurrencyRowsHost.Children.Count; i++)
            {
                if (!(CurrencyRowsHost.Children[i] is FrameworkElement child) || child.Visibility != Visibility.Visible)
                {
                    continue;
                }

                var margin = child.Margin;
                var height = child.ActualHeight;
                if (height <= 0 && ReferenceEquals(child, _miniCurrencyDragPlaceholder))
                {
                    height = _miniCurrencyDragPlaceholder.Height;
                }

                height = Math.Max(1, height);
                visibleChildIndexes.Add(i);
                visibleSlotCenters.Add(slotTop + margin.Top + (height * 0.5));
                slotTop += margin.Top + height + margin.Bottom;
            }

            var placeholderVisiblePos = visibleChildIndexes.IndexOf(currentPlaceholderIndex);
            if (placeholderVisiblePos < 0)
            {
                return;
            }

            if (movingUp && placeholderVisiblePos > 0)
            {
                var previousVisiblePos = placeholderVisiblePos - 1;
                var previousCenter = visibleSlotCenters[previousVisiblePos];
                if (pointerY < previousCenter)
                {
                    MoveMiniCurrencyDragPlaceholder(currentPlaceholderIndex, visibleChildIndexes[previousVisiblePos]);
                    return;
                }
            }

            if (movingDown && placeholderVisiblePos < visibleChildIndexes.Count - 1)
            {
                var nextVisiblePos = placeholderVisiblePos + 1;
                var nextCenter = visibleSlotCenters[nextVisiblePos];
                if (pointerY > nextCenter)
                {
                    // Insert at next visible child's current index so after removing placeholder
                    // it lands after that row (same behavior as original implementation).
                    MoveMiniCurrencyDragPlaceholder(currentPlaceholderIndex, visibleChildIndexes[nextVisiblePos]);
                }
            }
        }

        private void MoveMiniCurrencyDragPlaceholder(int currentIndex, int targetIndex)
        {
            if (CurrencyRowsHost == null || _miniCurrencyDragPlaceholder == null)
            {
                return;
            }

            CurrencyRowsHost.Children.RemoveAt(currentIndex);
            if (targetIndex > CurrencyRowsHost.Children.Count)
            {
                targetIndex = CurrencyRowsHost.Children.Count;
            }

            CurrencyRowsHost.Children.Insert(targetIndex, _miniCurrencyDragPlaceholder);
        }

        private int FindMiniCurrencyVisibleSiblingIndex(int startIndex, bool searchBackward)
        {
            if (CurrencyRowsHost == null)
            {
                return -1;
            }

            if (searchBackward)
            {
                for (var i = startIndex - 1; i >= 0; i--)
                {
                    if (CurrencyRowsHost.Children[i] is FrameworkElement row &&
                        row != _miniCurrencyDragPlaceholder &&
                        row.Visibility == Visibility.Visible)
                    {
                        return i;
                    }
                }

                return -1;
            }

            for (var i = startIndex + 1; i < CurrencyRowsHost.Children.Count; i++)
            {
                if (CurrencyRowsHost.Children[i] is FrameworkElement row &&
                    row != _miniCurrencyDragPlaceholder &&
                    row.Visibility == Visibility.Visible)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindMiniCurrencyLastVisibleIndex()
        {
            if (CurrencyRowsHost == null)
            {
                return -1;
            }

            for (var i = CurrencyRowsHost.Children.Count - 1; i >= 0; i--)
            {
                if (CurrencyRowsHost.Children[i] is FrameworkElement row &&
                    row != _miniCurrencyDragPlaceholder &&
                    row.Visibility == Visibility.Visible)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EndMiniCurrencyRowPointerInteraction(FrameworkElement row, uint pointerId, bool canceled)
        {
            if (row == null || row != _miniCurrencyPressedRow || pointerId != _miniCurrencyDragPointerId)
            {
                return;
            }

            row.ReleasePointerCaptures();
            CurrencyRowsViewport?.ReleasePointerCaptures();
            MiniCurrencyOverlay?.ReleasePointerCaptures();

            if (_miniCurrencyRowDragStarted && _miniCurrencyDraggingRow != null && _miniCurrencyDragPlaceholder != null)
            {
                var dragRoot = GetMiniCurrencyDragRoot();
                var dragLayer = GetMiniCurrencyDragLayer();
                var placeholderIndex = CurrencyRowsHost.Children.IndexOf(_miniCurrencyDragPlaceholder);
                if (placeholderIndex < 0)
                {
                    placeholderIndex = CurrencyRowsHost.Children.Count;
                }

                var overlayTop = Canvas.GetTop(_miniCurrencyDraggingRow);
                var placeholderTop = dragRoot != null
                    ? _miniCurrencyDragPlaceholder.TransformToVisual(dragRoot).TransformPoint(new Windows.Foundation.Point(0, 0)).Y
                    : overlayTop;

                var savedTransitions = CurrencyRowsHost?.ChildrenTransitions;
                if (CurrencyRowsHost != null)
                {
                    CurrencyRowsHost.ChildrenTransitions = null;
                }

                dragLayer?.Children.Clear();
                _miniCurrencyDraggingRow.Opacity = 1.0;
                _miniCurrencyDraggingRow.Width = double.NaN;
                ApplyMiniCurrencyUiScaleToRow(_miniCurrencyDraggingRow);

                CurrencyRowsHost.Children.Remove(_miniCurrencyDragPlaceholder);
                if (placeholderIndex > CurrencyRowsHost.Children.Count)
                {
                    placeholderIndex = CurrencyRowsHost.Children.Count;
                }
                CurrencyRowsHost.Children.Insert(placeholderIndex, _miniCurrencyDraggingRow);

                if (dragRoot != null)
                {
                    CurrencyRowsHost.UpdateLayout();
                    MiniCurrencyOverlay?.UpdateLayout();
                    var translate = _miniCurrencyDraggingRow.RenderTransform as TranslateTransform;
                    if (translate == null)
                    {
                        translate = new TranslateTransform();
                        _miniCurrencyDraggingRow.RenderTransform = translate;
                    }

                    var finalTop = _miniCurrencyDraggingRow.TransformToVisual(dragRoot).TransformPoint(new Windows.Foundation.Point(0, 0)).Y;
                    // Settle from the actual drop position to the target slot (current arranged layout after insert).
                    translate.Y = overlayTop - finalTop;
                    var animation = new DoubleAnimation
                    {
                        To = 0,
                        Duration = new Duration(TimeSpan.FromMilliseconds(180)),
                        EnableDependentAnimation = true,
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    Storyboard.SetTarget(animation, translate);
                    Storyboard.SetTargetProperty(animation, "Y");
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    storyboard.Completed += (s, a) =>
                    {
                        translate.Y = 0;
                        if (CurrencyRowsHost != null && CurrencyRowsHost.ChildrenTransitions == null)
                        {
                            CurrencyRowsHost.ChildrenTransitions = savedTransitions;
                        }
                    };
                    storyboard.Begin();
                }
                else if (CurrencyRowsHost != null)
                {
                    CurrencyRowsHost.ChildrenTransitions = savedTransitions;
                }

                if (!canceled)
                {
                    SaveMiniCurrencyRowOrder();
                    RebuildMiniCurrencyAddList();
                    _miniCurrencyIgnoreRowTapUntilUtc = DateTimeOffset.UtcNow.AddMilliseconds(60);
                }
            }

            _miniCurrencyPressedRow = null;
            _miniCurrencyDraggingRow = null;
            _miniCurrencyDragPlaceholder = null;
            _miniCurrencyDragPointerId = 0;
            _miniCurrencyRowDragStarted = false;
            _miniCurrencySuppressNextRowTap = false;
            _miniCurrencyDragLastPointerYInViewport = 0;
            _miniCurrencyDragHasLastPointerYInViewport = false;
        }

        private void ResetMiniCurrencyRowDragState(bool restoreRow, bool saveOrder)
        {
            var dragLayer = GetMiniCurrencyDragLayer();
            if (CurrencyRowsHost == null || dragLayer == null)
            {
                return;
            }

            if (restoreRow && _miniCurrencyDraggingRow != null)
            {
                var placeholderIndex = _miniCurrencyDragPlaceholder != null ? CurrencyRowsHost.Children.IndexOf(_miniCurrencyDragPlaceholder) : CurrencyRowsHost.Children.Count;
                dragLayer.Children.Clear();
                _miniCurrencyDraggingRow.Opacity = 1.0;
                _miniCurrencyDraggingRow.Width = double.NaN;
                ApplyMiniCurrencyUiScaleToRow(_miniCurrencyDraggingRow);

                if (_miniCurrencyDragPlaceholder != null)
                {
                    CurrencyRowsHost.Children.Remove(_miniCurrencyDragPlaceholder);
                }

                if (CurrencyRowsHost.Children.IndexOf(_miniCurrencyDraggingRow) < 0)
                {
                    placeholderIndex = Math.Max(0, Math.Min(placeholderIndex, CurrencyRowsHost.Children.Count));
                    CurrencyRowsHost.Children.Insert(placeholderIndex, _miniCurrencyDraggingRow);
                }

                if (saveOrder)
                {
                    SaveMiniCurrencyRowOrder();
                    RebuildMiniCurrencyAddList();
                }
            }
            else if (_miniCurrencyDragPlaceholder != null)
            {
                CurrencyRowsHost.Children.Remove(_miniCurrencyDragPlaceholder);
            }

            _miniCurrencyPressedRow = null;
            _miniCurrencyDraggingRow = null;
            _miniCurrencyDragPlaceholder = null;
            _miniCurrencyDragPointerId = 0;
            _miniCurrencyRowDragStarted = false;
            _miniCurrencySuppressNextRowTap = false;
            _miniCurrencyDragLastPointerYInViewport = 0;
            _miniCurrencyDragHasLastPointerYInViewport = false;
            CurrencyRowsViewport?.ReleasePointerCaptures();
            MiniCurrencyOverlay?.ReleasePointerCaptures();
        }

        private FrameworkElement GetMiniCurrencyDragRoot()
        {
            if (MiniCurrencyOverlay != null)
            {
                return MiniCurrencyOverlay;
            }

            return CurrencyRowsViewport;
        }

        private Canvas GetMiniCurrencyDragLayer()
        {
            if (CurrencyRowsDragOverlay != null)
            {
                return CurrencyRowsDragOverlay;
            }

            return null;
        }

        private void CurrencyRow_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current?.CoreWindow != null)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0);
            }
        }

        private void CurrencyRow_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current?.CoreWindow != null)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            }
        }

        private void CurrencyInput_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !(textBox.Tag is string code))
            {
                return;
            }

            e.Handled = true;
            ActivateMiniCurrencyInput(code);
        }

        private void CurrencyInput_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !(textBox.Tag is string code))
            {
                return;
            }

            ActivateMiniCurrencyInput(code);
            e.Handled = true;
        }

        private void CurrencyInput_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current?.CoreWindow != null)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0);
            }
        }

        private void CurrencyInput_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current?.CoreWindow != null)
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
            }
        }

        private void InitializeMiniCurrencyInputMode()
        {
            if (Window.Current?.CoreWindow == null)
            {
                return;
            }

            Window.Current.CoreWindow.CharacterReceived += MiniCurrency_CoreWindow_CharacterReceived;
            Window.Current.CoreWindow.KeyDown += MiniCurrency_CoreWindow_KeyDown;
        }

        private void ActivateMiniCurrencyInput(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || !_miniCurrencyInputs.TryGetValue(code, out var textBox))
            {
                return;
            }

            _miniCurrencyActiveCode = code;
            HighlightMiniCurrencyActiveRow(code);
            _miniCurrencyReplaceOnNextInput = true;

            // PointerPressed is handled, so TextBox should not enter normal edit mode.
        }

        private void MiniCurrency_CoreWindow_CharacterReceived(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.CharacterReceivedEventArgs args)
        {
            if (!IsMiniCurrencyMode || !_miniCurrencyInputs.TryGetValue(_miniCurrencyActiveCode, out var textBox) || !IsMiniCurrencyVisible(_miniCurrencyActiveCode))
            {
                return;
            }

            var ch = (char)args.KeyCode;
            if (char.IsControl(ch))
            {
                return;
            }

            if (ch == '.')
            {
                ch = ',';
            }

            if (!char.IsDigit(ch) && ch != ',')
            {
                return;
            }

            var next = _miniCurrencyReplaceOnNextInput ? string.Empty : (textBox.Text ?? string.Empty);

            if (ch == ',')
            {
                if (next.Contains(","))
                {
                    return;
                }

                next = string.IsNullOrEmpty(next) ? "0," : next + ",";
            }
            else
            {
                next += ch;
            }

            _miniCurrencyReplaceOnNextInput = false;
            textBox.Text = next;
            ConvertFromMiniCurrency(_miniCurrencyActiveCode);
            SaveMiniCurrencyValues();
        }

        private void MiniCurrency_CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (!IsMiniCurrencyMode)
            {
                return;
            }

            switch (args.VirtualKey)
            {
                case VirtualKey.F11:
                    EnterExitFullScreenMode();
                    args.Handled = true;
                    return;
                case VirtualKey.F12:
                    EnterExitCompactOverlayMode();
                    args.Handled = true;
                    return;
                case VirtualKey.F1:
                    if (App.IsPrimaryInstance && !App.IsGameBarWidget && RootSplitView != null)
                    {
                        RootSplitView.IsPaneOpen = !RootSplitView.IsPaneOpen;
                        args.Handled = true;
                    }

                    return;
                case VirtualKey.F2:
                    _ = OpenMiniCurrencyCurrencyManagementPaneAsync();
                    args.Handled = true;
                    return;
                case VirtualKey.F5:
                    _ = LoadMiniCurrencyRatesAsync(silent: false);
                    args.Handled = true;
                    return;
                case VirtualKey.Escape:
                    if (RootSplitView?.IsPaneOpen == true)
                    {
                        RootSplitView.IsPaneOpen = false;
                        args.Handled = true;
                    }

                    return;
            }

            if (!_miniCurrencyInputs.TryGetValue(_miniCurrencyActiveCode, out var textBox) || !IsMiniCurrencyVisible(_miniCurrencyActiveCode))
            {
                return;
            }

            if (args.VirtualKey == VirtualKey.Back)
            {
                var current = textBox.Text ?? string.Empty;
                if (_miniCurrencyReplaceOnNextInput)
                {
                    textBox.Text = string.Empty;
                    ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                    SaveMiniCurrencyValues();
                    return;
                }

                if (current.Length > 0)
                {
                    textBox.Text = current.Substring(0, current.Length - 1);
                }
                else
                {
                    textBox.Text = string.Empty;
                }

                ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                SaveMiniCurrencyValues();
                args.Handled = true;
                return;
            }

            if (args.VirtualKey == VirtualKey.Delete)
            {
                textBox.Text = string.Empty;
                _miniCurrencyReplaceOnNextInput = true;
                ConvertFromMiniCurrency(_miniCurrencyActiveCode);
                SaveMiniCurrencyValues();
                args.Handled = true;
            }
        }

        private void CurrencyInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_miniCurrencyIsUpdating)
            {
                return;
            }

            if (!(sender is TextBox textBox) || !(textBox.Tag is string code))
            {
                return;
            }

            _miniCurrencyActiveCode = code;
            HighlightMiniCurrencyActiveRow(code);
            ConvertFromMiniCurrency(code);
        }

        private void ConvertFromMiniCurrency(string fromCode)
        {
            if (!_miniCurrencyInputs.ContainsKey(fromCode) || !_miniCurrencyRates.ContainsKey(fromCode))
            {
                return;
            }

            var fromTextBox = _miniCurrencyInputs[fromCode];
            var value = ParseMiniCurrencyNumber(fromTextBox.Text);

            _miniCurrencyIsUpdating = true;
            try
            {
                if (!value.HasValue)
                {
                    foreach (var pair in _miniCurrencyInputs)
                    {
                        if (pair.Key == fromCode || !IsMiniCurrencyVisible(pair.Key))
                        {
                            continue;
                        }
                        pair.Value.Text = string.Empty;
                    }
                    return;
                }

                var fromRate = _miniCurrencyRates[fromCode];
                if (double.IsNaN(fromRate) || double.IsInfinity(fromRate) || fromRate <= 0)
                {
                    foreach (var pair in _miniCurrencyInputs)
                    {
                        if (pair.Key == fromCode || !IsMiniCurrencyVisible(pair.Key))
                        {
                            continue;
                        }

                        pair.Value.Text = string.Empty;
                    }

                    return;
                }

                var inUsd = value.Value / fromRate;

                foreach (var pair in _miniCurrencyInputs)
                {
                    if (pair.Key == fromCode || !IsMiniCurrencyVisible(pair.Key))
                    {
                        continue;
                    }
                    if (_miniCurrencyRates.TryGetValue(pair.Key, out var targetRate) &&
                        !(double.IsNaN(targetRate) || double.IsInfinity(targetRate) || targetRate <= 0))
                    {
                        pair.Value.Text = FormatMiniCurrencyNumber(inUsd * targetRate);
                    }
                    else
                    {
                        pair.Value.Text = string.Empty;
                    }
                }
            }
            finally
            {
                _miniCurrencyIsUpdating = false;
            }

            SaveMiniCurrencyValues();
        }

        private double? ParseMiniCurrencyNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var normalized = text.Replace(" ", string.Empty).Replace("\u00A0", string.Empty).Replace(",", ".");
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        private string FormatMiniCurrencyNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return string.Empty;
            }

            var abs = Math.Abs(value);
            if (abs == 0)
            {
                return "0";
            }

            // Adaptive precision: keep more fraction digits for small values (e.g., BTC),
            // while avoiding noisy long tails for normal fiat values.
            int decimals;
            if (abs >= 1)
            {
                decimals = 2;
            }
            else if (abs >= 0.1)
            {
                decimals = 4;
            }
            else if (abs >= 0.01)
            {
                decimals = 5;
            }
            else if (abs >= 0.001)
            {
                decimals = 6;
            }
            else if (abs >= 0.0001)
            {
                decimals = 7;
            }
            else
            {
                decimals = 8;
            }

            var roundedAbs = Math.Round(abs, decimals, MidpointRounding.AwayFromZero);
            if (roundedAbs == 0)
            {
                var separator = _miniCurrencyCulture.NumberFormat.NumberDecimalSeparator;
                var minVisible = "0" + separator + new string('0', Math.Max(0, decimals - 1)) + "1";
                return value < 0 ? "-" + minVisible : minVisible;
            }

            var format = "N" + decimals;
            var text = value.ToString(format, _miniCurrencyCulture).Replace('\u00A0', ' ');
            var decimalSeparator = _miniCurrencyCulture.NumberFormat.NumberDecimalSeparator;

            if (text.Contains(decimalSeparator))
            {
                text = text.TrimEnd('0');
                if (text.EndsWith(decimalSeparator))
                {
                    text = text.Substring(0, text.Length - decimalSeparator.Length);
                }
            }

            return text;
        }

        private bool IsMiniCurrencyVisible(string code)
        {
            return _miniCurrencyRows.TryGetValue(code, out var row) && row.Visibility == Visibility.Visible;
        }


    }
}






