using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StatusUpdateBot.Bots.Telegram;

namespace StatusUpdateBot.Utils
{
    public static class ActionHandler
    {
        public static void Do(
            Action action,
            int retryAfter = 3,
            int maxAttemptCount = 3)
        {
            Task.Factory.StartNew(() => Do(action, TimeSpan.FromSeconds(retryAfter), maxAttemptCount));
        }

        private static void Do(
            Action action,
            TimeSpan retryInterval,
            int maxAttemptCount)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    action();

                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            foreach (var exception in exceptions.ToArray())
            {
                TelegramErrorLogger.LogError(exception, $"Operation failed after @{maxAttemptCount} attepmts");
            }
        }
    }
}