using Persistence;
using Persistence.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Services
{
    public class ContextService
    {
        private static DFUnitOfWork uow = new DFUnitOfWork();
        private static DFRepository<Context> contexts = uow.Contexts;
        public static List<Context> GetContexts()
        {
            return contexts.Get().ToList();
        }

        public static void UpdateContext(string serverId, ulong channelId)
        {
            var context = contexts.Get(c => c.ServerId == serverId).FirstOrDefault();
            if (context == null) return;
            context.DefaultChannelId = "" + channelId;
            contexts.Update(context);
            uow.Save();
        }

        public static void CreateChannel(string serverId, ulong channelId)
        {
            var context = contexts.Get(c => c.ServerId == serverId).FirstOrDefault();
            if (context != null)
                UpdateContext(serverId, channelId);
            else
            {
                context = new Context()
                {
                    ServerId = serverId,
                    DefaultChannelId = "" + channelId
                };
                contexts.Insert(context);
                uow.Save();
            }
        }
    }
}
