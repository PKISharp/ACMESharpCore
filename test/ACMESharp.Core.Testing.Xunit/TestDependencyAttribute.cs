using System;

namespace ACMESharp.Testing.Xunit
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestDependencyAttribute : Attribute
    {
        /// <param name="methodName">the name of the test method (Fact)
        ///     that is a dependency.</param>
        public TestDependencyAttribute(string methodName)
        {
            MethodName = methodName;
        }

        /// <summary>
        /// The name of the test method (Fact) that is a dependency.
        /// </summary>
        public string MethodName { get; }
    }
}