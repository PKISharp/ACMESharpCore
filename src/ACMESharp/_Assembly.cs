using System.Runtime.CompilerServices;

// Expose private members to "friend" testing assemblies
[assembly: InternalsVisibleTo("ACMESharp.UnitTests")]
[assembly: InternalsVisibleTo("ACMESharp.IntegrationTests")]