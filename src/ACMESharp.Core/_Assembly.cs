using System.Runtime.CompilerServices;

// Expose private members to "friend" testing assemblies
[assembly: InternalsVisibleTo("ACMESharp.Core.UnitTests")]
[assembly: InternalsVisibleTo("ACMESharp.Core.IntegrationTests")]