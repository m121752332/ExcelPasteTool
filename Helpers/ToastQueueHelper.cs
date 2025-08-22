using Avalonia.Controls;
using Avalonia.Media;

namespace ExcelPasteTool.Helpers
{
    public class ToastQueueHelper
    {
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private bool _isShowingToast = false;
        private const int MaxQueue = 5;
        private readonly UserControl _owner;
        private readonly string _panelName;
        private readonly string _textName;

        public ToastQueueHelper(UserControl owner, string panelName = "ToastPanel", string textName = "ToastText")
        {
            _owner = owner;
            _panelName = panelName;
            _textName = textName;
        }

        public void EnqueueToast(string message)
        {
            if (_toastQueue.Count >= MaxQueue)
            {
                _toastQueue.Dequeue();
            }
            _toastQueue.Enqueue(message);
            if (!_isShowingToast)
            {
                _ = ProcessToastQueue();
            }
        }

        private async Task ProcessToastQueue()
        {
            _isShowingToast = true;
            var toastPanel = _owner.FindControl<Border>(_panelName);
            var toastText = _owner.FindControl<TextBlock>(_textName);
            if (toastPanel == null || toastText == null)
            {
                _isShowingToast = false;
                return;
            }
            var defaultMargin = new Avalonia.Thickness(0, 30, 30, 0); // 與 XAML 預設一致
            while (_toastQueue.Count > 0)
            {
                string message = _toastQueue.Dequeue();
                toastText.Text = message;
                var transform = new TranslateTransform { X = 100, Y = 0 };
                toastPanel.RenderTransform = transform;
                toastPanel.Margin = defaultMargin; // 顯示前重設位置
                toastPanel.IsVisible = true;
                for (int i = 0; i <= 10; i++)
                {
                    transform.X = 100 - i * 10;
                    toastPanel.RenderTransform = transform;
                    await Task.Delay(15);
                }
                transform.X = 0;
                toastPanel.RenderTransform = transform;
                await Task.Delay(2000);
                // 最後一則訊息時執行煙火動畫
                if (_toastQueue.Count == 0)
                {
                    await FireworkFadeOut(toastPanel, toastText, defaultMargin);
                }
                else
                {
                    for (int i = 0; i <= 10; i++)
                    {
                        transform.X = i * 10;
                        toastPanel.RenderTransform = transform;
                        await Task.Delay(15);
                    }
                    toastPanel.IsVisible = false;
                    toastPanel.Margin = defaultMargin; // 隱藏時也重設位置
                }
            }
            _isShowingToast = false;
        }

        // 煙火散開動畫：淡出、縮放、分散
        private async Task FireworkFadeOut(Border toastPanel, TextBlock toastText, Avalonia.Thickness defaultMargin)
        {
            var originalOpacity = toastPanel.Opacity;
            var originalScale = toastPanel.RenderTransform;
            var scaleTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
            var random = new Random();
            toastPanel.RenderTransform = scaleTransform;
            for (int i = 0; i < 15; i++)
            {
                // 淡出
                toastPanel.Opacity = 1 - i * 0.07;
                // 放大
                scaleTransform.ScaleX = 1 + i * 0.05;
                scaleTransform.ScaleY = 1 + i * 0.05;
                // 隨機分散（上下左右偏移）
                var offsetX = random.Next(-8, 8);
                var offsetY = random.Next(-8, 8);
                toastPanel.Margin = new Avalonia.Thickness(0, 20 + offsetY, 20 + offsetX, 0);
                await Task.Delay(18);
            }
            toastPanel.Opacity = originalOpacity;
            toastPanel.RenderTransform = originalScale;
            toastPanel.Margin = defaultMargin; // 動畫結束重設位置
            toastPanel.IsVisible = false;
        }
    }
}
