# ACMESharp Core <img align="right" width="100" src="https://raw.githubusercontent.com/PKISharp/ACMESharpCore/master/docs/acmesharp-logo-color.png">

An ACME client library for .NET Standard (Let's Encrypt)

:star: I appreciate your star, it helps me decide to which OSS projects I should allocate my spare time.

![CI](https://github.com/PKISharp/ACMESharpCore/workflows/CI/badge.svg)

<!--
[![AV Build status](https://ci.appveyor.com/api/projects/status/bvf3hiyantc3m8tv?svg=true)](https://ci.appveyor.com/project/ebekker/acmesharpcore)
[![ADO Build Status](https://dev.azure.com/PKISharp/ACMESharpCore/_apis/build/status/ACMESharpCore-ASP.NET%20Core%20(.NET%20Framework)-CI?branchName=master)](https://dev.azure.com/PKISharp/ACMESharpCore/_build/latest?definitionId=2&branchName=master)
-->

## Tests

| Component/Test Type | Linux | Windows |
|-|-:|-:|
| Base Unit Tests |[![Test](https://gist.github.com/ebekker/2241c76d38225d73fdab6b6f836bf475/raw/edd99b9bc5c2e1150858b4017cb487e1fbe3ef92/acmesharpcore-unit_tests-ubuntu.md_badge.svg)](https://gist.github.com/ebekker/2241c76d38225d73fdab6b6f836bf475) | [![Test](https://gist.github.com/ebekker/5f38b28861265c7c6270a7cdd17f48d9/raw/657cb5daaacf376963d1dc016e24ecda5bec721a/acmesharpcore-unit_tests-windows.md_badge.svg)](https://gist.github.com/ebekker/5f38b28861265c7c6270a7cdd17f48d9) |
| SimplePKI Unit Tests |[![Test](https://gist.github.com/ebekker/51e6ff56691ffc0f9711c03e0881075d/raw/d818c129979426f9fd0f1357ebe1f9f9eabcf08d/acmesharpcore-simplepki_unit_tests-ubuntu.md_badge.svg)](https://gist.github.com/ebekker/51e6ff56691ffc0f9711c03e0881075d) | [![Test](https://gist.github.com/ebekker/a4f43f8b87fc2ea76c81e87f9485e93f/raw/08c366d095a3ab8a19082aafea0967bf96264a30/acmesharpcore-simplepki_unit_tests-windows.md_badge.svg)](https://gist.github.com/ebekker/a4f43f8b87fc2ea76c81e87f9485e93f) |
| MockServer Unit Tess |[![Test](https://gist.github.com/ebekker/1710122c8142afca6d17baa949337b26/raw/4cc3ef38ce1aec888335c7ddcc881bb623037c49/acmesharpcore-mockserver_unit_tests-ubuntu.md_badge.svg)](https://gist.github.com/ebekker/1710122c8142afca6d17baa949337b26) | [![Test](https://gist.github.com/ebekker/f0cf60ccad4b402729657dc3ffb3a0b0/raw/9d7e199f7dbdb44e5df1c4be873fe57f292d8d31/acmesharpcore-mockserver_unit_tests-windows.md_badge.svg)](https://gist.github.com/ebekker/f0cf60ccad4b402729657dc3ffb3a0b0) |
| Integration Tests | [![Test](https://gist.github.com/ebekker/e0e7c1cc484fb0f306453f920b6c6afc/raw/4d2629679bd42c24c2055750d1cca033facf0cc4/acmesharpcore-integration_tests-ubuntu.md_badge.svg)](https://gist.github.com/ebekker/e0e7c1cc484fb0f306453f920b6c6afc) | [![Test](https://gist.githubusercontent.com/ebekker/97a5714f0f4a70cefe832be7aa190765/raw/acmesharpcore-integration_tests-windows.md_badge.svg)](https://gist.githubusercontent.com/ebekker/97a5714f0f4a70cefe832be7aa190765)

## Packages

| Component | Stable Release | Early Access |
|-|-|-|
| | Hosted on the [NuGet Gallery](https://www.nuget.org/packages?q=Tags%3A%22acmesharp%22) | Hosted on [MyGet Gallery](https://www.myget.org/gallery/acmesharp)
| ACMESharpCore client library | [![NuGet](https://img.shields.io/nuget/v/ACMESharpCore.svg)](https://www.nuget.org/packages/ACMESharpCore) | [![MyGet](https://img.shields.io/myget/acmesharp/vpre/ACMESharpCore.svg)](https://www.myget.org/feed/acmesharp/package/nuget/ACMESharpCore)
| Crypto Support library | [![NuGet](https://img.shields.io/nuget/v/ACMESharpCore.Crypto.svg)](https://www.nuget.org/packages/ACMESharpCore.Crypto) | [![MyGet](https://img.shields.io/myget/acmesharp/vpre/ACMESharpCore.Crypto.svg)](https://www.myget.org/feed/acmesharp/package/nuget/ACMESharpCore.Crypto)
| SimplePKI library | [![NuGet](https://img.shields.io/nuget/v/PKISharp.SimplePKI.svg)](https://www.nuget.org/packages/PKISharp.SimplePKI) | [![MyGet](https://img.shields.io/myget/acmesharp/vpre/PKISharp.SimplePKI.svg)](https://www.myget.org/feed/acmesharp/package/nuget/PKISharp.SimplePKI)

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
