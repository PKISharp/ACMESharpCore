using System;

namespace ACMESharp.Testing.Xunit
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TestCollectionDependencyAttribute : Attribute
    {
        public TestCollectionDependencyAttribute(Type @class)
        {
            Class = @class;
        }

        /// <summary>
        /// The test class (Collection) that is a dependency.
        /// </summary>
        public Type Class { get; }
    }
}