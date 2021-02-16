using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using DeveloperTest.DataTypes;
using DeveloperTest.Extensions;
using DeveloperTest.Utilities;
using Limilabs.Client.POP3;
using Limilabs.Mail;
using Limilabs.Mail.MIME;

namespace DeveloperTest.Services
{
    internal class Pop3ServerConnector : IEmailServerConnector
    {
        private EncryptionProtocol _encryptionProtocol;
        private SecureString _password;
        private Pop3 _pop3;
        private int _port;
        private string _server;
        private string _userName;

        public Pop3ServerConnector()
        {
            _pop3 = new Pop3();
        }

        public bool InUse { get; set; }
        public string LastErrorMessage { get; set; }

        public void Dispose()
        {
            if (_pop3 == null)
            {
                return;
            }

            if (Connected)
            {
                _pop3.Close();
            }

            _pop3.Dispose();
            _pop3 = null;
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
                _pop3.UseBestLogin(userName, password.ConvertToUnsecureString());
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
                    _pop3.SSLConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                    _pop3.ConnectSSL(server, port);
                }
                else
                {
                    _pop3.Connect(server, port);

                    if (encryptionProtocol == EncryptionProtocol.StartTls)
                    {
                        _pop3.StartTLS();
                    }
                }

                Connected = _pop3.Connected;
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
            if (_pop3 == null || !Connected)
            {
                return;
            }

            try
            {
                _pop3.Close();
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
                var builder = new MailBuilder();
                var headers = _pop3.GetHeadersByUID(messageUid.GetUidAsString());
                var email = builder.CreateFromEml(headers);

                return new EmailEnvelop(email.From[0].Name, email.From[0].Address, email.Subject,
                    email.Date);
            }
            catch (Exception e)
            {
                HandleException(e);
                return null;
            }
        }

        public EmailBody GetMailBody(MessageUid messageUid)
        {
            try
            {
                var eml = _pop3.GetMessageByUID(messageUid.GetUidAsString());
                var email = new MailBuilder().CreateFromEml(eml);

                return new EmailBody(email.IsHtml, email.Html, email.Text);
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
                return _pop3.GetAll().Select(a => new MessageUid(a)).ToList();
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
                var eml = _pop3.GetMessageByUID(messageUid.GetUidAsString());
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
            var pop3ServerConnector = new Pop3ServerConnector();

            if (!pop3ServerConnector.Connect(_server, _port, _encryptionProtocol))
            {
                return null;
            }

            if (!pop3ServerConnector.Login(_userName, _password))
            {
                return null;
            }

            pop3ServerConnector.InUse = false;

            return pop3ServerConnector;
        }

        private void HandleException(Exception e)
        {
            LastErrorMessage = e.Message;
            Logger.Instance.LogError(e, "Pop3ServerConnector");
        }
    }
}