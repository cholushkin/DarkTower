using System;
using R3;

namespace Game
{
    /// Minimal lifecycle base for runtime models that own R3 resources. Reactive properties are
    /// registered in Disposables; models owning child models in observable lists dispose them in
    /// OnDispose. The owning session scope disposes the root, which cascades.
    public abstract class ModelBase : IDisposable
    {
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();
        private bool _disposed;

        public bool IsDisposed => _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OnDispose();
            Disposables.Dispose();
        }

        /// Dispose child models and any resources not already registered in Disposables.
        protected virtual void OnDispose() { }
    }
}
