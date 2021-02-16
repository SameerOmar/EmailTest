namespace DeveloperTest.DataTypes
{
    internal class MessageUid
    {
        private readonly string _uid;

        public MessageUid(string messageUid)
        {
            _uid = messageUid;
        }

        public MessageUid(long messageUid)
        {
            _uid = messageUid.ToString();
        }

        public string GetUidAsString()
        {
            return _uid;
        }

        public long GetUidAsLong()
        {
            long.TryParse(_uid, out var longUid);

            return longUid;
        }
    }
}