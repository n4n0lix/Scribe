using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace Scribe.Tools
{
    public sealed class KeyedRouterAsync<TKey, TMessage>
    {
        readonly Func<TKey, IReadOnlyList<Func<TMessage, UniTask>>> handlerLookup;
        readonly Func<TMessage, UniTask>                            fallbackHandler;
        readonly Action<Exception>                                  exceptionHandler;

        public KeyedRouterAsync(
            Func<TKey, IReadOnlyList<Func<TMessage, UniTask>>> handlerLookup,
            Func<TMessage, UniTask> fallbackHandler = null,
            Action<Exception> exceptionHandler = null)
        {
            this.handlerLookup = handlerLookup
                ?? throw new ArgumentNullException(nameof(handlerLookup));

            this.fallbackHandler = fallbackHandler;
            this.exceptionHandler = exceptionHandler ?? Debug.LogException;
        }

        public async UniTask Route(TKey key, TMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var handlers = handlerLookup(key);

            if (handlers != null && handlers.Count > 0)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(message);
                    }
                    catch (Exception e)
                    {
                        exceptionHandler(e);
                    }
                }

                return;
            }

            if (fallbackHandler == null)
                return;

            try
            {
                await fallbackHandler(message);
            }
            catch (Exception e)
            {
                exceptionHandler(e);
            }
        }
    }
}