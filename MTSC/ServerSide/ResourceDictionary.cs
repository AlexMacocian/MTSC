using System;
using System.Collections.Generic;

namespace MTSC.ServerSide
{
    public class ResourceDictionary : IDisposable
    {
        private Dictionary<Type, object> Resources = new();

        public void RemoveResourceIfExists<TValue>()
        {
            if (this.Resources.ContainsKey(typeof(TValue)))
            {
                this.Resources.Remove(typeof(TValue));
            }
        }

        public void RemoveResource<TValue>()
        {
            this.Resources.Remove(typeof(TValue));
        }

        public void SetResource<TValue>(TValue value)
        {
            this.Resources[typeof(TValue)] = value;
        }

        public TValue GetResource<TValue>()
        {
            return (TValue)this.Resources[typeof(TValue)];
        }

        public bool Contains<TValue>()
        {
            return this.Resources.ContainsKey(typeof(TValue));
        }

        public bool TryGetResource<TValue>(out TValue value)
        {
            if (this.Resources.TryGetValue(typeof(TValue), out var objValue))
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
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    foreach (var value in this.Resources.Values)
                    {
                        (value as IDisposable)?.Dispose();
                    }

                    this.Resources.Clear();
                    this.Resources = null;
                    
                }

                this.disposedValue = true;
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
            this.Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
