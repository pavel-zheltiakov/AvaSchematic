# Third-party notices

AvaSchematic builds on the components below. Their licences govern them and are not superseded by
`LICENSE.md`; the licence identifiers here are the ones each package declares in its own metadata.

Nothing in this list restricts commercial use, and none of it requires you to attribute anything in your own
application's user interface.

## Used by the library

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |

That is the whole list. The library is one assembly with one dependency: it draws with Avalonia's own
drawing context and uses no graphics library, no geometry library and no serializer beyond the one in the
framework.

## Used by the demo application only

These are not dependencies of the library. They are here because the demo's source is included and builds
against them.

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia.Desktop](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |
| [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |
| [Avalonia.Fonts.Inter](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |
| [AvaloniaUI.DiagnosticsSupport](https://github.com/AvaloniaUI/Avalonia) | 2.2.3 | MIT |

## Used by the build only

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia.Headless](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |
| [Avalonia.Skia](https://github.com/AvaloniaUI/Avalonia) | 12.1.1 | MIT |
| [xunit.v3](https://github.com/xunit/xunit) | 3.2.2 | Apache-2.0 |
| [System.Reflection.MetadataLoadContext](https://github.com/dotnet/runtime) | 9.0.0 | MIT |

The symbol drawings in `AvaSchematic.Symbols.Libraries` are original work, drawn from the shapes published
in IEC 60617 and ANSI Y32.2. Those standards describe what the symbols mean; the geometry here is ours and
travels under `LICENSE.md` with the rest of the library.
