**Extension enforces the use of the ConfigureAwait expression.**

Extension for Visual studio (2019) is available here: https://marketplace.visualstudio.com/items?itemName=Rene-Stein.ConfigureAwaitEnforcer

Extension for Visual studio 2022 (preview) is available here:  https://marketplace.visualstudio.com/items?itemName=Rene-Stein.ConfigureAwaitEnforcer2022

Nuget:  _Install-Package ConfigureAwaitEnforcer_
(https://www.nuget.org/packages/ConfigureAwaitEnforcer/)

**Version 2.0.0**
- **Added support for Visual Studio 2022  (Preview). (VS 2022 extension: - https://marketplace.visualstudio.com/items?itemName=Rene-Stein.ConfigureAwaitEnforcer2022)**
- Added support for 'await foreach' statement.
- Added support for 'await using' statement.

**Version 1.2.0**

- Better support for the nested await expressions.

e. g.
```
await tf.StartNew(async () => await Task.FromResult(5).ConfigureAwait(false)).ConfigureAwait(false);
```
```
await tf.StartNew(async () => await tf.StartNew(async () => await Task.FromResult(new Object()).ConfigureAwait(false)).ConfigureAwait(false)).ConfigureAwait(false);
```
- Support for expressions that return ValueTask<T>. Support for async LINQ.

e.g.
```
 var parseResult = await enumerateLines(reader) 
                              .Where(line => !String.IsNullOrEmpty(line) || line[0].Equals(COMMENT))
                              .AggregateAsync((ParserState.WaitingForExpressionFormat, new Sat(SimpleDPLLStrategy.Solve)),
                                              parseLine).ConfigureAwait(false);
```

**Version 1.1.3.0**
- Support for the VS 2019 RTM

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

