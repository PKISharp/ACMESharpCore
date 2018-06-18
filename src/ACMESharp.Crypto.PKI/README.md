# README - ACMESharp.Crypto.PKI

In order to use the ACMESharp library to properly interact with an ACME CA in producing signed
certificates it is necessary to perform various PKI certificate management operations on the
client side such as selecting and generating key pairs for supported asymmetric encryption
algorithms, generating a certificate signing request in a DER encoded format, and exporting
a certificate and its private key in a usable archive format.

Eventually, it is hoped that one would be able to do all these operations out of the box
with the .NET platform, specifically with .NET Standard, but today there is a limited
set of these operations that are possible.  Work is already underway to expand this
support (such as [here](https://github.com/dotnet/corefx/issues/21833),
[here](https://github.com/dotnet/corefx/issues/20414) and
[here](https://github.com/dotnet/designs/issues/11)).

In the meantime, support can be found *outside of the box* with the help of the
excellent [Bouncy Castle](https://www.bouncycastle.org/csharp/index.html) crypto
library.  The purpose of this ACMESharp library is to provide a small, targeted
API that identifies the specific operations that are typically needed on the
client side when working with the ACME protocol.
