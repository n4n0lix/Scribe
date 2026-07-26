using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Scribe.Tools
{
    public class RouterByType
    {
        static readonly System.Reflection.MethodInfo routeTypedMethod = typeof(RouterByType)
            .GetMethod(nameof(RouteSpecificTyped), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        readonly List<Action<object>> genericHandlers = new List<Action<object>>();
        readonly Dictionary<Type, IList> specificHandlers = new Dictionary<Type, IList>();

        public void AddGenericHandler(Action<object> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            genericHandlers.Add(handler);
        }

        public void RemoveGenericHandler(Action<object> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            genericHandlers.Remove(handler);
        }

        public void AddHandler(Type messageType, Action<object> handler)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!specificHandlers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Action<object>>();
                specificHandlers[messageType] = handlers;
            }

            ((List<Action<object>>)handlers).Add(handler);
        }

        public void RemoveHandler(Type messageType, Action<object> handler)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!specificHandlers.TryGetValue(messageType, out var rawHandlers)) return;

            var handlers = (List<Action<object>>)rawHandlers;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                specificHandlers.Remove(messageType);
        }

        public void AddHandler<TMessage>(Action<TMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(TMessage);
            if (!specificHandlers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Action<TMessage>>();
                specificHandlers[messageType] = handlers;
            }

            ((List<Action<TMessage>>)handlers).Add(handler);
        }

        public void RemoveHandler<TMessage>(Action<TMessage> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(TMessage);
            if (!specificHandlers.TryGetValue(messageType, out var rawHandlers)) return;

            var handlers = (List<Action<TMessage>>)rawHandlers;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                specificHandlers.Remove(messageType);
        }

        public void Route(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var messageType = message.GetType();
            RouteGeneric(message);
            RouteSpecific(messageType, message);
        }

        void RouteGeneric(object message)
        {
            foreach (var handler in genericHandlers.ToArray())
            {
                try
                {
                    handler(message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        void RouteSpecific(Type messageType, object message)
        {
            if (!specificHandlers.TryGetValue(messageType, out var handlers)) return;

            if (handlers is List<Action<object>> objectHandlers)
            {
                foreach (var handler in objectHandlers.ToArray())
                {
                    try
                    {
                        handler(message);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                return;
            }

            var genericMethod = routeTypedMethod.MakeGenericMethod(messageType);
            genericMethod.Invoke(this, new[] { message });
        }

        void RouteSpecificTyped<TMessage>(object message)
        {
            if (!specificHandlers.TryGetValue(typeof(TMessage), out var handlers)) return;

            foreach (var handler in ((List<Action<TMessage>>)handlers).ToArray())
            {
                try
                {
                    handler((TMessage)message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
