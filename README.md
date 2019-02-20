# ACMESharp Core <img align="right" width="100" src="https://raw.githubusercontent.com/PKISharp/ACMESharpCore/master/docs/acmesharp-logo-color.png">

An ACME client library for .NET Standard (Let's Encrypt)

[![AV Build status](https://ci.appveyor.com/api/projects/status/bvf3hiyantc3m8tv?svg=true)](https://ci.appveyor.com/project/ebekker/acmesharpcore)
[![ADO Build Status](https://dev.azure.com/PKISharp/ACMESharpCore/_apis/build/status/ACMESharpCore-ASP.NET%20Core%20(.NET%20Framework)-CI?branchName=master)](https://dev.azure.com/PKISharp/ACMESharpCore/_build/latest?definitionId=2&branchName=master)

## Packages

| | |
|-|-|
|
| **Stable Release** | Hosted on the [NuGet Gallery](https://www.nuget.org/packages?q=Tags%3A%22acmesharp%22)
| [![NuGet](https://img.shields.io/nuget/v/ACMESharpCore.svg)](https://www.nuget.org/packages/ACMESharpCore)               | ACMESharpCore client library
| [![NuGet](https://img.shields.io/nuget/v/ACMESharpCore.Crypto.svg)](https://www.nuget.org/packages/ACMESharpCore.Crypto) | Crypto Support library
| [![NuGet](https://img.shields.io/nuget/v/PKISharp.SimplePKI.svg)](https://www.nuget.org/packages/PKISharp.SimplePKI)     | SimplePKI library
|
| **Early Access** | Hosted on [MyGet Gallery](https://www.myget.org/gallery/acmesharp)
| [![NuGet](https://img.shields.io/myget/acmesharp/vpre/ACMESharpCore.svg)](https://www.nuget.org/packages/ACMESharpCore)               | ACMESharpCore client library
| [![NuGet](https://img.shields.io/myget/acmesharp/vpre/ACMESharpCore.Crypto.svg)](https://www.nuget.org/packages/ACMESharpCore.Crypto) | Crypto Support library
| [![NuGet](https://img.shields.io/myget/acmesharp/vpre/PKISharp.SimplePKI.svg)](https://www.nuget.org/packages/PKISharp.SimplePKI)     | SimplePKI library

## Overview

This library originated as a port of the [ACMESharp](https://github.com/ebekker/ACMESharp) client library from .NET Framework to .NET Standard 2.0.

However, this rewrite is now actually more complete than the [original](https://github.com/ebekker/ACMESharp),
including operations from the ACME specification that were left out of the original and supporting the latest
versions of the specification.  Check out the [library-specific README](/src/ACMESharp) for details as they develop.

A couple of useful examples have been [put together](https://github.com/PKISharp/ACMESharpCore/tree/master/src/examples) to demonstrate how to use the client library to implement a [CLI tool](https://github.com/PKISharp/ACMESharpCore/tree/master/src/examples/ACMECLI) and automated certificate installation for [ASP.NET Core](https://github.com/PKISharp/ACMESharpCore/tree/master/src/examples/ACMEKestrel) applications.  More are coming...

----

 Please Note: |
--------------|
If you are interested in using Let's Encrypt, or any other ACME-compliant CA in a .NET context, please see the [ACMESharp project](https://github.com/ebekker/ACMESharp) for a working implementation of an ACME client library for .NET Framework and complementary PowerShell module for Windows PowerShell.

----

The goals for this project:

* [x] Migrate the ACMESharp client library to .NET Standard 2.0
* [x] Remove legacy cruft
* [x] Clean up the namespace structure and code org
* [x] Adjust coding standards to better conform with industry standards
* [x] Complete any missing features from the ACME spec
* [x] Prepare for, and implement move to ACME 2.0 spec
* [ ] Clearly separate and maintain independently the client library and the PS module
