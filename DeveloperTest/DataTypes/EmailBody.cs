namespace DeveloperTest.DataTypes
{
    internal class EmailBody
    {
        public EmailBody(bool isHtml, string html, string text)
        {
            IsHtml = isHtml;
            Html = html;
            Text = text;
        }

        public string Html { get; set; }
        public bool IsHtml { get; set; }
        public string Text { get; set; }
    }
}