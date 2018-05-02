using System;
using System.Net.Http;

namespace ACMESharp.IntegrationTests
{
    /// <summary>
    /// Used to share an ACME client and related state
    /// across multiple test methods of a test class.
    /// </summary>
    public class ClientsFixture : IDisposable
    {
        public Uri BaseAddress { get; set; }

        public HttpClient Http { get; set; }

        public AcmeClient Acme { get; set; }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Acme?.Dispose();
                    Acme = null;
                    Http?.Dispose();
                    Http = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~HttpClientFixture() {
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