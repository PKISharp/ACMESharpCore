using System;

namespace ACMESharp.Crypto.JOSE
{
    public abstract class JwsAlgorithm : IDisposable
    {
        /// <summary>
        /// Returns the Algorithm Identifier used by ACMESharp.
        /// </summary>
        protected string AlgorithmIdentifier { get; set; }

        /// <summary>
        /// Verifies, that the current instance is able to handel the ACMESharp Algorithm Identifier
        /// </summary>
        /// <param name="algorithmIdentifier">The algorithm identifier to validate.</param>
        /// <returns>True, if the instance is able to handle the identifier, otherwise false.</returns>
        protected abstract bool IsValidIdentifier(string algorithmIdentifier);

        /// <summary>
        /// Does the actual signing of the input.
        /// </summary>
        /// <param name="input">The input bytes to be signed.</param>
        /// <returns>The JWS signature.</returns>
        protected abstract byte[] SignInternal(byte[] input);

        /// <summary>
        /// Exports the public JWK in canoncial form as defined in RFC.
        /// </summary>
        /// <returns>An object containing the canical form of the public JWK</returns>
        public abstract object ExportPublicJwk();

        /// <summary>
        /// Exports everything neccessary, to restore the algorithms keys and other needed values.
        /// This serializes the export to string.
        /// </summary>
        /// <returns>String serialized export of algorithm values, like public/private keys.</returns>
        protected abstract string ExportInternal();

        /// <summary>
        /// Imports the algorithm settings from its string serialized form.
        /// </summary>
        /// <param name="exported">The string serialized form generated via ExportInternal()</param>
        protected abstract void ImportInternal(string exported);
        
        /// <summary>
        /// The JWS-conform name of the algorithm.
        /// </summary>
        public string JwsAlg { get; protected set; }
        

        public JwsAlgorithmExport Export()
        {
            var export = new JwsAlgorithmExport
            {
                AlgorithmIdentifier = AlgorithmIdentifier,
                Export = ExportInternal()
            };
            
            return export;
        }

        public void Import(JwsAlgorithmExport exported)
        {
            if (!IsValidIdentifier(exported.AlgorithmIdentifier))
                throw new InvalidOperationException("The AlgorithmIdentifier is not valid for this jwsAlgorithm");

            ImportInternal(exported.Export);
        }
        
        /// <summary>
        /// Gets the bytes of the inputs UTF8 representation and JWS-Signs it.
        /// </summary>
        /// <param name="inputText">The text to be signed</param>
        /// <returns>The JWS signature as byte-array</returns>
        public byte[] Sign(string inputText)
        {
            return SignInternal(System.Text.Encoding.UTF8.GetBytes(inputText));
        }

        /// <summary>
        /// JWS-Signs the input.
        /// </summary>
        /// <param name="input">The byte array to be signed</param>
        /// <returns>The JWS signature as byte array</returns>
        public byte[] Sign(byte[] input)
        {
            return SignInternal(input);
        }

        public abstract void Dispose();
    }
}
