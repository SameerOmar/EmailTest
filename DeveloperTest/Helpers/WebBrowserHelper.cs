using System.Windows;
using System.Windows.Controls;

namespace DeveloperTest.Helpers
{
    internal class WebBrowserHelper
    {
        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.RegisterAttached("Body", typeof(string), typeof(WebBrowserHelper),
                new PropertyMetadata(OnBodyChanged));

        public static string GetBody(DependencyObject dependencyObject)
        {
            return (string) dependencyObject.GetValue(BodyProperty);
        }

        public static void SetBody(DependencyObject dependencyObject, string body)
        {
            dependencyObject.SetValue(BodyProperty, body);
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var htmlString = string.Empty;

            if (e.NewValue != null)
            {
                htmlString = e.NewValue.ToString();
            }

            var webBrowser = (WebBrowser) d;
            webBrowser.NavigateToString(string.IsNullOrWhiteSpace(htmlString) ? "<body></body>" : htmlString);
        }
    }
}