namespace MTSC.Common
{
    public interface IProducerQueue<in T>
    {
        /// <summary>
        /// Enqueues the value to the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        void Enqueue(T value);
    }
}
