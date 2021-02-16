using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using DeveloperTest.DataTypes;
using DeveloperTest.Extensions;
using DeveloperTest.Utilities;
using Limilabs.Client.IMAP;
using Limilabs.Mail;
using Limilabs.Mail.MIME;

namespace DeveloperTest.Services
{
    internal class ImapServerConnector : IEmailServerConnector
    {
        private EncryptionProtocol _encryptionProtocol;
        private Imap _imap;
        private SecureString _password;
        private int _port;
        private string _server;
        private string _userName;

        public ImapServerConnector()
        {
            _imap = new Imap();
        }

        public bool InUse { get; set; }
        public string LastErrorMessage { get; set; }

        public void Dispose()
        {
            if (_imap == null)
            {
                return;
            }

            if (Connected)
            {
                _imap.Close();
            }

            _imap.Dispose();
            _imap = null;
        }

        public bool Connected { get; set; }

        public bool Login(string userName, SecureString password)
        {
            _password = password;
            _userName = userName;

            if (!Connected)
            {
                return false;
            }

            try
            {
                _imap.UseBestLogin(userName, password.ConvertToUnsecureString());
            }
            catch (Exception e)
            {
                HandleException(e);
                return false;
            }

            return true;
        }

        public bool Connect(string server, int port, EncryptionProtocol encryptionProtocol)
        {
            _encryptionProtocol = encryptionProtocol;
            _port = port;
            _server = server;

            try
            {
                if (encryptionProtocol == EncryptionProtocol.SslTls)
                {
                    _imap.SSLConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                    _imap.ConnectSSL(server, port);
                }
                else
                {
                    _imap.Connect(server, port);

                    if (encryptionProtocol == EncryptionProtocol.StartTls)
                    {
                        _imap.StartTLS();
                    }
                }

                Connected = _imap.Connected;
            }
            catch (Exception e)
            {
                HandleException(e);
                return false;
            }

            return Connected;
        }

        public void Disconnect()
        {
            if (_imap == null || !Connected)
            {
                return;
            }

            try
            {
                _imap.Close();
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        public EmailEnvelop GetMailEnvelope(MessageUid messageUid)
        {
            try
            {
                _imap.SelectInbox();
                var messageInfo = _imap.GetMessageInfoByUID(messageUid.GetUidAsLong());
                var emailFrom = messageInfo.Envelope.From.First();

                return new EmailEnvelop(emailFrom.Name, emailFrom.Address, messageInfo.Envelope.Subject,
                    messageInfo.Envelope.Date);
            }
            catch (Exception e)
            {
                HandleException(e);
                return null;
            }
        }

        public EmailBody GetMailBodyStructure(MessageUid messageUid)
        {
            try
            {
                var text = string.Empty;
                var html = string.Empty;

                _imap.SelectInbox();
                var bodyStructure = _imap.GetBodyStructureByUID(messageUid.GetUidAsLong());

                if (bodyStructure.Text != null)
                {
                    text = _imap.GetTextByUID(bodyStructure.Text);
                }

                if (bodyStructure.Html != null)
                {
                    html = _imap.GetTextByUID(bodyStructure.Html);
                }

                return new EmailBody(!string.IsNullOrWhiteSpace(html), html, text);
            }
            catch (Exception e)
            {
                HandleException(e);
                return null;
            }
        }

        public List<MessageUid> GetMessagesUids()
        {
            try
            {
                _imap.SelectInbox();
                return _imap.Search(Flag.All).Select(a => new MessageUid(a)).ToList();
            }
            catch (Exception e)
            {
                HandleException(e);
                return null;
            }
        }

        public List<MimeData> GetMailAttachments(MessageUid messageUid)
        {
            try
            {
                _imap.SelectInbox();
                var eml = _imap.GetMessageByUID(messageUid.GetUidAsLong());
                var email = new MailBuilder()
                    .CreateFromEml(eml);

                return email.Attachments.ToList();
            }
            catch (Exception e)
            {
                HandleException(e);
                return null;
            }
        }

        public IEmailServerConnector Clone()
        {
            var imapServerConnector = new ImapServerConnector();

            if (!imapServerConnector.Connect(_server, _port, _encryptionProtocol))
            {
                return null;
            }

            if (!imapServerConnector.Login(_userName, _password))
            {
                return null;
            }

            imapServerConnector.InUse = false;

            return imapServerConnector;
        }

        private void HandleException(Exception e)
        {
            LastErrorMessage = e.Message;
            Logger.Instance.LogError(e, "ImapServerConnector");
        }
    }
}