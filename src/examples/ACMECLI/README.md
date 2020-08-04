# README - ACMECLI

This project provides an example .NET Core console application that demonstrates one approach
to using the ACMESharpCore client library for obtaining PKI certificates from the Let's Encrypt
CA.

***This is only a simple example* -- it is not a thorough or necessarily complete application**,
but it does cover all the basic steps that are necessary.  The general flow of obtaining a
certificate from Let's Encrypt (or any ACME v2-compliant CA) is as follows:

* Create an Account with the CA.
* Create an Order with the CA for one or more target DNS Identifiers (DNS names).
  * This can include wildcard names (ACME v2) and, specifically for Let's Encrypt,
    up to 100 Subject Alternative Names (SAN) on a single certificate.
* For each Authorization required for the Order, resolve the Challenge using
  either the DNS or HTTP method.
* Submit the Challenge Answer, wait for each Authorization to become Valid.
* Finalize the Order with a CSR, wait for the Order to become valid.
* Retrieve the requested certificate and associated signer chain.

## CLI Options

The tool provides for the following parameters and options:

```shell
Usage: ACMECLI [options]

Options:
  --state <STATE>                              Directory to store stateful information; defaults to current
  --ca-name <CA_NAME>                          Name of a predefined ACME CA base endpoint (specify invalid value to see list)
  --ca-url <CA_URL>                            Full URL of an ACME CA endpoint; this option overrides CaName
  --refresh-dir                                Flag indicates to refresh the current cached ACME Directory of service endpoints for the target CA
  --email <EMAIL>                              One or more emails to be registered as account contact info (can be repeated)
  --accept-tos                                 Flag indicates that you agree to CA's terms of service
  --dns <DNS>                                  One or more DNS names to include in the cert; the first is primary subject name, subsequent are subject alternative names (can be repeated)
  --name-server <NAME_SERVER>                  One or more DNS name servers to be used to resolve host entries, such as during testing (can be repeated)
  --refresh-order                              Flag indicates to refresh the state of pending ACME Order
  --challenge-type <CHALLENGE_TYPE>            Indicates that only one specific Challenge type should be handled
  --refresh-challenges                         Flag indicates to refresh the state of the Challenges of the pending ACME Order
  --test-challenges                            Flag indicates to check if the Challenges have been handled correctly
  --wait-for-test[:<WAIT_FOR_TEST>]            Flag indicates to wait until Challenge tests are successfully validated, optionally override the default timeout of 300 (seconds)
  --answer-challenges                          Flag indicates to submit Answers to pending Challenges
  --wait-for-authz[:<WAIT_FOR_AUTHZ>]          Flag indicates to wait until Authorizations become Valid, optionally override the default timeout of 300 (seconds)
  --finalize                                   Flag indicates to finalize the pending ACME Order
  --key-algor <KEY_ALGOR>                      Indicates the encryption algorithm of certificate keys, defaults to RSA
  --key-size <KEY_SIZE>                        Indicates the encryption algorithm key size, defaults to 2048 (RSA) or 256 (EC)
  --regenerate-csr                             Flag indicates to regenerate a certificate key pair and CSR
  --refresh-cert                               Flag indicates to refresh the local cache of an issued certificate
  --wait-for-cert[:<WAIT_FOR_CERT>]            Flag indicates to wait until Certificate is available, optionally override the default timeout of 300 (seconds)
  --export-cert <EXPORT_CERT>                  Save the certificate chain (PEM) to the named file path
  --export-pfx <EXPORT_PFX>                    Save the certificate chain as PFX (PKCS12) to the named file path
  --export-pfx-password <EXPORT_PFX_PASSWORD>  Includes the private key to the PFX (PKCS12) and secures with specified password (use ' ' for no password)
  -?|-h|--help                                 Show help information
```

You can invoke it piecemeal and complete each step independently or you can combine all the
steps together using the "wait" options to have the tool test and wait for individual steps
to be completed.

## Piecemeal Invocation

For example, here's a sample sequence to control the flow yourself and do each part piecemeal:

```shell
Create a new Account; this will dump the Account details afterwards
> acmecli --email foobar@example.com --accept-tos
```

Create a new Order with a primary DNS and 2 alternates; this will dump
the details for how to handle each Challenge response using the DNS method

```shell
> acmecli --dns myapp.example.com --dns myapp-0.example.com --dns myapp-1.example.com --challenge-type dns-01
```

At this point, use the info provided to create the appropriate
DNS TXT records for each DNS entry so that it will be authorized
Be sure to test your DNS responses to make sure they are returning
the expected response -- you may have to wait a bit before the
DNS TTL values expire and the correct response (or any response)
is finally returned from a DNS query. You can use the tool to confirm
the Challenge has been implemented

```shell
> acmecli --dns myapp.example.com --dns myapp-0.example.com --dns myapp-1.example.com --challenge-type dns-01 --test-challenges
```

Now it's time to submit the Challenge answers so that the ACME CA can test
them authorize your account to create certificates with associated DNS names.

```shell
> acmecli --dns myapp.example.com --dns myapp-0.example.com --dns myapp-1.example.com --answer-challenges
```

Now we finalize the Order -- this will also create a new
private key and generate the CSR to submit to the CA.

```shell
> acmecli --dns myapp.example.com --dns myapp-0.example.com --dns myapp-1.example.com --finalize
```

Finally, save the complete certificate chain and corresponding
private key to a PKCS#12 format file with NO password.

```shell
> acmecli --dns myapp.example.com --dns myapp-0.example.com --dns myapp-1.example.com --export-pfx mycertificate.pfx --export-pfx-password " "
```

## Invoke in _One Fell Swoop_

Alternatively, you can invoke the tool once with some additional options that indicate
waiting periods in between steps which will pause and test for expected outcomes from
either the ACME CA or from your actions, i.e. by completing the Challenges.

```shell
## In this case all the DNS Identifiers are wildcards, so the
## CA will only issue DNS type Challenges as per the ACME spec
> acmecli --email jane.doe@example.com --email john.doe@email.com --accept-tos --dns *.example.com --dns *.example.net --test-challenges --wait-for-test:600 --answer-challenges --wait-for-authz --finalize --key-algor ec --key-size 256 --wait-for-cert --export-cert my-example.pem --export-pfx my-example.pfx --export-pfx-password " "
```

With the single command above:

* We create a new Account with 2 email contacts and agree to the ToS
* Create an order with 2 wildcard DNS names -- as per the ACME spec,
  wildcard certs can only be authorized using DNS Challenges
* We've indicated that the DNS Challenges should be tested *and* that
  we should wait until we get a successful, expected response.
  * In this case, we've specified a 10-minute timeout for the wait,
    which should give us enough time to manually create the necessary
    DNS record entries to answer the Challenges
* Once the Challenges test successfully, we Answer them and wait for
  the Authorizations to be valid (default wait of 5 mins)
* Next, we finalize the order
  * We request an Elliptic Curve private key with a size of 256
* Finally, we wait for the Order to become finalized and the certificate
  to be issued
* Lastly, we export the certificate chain to a PEM-encoded file and export
  the chain with private key to a PKCS#12 format file.

## What's Missing?

This example program can be used to request multiple certificates with different
combinations of DNS names, but it does not handle the case of issuing different
certificates with the same names, such as when a certificate expires.  To do that
you would need to delete the state data that's saved to disk for a particular
order.
