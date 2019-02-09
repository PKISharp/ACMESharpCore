# README - `PKISharp.SimplePKI` <img align="right" width="100" src="https://raw.githubusercontent.com/PKISharp/ACMESharpCore/master/docs/pkisharp-logo-color.png">

The purpose of this support library is to provide a small, targeted
API that implements the specific operations that are typically needed on the
client side when working with certificates and certificate requests.


[![NuGet Pre Release](https://img.shields.io/nuget/vpre/PKISharp.SimplePKI.svg)](https://www.nuget.org/packages/PKISharp.SimplePKI)


## Background

This library originated out of a need to handle various client-side operations
in support of the ACMESharp client.

In order to use the [ACMESharp](https://github.com/PKISharp/ACMESharpCore) library
to properly interact with an ACME CA in producing signed certificates it is necessary to
perform various PKI certificate management operations on the client side such as selecting
and generating key pairs for supported asymmetric encryption algorithms, generating a
certificate signing request in a DER encoded format, and exporting
a certificate and its private key in a usable archive format.

Eventually, it is hoped that one would be able to do all these operations out of the box
with the .NET platform, specifically with .NET Standard, but today there is a limited
set of these operations that are possible.  Work is already underway to expand this
support (such as [here](https://github.com/dotnet/corefx/issues/21833),
[here](https://github.com/dotnet/corefx/issues/20414) and
[here](https://github.com/dotnet/designs/issues/11)).

In the meantime, support can be found *outside of the box* with the help of the
excellent [Bouncy Castle](https://www.bouncycastle.org/csharp/index.html) crypto
library.

While this library grew out of a need to support the ACME protocol and process,
it is independent of the ACMESharp library and stands on its own as a useful tool.

## Features

The following primary PKI-supporting entities and operations are supported by SimplePKI:

* Asymmetric Keys and Key Pairs:
  * Generate RSA and ECDSA key pairs using common key lengths and named curves (EC)
  * Export to DER/PEM formats
* Certificate Signing Requests:
  * Conforms to PKCS#10 request formats
  * Export to DER/PEM formats
  * Generate Signed certs
  * Generate Self-signed and CA-like certs
  * Support for Subject Alternative Name (SAN) extension
* Certificates:
  * Export to DER/PEM formats
  * Export to PKCS#12 format archive with optional private key and certificate chain
  * Conversion to standard BCL `X509Certificate2`

Additionally, all the primary entities listed above support saving to/loading from an
opaque persistent format that can be useful when needing to support long-running
operations that require durable storage.
