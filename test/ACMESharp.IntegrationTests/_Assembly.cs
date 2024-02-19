
using ACMESharp.Testing.Xunit;
using Xunit;

[assembly: TestCaseOrderer(TestOrderer.TypeName, TestOrderer.AssemblyName)]
[assembly: TestCollectionOrderer(TestOrderer.TypeName, TestOrderer.AssemblyName)]

[assembly: CollectionBehavior(
    CollectionBehavior.CollectionPerClass
    //CollectionBehavior.CollectionPerAssembly
    //,MaxParallelThreads = n
    , DisableTestParallelization = true
)]
