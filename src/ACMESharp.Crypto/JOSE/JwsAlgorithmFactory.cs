using System;
using System.Collections.Generic;
using System.Linq;
using ACMESharp.Crypto.JOSE.Impl;

namespace ACMESharp.Crypto.JOSE
{
    public class JwsAlgorithmFactory
    {
        public class JwsAlgorithmCreator
        {
            public Predicate<string> AlgorithmNamePredicate { get; }
            public Func<string, JwsAlgorithm> CreatorFunction { get; }
            
            public JwsAlgorithmCreator(Predicate<string> algorithmNamePredicate, Func<string, JwsAlgorithm> creatorFunction)
            {
                AlgorithmNamePredicate = algorithmNamePredicate 
                    ?? throw new ArgumentNullException(nameof(algorithmNamePredicate));

                CreatorFunction = creatorFunction 
                    ?? throw new ArgumentNullException(nameof(creatorFunction));
            }
        }

        public List<JwsAlgorithmCreator> Creators { get; private set; }

        public JwsAlgorithmFactory()
        {
            Creators = new List<JwsAlgorithmCreator>();

            AddJwsAlgorithm(ESJwsSigner.IsValidName, alg => new ESJwsSigner(alg));
            AddJwsAlgorithm(RSJwsSigner.IsValidName, alg => new RSJwsSigner(alg));
        }
        
        public void AddJwsAlgorithm(Predicate<string> algorithmNameMatch, Func<string, JwsAlgorithm> creatorFunction)
        {
            Creators.Add(new JwsAlgorithmCreator(algorithmNameMatch, creatorFunction));
        }

        public JwsAlgorithm Create(string algorithmImplementationName)
        {
            var creator = Creators
                .Where(c => c.AlgorithmNamePredicate(algorithmImplementationName))
                .Select(c => c.CreatorFunction)
                .Single();

            return creator(algorithmImplementationName);
        }

        public JwsAlgorithm Create(JwsAlgorithmExport jwsAlgorithmExport)
        {
            var jwsAlgorithm = Create(jwsAlgorithmExport.AlgorithmIdentifier);
            jwsAlgorithm.Import(jwsAlgorithmExport);

            return jwsAlgorithm;
        }
    }
}
