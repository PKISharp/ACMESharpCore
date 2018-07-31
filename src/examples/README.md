# README - ACMESharp Core Examples

This folder provides a collection of example ACME clients that internally use the ACMESharp v2.x
client library.  The idea is to provide range of clients that target different execution
environments and different approaches to collecting input and data from users and storage
systems to provide guidance and solution ideas to others who may wish to build more complete
client solutions atop ACMESharp.

As these examples are worked through and evolved they also will influence and help steer
the direction of the ACMESharp client library itself.

The examples and their current state are as follows:

| Project | Status | Notes
|-|-|-|
| ACMECli | Works! | A console-based CLI app.  See [full details](./ACMECli/README.md).
| ACMEKestrel | Works! | Provide sample middleware and services to automatically obtain ACME certs for a Kestrel-based (ASP.NET Core) app.
| ACMEBlazor | In-progress | Provides a simple, browser-only client based on the [Blazor](https://blazor.net) project.
| ACMETerm | In-progress | Provides a terminal-based GUI app based on the [gui.cs](https://github.com/migueldeicaza/gui.cs) project.
| ACMEForms | In-progress | Provides a WinForms client, testing ACMESharp support on earlier versions of .NET Framework.
| ACMEAvalon | Investigating | Provides a cross-platform, WPF-like GUI client, based on the [Avalonia](http://avaloniaui.net/) project.

