using System;
using System.Collections.Generic;

namespace MTSC.ServerSide
{
    public class ResourceDictionary : IDisposable
    {
        private Dictionary<Type, object> Resources = new Dictionary<Type, object>();

        public void RemoveResource<TValue>()
        {
            Resources.Remove(typeof(TValue));
        }

        public void SetResource<TValue>(TValue value)
        {
            Resources[typeof(TValue)] = value;
        }

        public TValue GetResource<TValue>()
        {
            return (TValue)Resources[typeof(TValue)];
        }

        public bool Contains<TValue>()
        {
            return Resources.ContainsKey(typeof(TValue));
        }

        public bool TryGetResource<TValue>(out TValue value)
        {
            if (Resources.TryGetValue(typeof(TValue), out object objValue))
            {
                value = (TValue)objValue;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                foreach(var value in Resources.Values)
                {
                    (value as IDisposable)?.Dispose();
                }
                Resources.Clear();
                Resources = null;
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ResourceDictionary()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
