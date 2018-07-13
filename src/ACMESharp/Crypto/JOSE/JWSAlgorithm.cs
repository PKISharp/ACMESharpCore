using System;

namespace ACMESharp.Crypto.JOSE
{
    public class JwsAlgorithm
    {
        private IJwsSigner _jwsTool;

        public JwsAlgorithm(string jwsAlgorithm)
        {
            if (!int.TryParse(jwsAlgorithm.Substring(2), out int size))
                throw new ArgumentException("Could not parse Key or Hash size", nameof(jwsAlgorithm));

            if (jwsAlgorithm.StartsWith("ES"))
            {
                var tool = new Impl.ESJwsSigner(size);

                _jwsTool = tool;
            }

            if (jwsAlgorithm.StartsWith("RS"))
            {
                var tool = new Impl.RSJwsSigner(size);

                _jwsTool = tool;
            }

            if (_jwsTool == null)
                throw new InvalidOperationException("Unknown JwsAlgorithm");
        }

        public JwsAlgorithm(JwsAlgorithmExport exported)
            :this(exported.Algorithm)
        {
            _jwsTool.Import(exported.Export);
        }

        public string JwsAlg => _jwsTool.JwsAlg;
        
        public void Dispose()
        {
            _jwsTool.Dispose();
        }

        public JwsAlgorithmExport Export()
        {
            var export = new JwsAlgorithmExport
            {
                Algorithm = _jwsTool.JwsAlg,
                Export = _jwsTool.ExportAlgorithm()
            };
            
            return export;
        }

        public object ExportPublicJwk()
        {
            return _jwsTool.ExportPublicJwk();
        }

        public byte[] Sign(string raw)
        {
            // For Base64 Strings, UTF8 and ASCII will yield the same results, so we can safely use UTF8
            return Sign(System.Text.Encoding.UTF8.GetBytes(raw));
        }

        public byte[] Sign(byte[] raw)
        {
            return _jwsTool.Sign(raw);
        }
    }
}
