# README - ACMESharp Core

## Changes

* Based on .NET Standard / .NET Core
  * Moved to HttpClient from WebRequest
* Using async code throughout
* Uses PO files for message localization?
* Expanded support for RSA keys to include more SHA (348, 512) and RSA (2048-4096) sizes
* Added support for EC keys for both account and cert keys, supporting standard curves
  * P-256
  * P-348
  * P-521
* Improved Integration Testing setup
* Better separation of concerns in the core API

* Hoped to use some other, more complete and well maintained JOSE library for
  doing things like JWS signing and JWK-related operations, but unfortunately
  the landscape for this hasn't changed too much in that still needed to use
  an internal library for this.

## What's Implemented and Working

* ACME Resource Directory Lookup
* First Nonce Lookup
* Create Account
  * Create/Check Duplicate Account
* Update Account
* Change Account Key
* Deactivate Account
* Create Order
* Decode Challenge details for types:
  * `dns-01`
  * `http-01`
  * `tls-alpn-01` - initial support for [this extension](https://tools.ietf.org/html/draft-ietf-acme-tls-alpn-05) (thanks [Wouter](https://github.com/WouterTinus))
* Answer Challenge
* Refresh Challenge
* Deactivate Authorization
* Finalize Authorization (Submit CSR)
* Revoke Certificate (thank you [Wouter](https://github.com/WouterTinus)!)
* Cross-platform support
  * Tested on [Windows](https://ci.appveyor.com/project/ebekker/acmesharpcore/build/job/6vive79j4xprmh93/tests)
  * Tested on [Linux](https://ci.appveyor.com/project/ebekker/acmesharpcore/build/job/1a528ap82uol4bsg/tests) (Ubuntu 16.04)

## What's Not Implemented/Not Working

* Automatic detection/handling of Change of TOS from [ACME 7.3.4](https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.4) is not implemented
* External Account Binding from [ACME 7.3.5](https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.5) has not been implemented
* Order Pre-Authorizations from [ACME 7.4.1](https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4.1) is not implemented

## Contributors

* Translate PO files
