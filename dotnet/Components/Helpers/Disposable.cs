using System;

namespace TeamHitori.Mulplay.Container.Web.Components.Helpers
{
    public class Disposable : IDisposable
    {
        private readonly Action onDispose;

        public Disposable(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            onDispose();
        }
    }
}
