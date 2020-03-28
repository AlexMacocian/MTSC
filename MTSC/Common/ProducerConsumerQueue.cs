using System;
using System.Collections.Generic;

namespace MTSC.Common
{
    public class ProducerConsumerQueue<TValue> : IProducerQueue<TValue>, IConsumerQueue<TValue>
    {
        private Queue<TValue> queue = new Queue<TValue>();

        TValue IConsumerQueue<TValue>.Dequeue()
        {
            lock (queue)
            {
                if (queue.Count > 0) return queue.Dequeue();
                else throw new InvalidOperationException("There are no elements to dequeue from the queue");
            }
        }

        bool IConsumerQueue<TValue>.TryDequeue(out TValue value)
        {
            lock (queue)
            {
                if (queue.Count > 0) 
                {
                    value = queue.Dequeue();
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }
        }

        void IProducerQueue<TValue>.Enqueue(TValue value)
        {
            lock (queue)
            {
                queue.Enqueue(value);
            }
        }
    }
}
