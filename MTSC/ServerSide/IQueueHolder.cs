using MTSC.Common;

namespace MTSC.ServerSide
{
    interface IQueueHolder<T>
    {
        IConsumerQueue<T> ConsumerQueue { get; }
        void Enqueue(T value);
        T Dequeue();
        bool TryDequeue(out T Value);
    }
}
