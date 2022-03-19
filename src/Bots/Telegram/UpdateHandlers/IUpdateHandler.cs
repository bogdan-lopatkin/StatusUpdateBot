using Telegram.Bot.Types;

namespace StatusUpdateBot.Bots.Telegram.UpdateHandlers
{
    public interface IUpdateHandler
    {
        public bool IsApplicable(Update update);

        public void HandleUpdate(Update update);
    }
}