namespace MTSC.Common
{
    public interface IConsumerQueue<T>
    {
        /// <summary>
        /// Dequeues the first element from the queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        T Dequeue();
        /// <summary>
        /// Tries to dequeue the first element from the queue. <see cref="T"/> is set to the proper value 
        /// if the operation succeeds. Otherwise, it is set to default value of <see cref="T"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns>True if the operation succeeds. False if the operations has failed</returns>
        bool TryDequeue(out T value);
    }
}
