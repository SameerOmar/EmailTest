using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DeveloperTest.Annotations;
using DeveloperTest.DataTypes;

namespace DeveloperTest.Models
{
    internal class EmailMessageModel : INotifyPropertyChanged
    {
        private string _htmlBody;
        private string _textBody;

        public EmailMessageModel()
        {
            Attachments = new List<AttachmentModel>();
        }

        public List<AttachmentModel> Attachments { get; set; }

        public bool BodyDownloaded { get; set; }

        public DateTime? Date { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public bool HasAttachments => Attachments?.Count > 0;

        public string HtmlBody
        {
            get => _htmlBody;
            set
            {
                if (value == _htmlBody)
                {
                    return;
                }

                _htmlBody = value;
                OnPropertyChanged();
            }
        }

        public MessageUid MessageId { get; set; }
        public string Subject { get; set; }

        public string TextBody
        {
            get => _textBody;
            set
            {
                if (value == _textBody)
                {
                    return;
                }

                _textBody = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}