using System.ComponentModel;

namespace DeveloperTest.DataTypes
{
    internal enum ServerType
    {
        [Description("IMAP")] 
        Imap,
        [Description("POP3")] 
        Pop3
    }
}