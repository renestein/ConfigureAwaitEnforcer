**Extension enforces the use of the ConfigureAwait expression.**

Visual studio extension is available here: https://marketplace.visualstudio.com/items?itemName=Rene-Stein.ConfigureAwaitEnforcer


Nuget:  _Install-Package ConfigureAwaitEnforcer_
(https://www.nuget.org/packages/ConfigureAwaitEnforcer/)

**Version 1.1.3.0**
Support for the VS 2019 RTM

**Version 1.1.2.0**
- Improved compatibility with VS 2019.
- Deleted Microsoft.VisualStudio.MPF.15.0 dependency.

**Version 1.1.1.0**
- Improved compatibility with VS 2019.
- Fixed reading of the configuration.
- Misc. bug fixes.

**Version 1.1.0.0**
- Added ConfigureAwait(true) code fix.
- Added Options page (menu Tools/Options/ConfigureAwaitEnforcer in Visual Studio).
- Added 'Diagnostics severity' option  (values Error, Warning, Info, Hidden). Default value is Error. **You have to restart Visual Studio to see the change.**

