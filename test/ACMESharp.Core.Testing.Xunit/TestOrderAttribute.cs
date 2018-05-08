using System;

namespace ACMESharp.Testing.Xunit
{
    /// <summary>
    /// Declares a relative test ordering weight.  Can be applied to individual
    /// test methods (test facts) or classes (test collections).
    /// </summary>
    /// <remarks>
    /// To be used <b>only</b> for <b>non-unit-tests</b> such as integration tests
    /// where the exact order of individual tests is significant due to dependencies
    /// between tests.
    /// <para>
    /// To use this attribute, you must use the <see cref="TestOrderer"/> custom
    /// test ordering support.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class TestOrderAttribute : Attribute
    {
        public TestOrderAttribute(int order, string group = null)
        {
            Order = order;
            Group = group;
        }

        public int Order { get; }

        public string Group { get; }
    }
}