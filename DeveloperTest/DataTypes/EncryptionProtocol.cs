using System.ComponentModel;

namespace DeveloperTest.DataTypes
{
    internal enum EncryptionProtocol
    {
        [Description("Unencrypted")] 
        Unencrypted,
        [Description("SSL/TLS")] 
        SslTls,
        [Description("STARTTLS")] 
        StartTls
    }
}