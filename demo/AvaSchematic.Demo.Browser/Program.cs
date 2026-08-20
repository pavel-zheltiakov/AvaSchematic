using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Logging;

namespace AvaSchematic.Demo.Browser;

/// <summary>The browser head: the same application, in a tab.</summary>
[SupportedOSPlatform("browser")]
internal static class Program
{
    private static async Task Main(string[] args)
    {
        await BuildAvaloniaApp().StartBrowserAppAsync("out");

        // And then never return.
        //
        // StartBrowserAppAsync completes once the application is up, so a Main that simply awaited it
        // would fall off the end - and falling off the end of Main is how a .NET WebAssembly process asks
        // the runtime to exit. The runtime obliges, and the tab keeps a page that is never drawn again.
        await Task.Delay(Timeout.Infinite);
    }

    /// <summary>
    /// The application, with Avalonia's own diagnostics turned on.
    ///
    /// LogToTrace is not decoration here. A browser tab has no debugger attached and no terminal, so an
    /// exception thrown inside layout or render - which Avalonia logs and swallows rather than letting
    /// escape - would leave no trace at all. Routing the log to Trace puts those messages in the devtools
    /// console, which is the only place anyone can read them from.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .LogToTrace(LogEventLevel.Warning);
}
