using System;
using System.Collections.Generic;

namespace MTSC.Common
{
    public class ProducerConsumerQueue<TValue> : IProducerQueue<TValue>, IConsumerQueue<TValue>
    {
        private Queue<TValue> queue = new();

        TValue IConsumerQueue<TValue>.Dequeue()
        {
            lock (this.queue)
            {
                if (this.queue.Count > 0)
                {
                    return this.queue.Dequeue();
                }
                else
                {
                    throw new InvalidOperationException("There are no elements to dequeue from the queue");
                }
            }
        }

        bool IConsumerQueue<TValue>.TryDequeue(out TValue value)
        {
            lock (this.queue)
            {
                if (this.queue.Count > 0) 
                {
                    value = this.queue.Dequeue();
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
            lock (this.queue)
            {
                this.queue.Enqueue(value);
            }
        }
    }
}
