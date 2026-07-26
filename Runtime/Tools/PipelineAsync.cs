using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
namespace Scribe.Tools
{
    public class PipelineAsync
    {
        class Entry<THANDLED>
        {
            public Func<THANDLED, UniTask> Handler;
            public int                     Order;
            public long                    Sequence;
        }

        static readonly MethodInfo executeTypedMethod = typeof(PipelineAsync)
            .GetMethod(nameof(PumpTyped), BindingFlags.NonPublic | BindingFlags.Instance);

        readonly Dictionary<Type, IList> handlers = new Dictionary<Type, IList>();

        long nextSequence;

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var list in handlers.Values)
                    count += list.Count;
                return count;
            }
        }

        public void Add(Func<object, UniTask> handler, int order = 0)
        {
            AddTyped(handler, order);
        }

        public void Remove(Func<object, UniTask> handler)
        {
            RemoveTyped(handler);
        }

        public void AddTyped<THANDLED>(Func<THANDLED, UniTask> handler, int order = 0)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(THANDLED);
            if (!handlers.TryGetValue(messageType, out var rawHandlers))
            {
                rawHandlers = new List<Entry<THANDLED>>();
                handlers[messageType] = rawHandlers;
            }

            ((List<Entry<THANDLED>>)rawHandlers).Add(new Entry<THANDLED>
            {
                Handler = handler,
                Order = order,
                Sequence = nextSequence++
            });
        }

        public void RemoveTyped<THANDLED>(Func<THANDLED, UniTask> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var messageType = typeof(THANDLED);
            if (!handlers.TryGetValue(messageType, out var rawHandlers)) return;

            var typedHandlers = (List<Entry<THANDLED>>)rawHandlers;
            for(var i = typedHandlers.Count - 1; i >= 0; i--)
            {
                if (Equals(typedHandlers[i].Handler, handler))
                    typedHandlers.RemoveAt(i);
            }

            if (typedHandlers.Count == 0)
                handlers.Remove(messageType);
        }

        public async UniTask Pump(object message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            await PumpTyped<object>(message);

            var runtimeType = message.GetType();
            if (runtimeType == typeof(object)) return;

            var genericMethod = executeTypedMethod.MakeGenericMethod(runtimeType);
            await (UniTask)genericMethod.Invoke(this, new[] { message });
        }

        async UniTask PumpTyped<THANDLED>(object message)
        {
            if (!handlers.TryGetValue(typeof(THANDLED), out var rawHandlers)) return;

            var typedHandlers = (List<Entry<THANDLED>>)rawHandlers;
            typedHandlers.Sort((a, b) =>
            {
                var orderCompare = a.Order.CompareTo(b.Order);
                if (orderCompare != 0) return orderCompare;
                return a.Sequence.CompareTo(b.Sequence);
            });

            foreach (var entry in typedHandlers.ToArray())
                await entry.Handler((THANDLED)message);
        }
    }
}