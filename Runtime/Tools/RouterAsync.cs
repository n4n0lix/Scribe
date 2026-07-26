using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Scribe.Tools
{
    public class RouterAsync
    {
        static readonly MethodInfo routeTypedMethod = typeof(RouterAsync)
            .GetMethod(nameof(RouteSpecificTyped), BindingFlags.NonPublic | BindingFlags.Instance);

        readonly List<Func<object, UniTask>> genericHandlers  = new List<Func<object, UniTask>>();
        readonly Dictionary<Type, IList>     specificHandlers = new Dictionary<Type, IList>();

        public void AddGenericHandler(Func<object, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            genericHandlers.Add(handler);
        }

        public void RemoveGenericHandler(Func<object, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            genericHandlers.Remove(handler);
        }

        public void AddHandler(Type messageType, Func<object, UniTask> handler)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!specificHandlers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Func<object, UniTask>>();
                specificHandlers[messageType] = handlers;
            }

            ((List<Func<object, UniTask>>)handlers).Add(handler);
        }

        public void RemoveHandler(Type messageType, Func<object, UniTask> handler)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!specificHandlers.TryGetValue(messageType, out var rawHandlers)) return;

            var handlers = (List<Func<object, UniTask>>)rawHandlers;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                specificHandlers.Remove(messageType);
        }

        public void AddHandler<TMessage>(Func<TMessage, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(TMessage);
            if (!specificHandlers.TryGetValue(messageType, out var handlers))
            {
                handlers = new List<Func<TMessage, UniTask>>();
                specificHandlers[messageType] = handlers;
            }

            ((List<Func<TMessage, UniTask>>)handlers).Add(handler);
        }

        public void RemoveHandler<TMessage>(Func<TMessage, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(TMessage);
            if (!specificHandlers.TryGetValue(messageType, out var rawHandlers)) return;

            var handlers = (List<Func<TMessage, UniTask>>)rawHandlers;
            handlers.Remove(handler);
            if (handlers.Count == 0)
                specificHandlers.Remove(messageType);
        }

        public int GenericHandlerCount => genericHandlers.Count;

        public int GetHandlerCount(Type messageType)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            return specificHandlers.TryGetValue(messageType, out var handlers) ? handlers.Count : 0;
        }

        public int GetHandlerCount<TMessage>() => GetHandlerCount(typeof(TMessage));

        public async UniTask Route(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var messageType = message.GetType();
            await RouteGeneric(message);
            await RouteSpecific(messageType, message);
        }

        async UniTask RouteGeneric(object message)
        {
            foreach (var handler in genericHandlers.ToArray())
            {
                try
                {
                    await handler(message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        async UniTask RouteSpecific(Type messageType, object message)
        {
            if (!specificHandlers.TryGetValue(messageType, out var handlers)) return;

            if (handlers is List<Func<object, UniTask>> objectHandlers)
            {
                foreach (var handler in objectHandlers.ToArray())
                {
                    try
                    {
                        await handler(message);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                return;
            }

            var genericMethod = routeTypedMethod.MakeGenericMethod(messageType);
            await (UniTask)genericMethod.Invoke(this, new[] { message });
        }

        async UniTask RouteSpecificTyped<TMessage>(object message)
        {
            if (!specificHandlers.TryGetValue(typeof(TMessage), out var handlers)) return;

            foreach (var handler in ((List<Func<TMessage, UniTask>>)handlers).ToArray())
            {
                try
                {
                    await handler((TMessage)message);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}