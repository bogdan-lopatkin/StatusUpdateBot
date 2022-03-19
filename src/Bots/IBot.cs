namespace StatusUpdateBot.Bots
{
    public interface IBot
    {
        public void StartReceivingMessages();

        public void StartNotifyingUsers();

        public void StartLogging();
    }
}