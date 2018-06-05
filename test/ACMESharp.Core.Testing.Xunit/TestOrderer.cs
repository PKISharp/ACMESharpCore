using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ACMESharp.Testing.Xunit
{
    // With guidance from:
    //    https://damsteen.nl/blog/2016/06/08/ordered-tests-with-nunit-mstest-xunit-pt4-xunit

    public class TestOrderer : ITestCaseOrderer, ITestCollectionOrderer
    {
        // These have to be constants to be usable in the Xunit.TestCaseOrderer and
        // Xunit.TestCollectionOrderer assembly-level attributes however we define
        // a static class constructor whose purpose is to verify their correctness
        public const string TypeName = "ACMESharp.Testing.Xunit.TestOrderer";
        public const string AssemblyName = "ACMESharp.Testing.Xunit";

        private Random _rng = new Random();

        static TestOrderer()
        {
            var t = typeof(TestOrderer);

            var tName = t.FullName;
            if (TypeName != tName)
                throw new Exception($"TestOrderer.TypeName constant [{TypeName}] is WRONG ({tName})");

            var aName = t.Assembly.GetName().Name;
            if (AssemblyName != aName)
                throw new Exception($"TestOrderer.AssemblyName constant [{AssemblyName}] is WRONG ({aName})");
        }

        IEnumerable<ITestCollection> ITestCollectionOrderer.OrderTestCollections(IEnumerable<ITestCollection> testCollections)
        {
            // First we randomize, then we sort -- this gives us variations for any tests
            // that are not explicitly ordered or that are assigned the same order weight.
            return testCollections.OrderBy(x => _rng.Next()).OrderBy(x => GetTestOrder(x));
        }

        IEnumerable<TTestCase> ITestCaseOrderer.OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) // where TTestCase : ITestCase
        {
            // First we randomize, then we sort -- this gives us variations for any tests
            // that are not explicitly ordered or that are assigned the same order weight.
            return testCases.OrderBy(x => _rng.Next()).OrderBy(x => GetTestOrder(x));
        }

        public static int GetTestOrder(MemberInfo member)
        {
            return member.GetCustomAttribute<TestOrderAttribute>()?.Order ?? int.MaxValue;
        }

        public static string GetTestGroup(MemberInfo member)
        {
            return member.GetCustomAttribute<TestOrderAttribute>()?.Group;
        }

        public static int GetTestOrder(object instance)
        {
            var t = instance.GetType();
            return t.GetCustomAttribute<TestOrderAttribute>()?.Order ?? int.MaxValue;
        }

        public static int GetTestOrder(ITestCollection tc)
        {
            var cd = tc.CollectionDefinition;
            var toa = cd?.GetCustomAttributes(typeof(TestOrderAttribute)).FirstOrDefault();

            if (toa != null)
                return toa.GetNamedArgument<int>(nameof(TestOrderAttribute.Order));

            var tt = Type.GetType(tc.DisplayName);
            return tt?.GetCustomAttribute<TestOrderAttribute>().Order ?? int.MaxValue;
        }

        public static long GetTestOrder(ITestCase tc)
        {
            var tm = tc.TestMethod.Method;
            var toa = tm.GetCustomAttributes(typeof(TestOrderAttribute)).FirstOrDefault();

            return toa?.GetNamedArgument<int>(nameof(TestOrderAttribute.Order))
                ?? int.MaxValue;
        }
    }
}