using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            while (_toastQueue.Count > 0)
            {
                string message = _toastQueue.Dequeue();
                toastText.Text = message;
                var transform = new TranslateTransform { X = 100, Y = 0 };
                toastPanel.RenderTransform = transform;
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
                for (int i = 0; i <= 10; i++)
                {
                    transform.X = i * 10;
                    toastPanel.RenderTransform = transform;
                    await Task.Delay(15);
                }
                toastPanel.IsVisible = false;
            }
            _isShowingToast = false;
        }
    }
}
