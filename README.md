# ACMESharp Core
An ACME client library for .NET Standard (Let's Encrypt)

***This project is a very early work-in-progress.***

This is a port of the [ACMESharp](https://github.com/ebekker/ACMESharp) client library to .NET Standard 2.0.

 Please Note: |
--------------|
If you are interested in using Let's Encrypt, or any other ACME-compliant CA in a .NET context, please see the [ACMESharp project](https://github.com/ebekker/ACMESharp) for a working implementation of an ACME client library for .NET Framework and complementary PowerShell module for Windows PowerShell.

----

The goals for this project:

* Migrate the ACMESharp client library to .NET Standard 2.0
* Remove legacy cruft
* Clean up the namespace structure and code org
* Adjust coding standards to better conform with industry standards
* Clearly separate and maintain independently the client library and the PS module
* Complete any missing features from the ACME spec
* Prepare for, and implement move to ACME 2.0 spec
