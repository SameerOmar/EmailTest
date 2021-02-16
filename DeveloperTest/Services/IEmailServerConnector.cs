using System;
using System.Collections.Generic;
using System.Security;
using DeveloperTest.DataTypes;
using Limilabs.Mail.MIME;

namespace DeveloperTest.Services
{
    internal interface IEmailServerConnector : IDisposable
    {
        bool Connected { get; set; }
        bool InUse { get; set; }
        string LastErrorMessage { get; set; }

        bool Login(string userName, SecureString password);
        bool Connect(string server, int port, EncryptionProtocol encryptionProtocol);
        void Disconnect();
        EmailEnvelop GetMailEnvelope(MessageUid messageUid);
        EmailBody GetMailBody(MessageUid messageUid);
        List<MessageUid> GetMessagesUids();
        List<MimeData> GetMailAttachments(MessageUid messageUid);
        IEmailServerConnector Clone();
    }
}