using System;

namespace ACMESharp.Crypto.JOSE
{
    public class JwsTool
    {
        private IJwsTool _jwsTool;

        public JwsTool(string JwsAlgorithm)
        {
            if (JwsAlgorithm.StartsWith("ES"))
            {
                var tool = new Impl.ESJwsTool();
                tool.HashSize = int.Parse(JwsAlgorithm.Substring(2));

                _jwsTool = tool;
            }

            if (JwsAlgorithm.StartsWith("RS"))
            {
                var tool = new Impl.RSJwsTool();
                tool.KeySize = int.Parse(JwsAlgorithm.Substring(2));

                _jwsTool = tool;
                tool.Init();
            }

            if (_jwsTool == null)
                throw new InvalidOperationException("Unknown JwsAlgorithm");

            _jwsTool.Init();
        }

        public JwsTool(JwsExport exported)
            :this(exported.JwsAlgorithm)
        {
            _jwsTool.Import(exported.JwsParameters);
        }

        public string JwsAlg => _jwsTool.JwsAlg;
        
        public void Dispose()
        {
            _jwsTool.Dispose();
        }

        public JwsExport Export()
        {
            var export = new JwsExport
            {
                JwsAlgorithm = _jwsTool.JwsAlg,
                JwsParameters = _jwsTool.Export()
            };
            
            return export;
        }

        public object ExportJwk(bool canonical = false)
        {
            return _jwsTool.ExportJwk(canonical);
        }

        public byte[] Sign(byte[] raw)
        {
            return _jwsTool.Sign(raw);
        }
    }
}
