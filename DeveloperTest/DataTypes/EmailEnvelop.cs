using System;

namespace DeveloperTest.DataTypes
{
    internal class EmailEnvelop
    {
        public EmailEnvelop(string fromName, string fromAddress, string subject, DateTime? date)
        {
            FromName = fromName;
            FromAddress = fromAddress;
            Subject = subject;
            Date = date;
        }

        public DateTime? Date { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string Subject { get; set; }
    }
}