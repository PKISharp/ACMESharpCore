using System;

namespace ACMESharp.Crypto.JOSE
{
    public class JWSAlgorithm
    {
        private IJwsSigner _jwsTool;

        public JWSAlgorithm(string jwsAlgorithm)
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

        public JWSAlgorithm(JwsAlgorithmExport exported)
            :this(exported.Algorithm)
        {
            _jwsTool.Import(exported.PrivateKey);
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
                PrivateKey = _jwsTool.ExportPrivateJwk()
            };
            
            return export;
        }

        public object ExportPublicJwk()
        {
            return _jwsTool.ExportPublicJwk();
        }

        public byte[] Sign(byte[] raw)
        {
            return _jwsTool.Sign(raw);
        }
    }
}
