using System;
using System.Collections.Generic;
using System.Linq;
using flow.Connection;
using System.Threading.Tasks;

namespace flow
{
    using MessageCallback = Func<(FlowConnection conn, FlowMessage message), Task>;
    public class MessageDispatcher
    {
        protected Dictionary<Type, List<MessageCallback>> delegates {get; set; }

        public MessageDispatcher()
        {
            delegates = new Dictionary<Type, List<MessageCallback>>();
        }

        public async void OnMessageReceived(FlowConnection connection, FlowMessage message)
        {
            if (delegates.ContainsKey(message.GetType()))
            {
                var callbacks = delegates[message.GetType()];
                foreach (var callback in callbacks)
                {
                    await callback((connection, message));
                }
            }
        }

        public List<MessageCallback> GetMessageDelegate(Type type)
        {
            if (!delegates.ContainsKey(type))
            {
                delegates.Add(type, new List<MessageCallback>());
            }

            return delegates[type];
        }
    }
}