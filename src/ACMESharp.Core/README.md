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


* Hoped to use some other, more complete and well maintained JOSE library for
  doing things like JWS signing and JWK-related operations, but unfortunately
  the landscape for this hasn't changed too much in that still needed to use
  an internal library for this.

## Contributors

* Translate PO files
