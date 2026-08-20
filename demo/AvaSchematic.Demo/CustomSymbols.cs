using AvaSchematic.Model;
using AvaSchematic.Symbols;
using AvaSchematic.Symbols.Libraries;

namespace AvaSchematic.Demo;

/// <summary>
/// Two parts this application draws for itself, and the whole of what that takes.
///
/// The point of them is that nothing here is privileged. They are built with the same
/// <see cref="SymbolBuilder"/> the shipped library is built with, registered into the same
/// <see cref="SymbolLibrary"/>, and they appear in the palette beside the eighty that came in the
/// box - placed the same way, wired the same way, saved the same way. A part you need and we did not
/// think of is six lines away.
///
/// There are two because there are two ways to arrive at a symbol. A relay is <em>drawn</em>: you
/// know what it looks like and you say so. A microcontroller is <em>tabulated</em>: nobody draws a
/// forty-pin chip, they copy the pin list out of the datasheet and let something else lay it out.
/// </summary>
public static class CustomSymbols
{
    /// <summary>The palette group these appear under. Any string will do; it is only a heading.</summary>
    public const string Category = "Your own parts";

    /// <summary>Keys, in the same shape as the built-in ones - the group here is the application.</summary>
    public static class Keys
    {
        public static readonly string Relay = "Demo.Relay";
        public static readonly string MCU   = "Demo.MCU";
    }

    /// <summary>Adds both to a library. Call it once, at start-up, before any sheet is built.</summary>
    public static SymbolLibrary AddTo(SymbolLibrary library)
    {
        if (library.Contains(Keys.Relay))
            return library;

        return library.Register(Relay(), Microcontroller());
    }

    /// <summary>
    /// A relay, drawn by hand.
    ///
    /// Everything is in the symbol's own coordinates, around its own origin: the body is a box 40 by
    /// 50 centred on nothing in particular, and the pins stick out where a relay's pins are. The tip
    /// of a pin - the end a wire lands on - is what the coordinates name, so keeping those on a round
    /// number is what keeps every instance on the grid however it is turned.
    /// </summary>
    public static SymbolDefinition Relay() => SymbolBuilder
        .Create(Keys.Relay, "Relay")
        .Category(Category)
        .FixedSize()                                        // a part keeps its pin pitch
        .Rect(-20, -25, 40, 50).NoFill()                    // the body
        .Line(-20, -5, 20, -5)                              // coil above, contacts below
        .Rect(-12, -20, 24, 10).NoFill()                    // the coil
        .Line(-8, 10, 8, 4)                                 // the moving contact
        .Pin("A1", -40, -15, PortSide.Left, 20, name: "COIL+", number: "1")
        .Pin("A2", 40, -15, PortSide.Right, 20, name: "COIL-", number: "2")
        .Pin("COM", -40, 10, PortSide.Left, 20, name: "COM", number: "3")
        .Pin("NO", 40, 4, PortSide.Right, 20, name: "NO", number: "4")
        .Pin("NC", 40, 16, PortSide.Right, 20, name: "NC", number: "5")
        .Label(LabelPlacement.None)
        .Attribute(AttributeKeys.Designator, "K?", 0, -32, true, TextHAlign.Center, TextVAlign.Bottom)
        .Attribute(AttributeKeys.Value, "SRD-05VDC", 0, 32, true, TextHAlign.Center, TextVAlign.Top)
        .Build();

    /// <summary>
    /// A microcontroller, from nothing but its pin list.
    ///
    /// This is the usual way an application turns a datasheet into a usable symbol: the body, the pin
    /// spacing, the numbering and the labels are all worked out from the two lists. A null entry
    /// leaves a gap, which is how a real package's unused legs are drawn.
    /// </summary>
    public static SymbolDefinition Microcontroller() => ElectronicSymbols.CreateIc(
        Keys.MCU, "STM32G031",
        leftPins: new[] { "VDD", "VSS", "NRST", "PA0", "PA1" },
        rightPins: new[] { "PA2", "PA3", "PB0", "SWDIO", "SWCLK" },
        bodyWidth: 120);

    /// <summary>
    /// The source of <see cref="Relay"/>, for the demo to show. Kept beside the thing it describes so
    /// that the two cannot drift - a screenshot of code that no longer compiles is worse than none.
    /// </summary>
    public const string RelaySource = """
        // Six lines of drawing and five of pins. Register it and it is in the palette.
        var relay = SymbolBuilder.Create("Demo.Relay", "Relay")
            .Category("Your own parts")
            .FixedSize()                                     // a part keeps its pin pitch
            .Rect(-20, -25, 40, 50).NoFill()                 // the body
            .Line(-20, -5, 20, -5)                           // coil above, contacts below
            .Rect(-12, -20, 24, 10).NoFill()                 // the coil
            .Line(-8, 10, 8, 4)                              // the moving contact
            .Pin("A1",  -40, -15, PortSide.Left,  20, name: "COIL+", number: "1")
            .Pin("A2",   40, -15, PortSide.Right, 20, name: "COIL-", number: "2")
            .Pin("COM", -40,  10, PortSide.Left,  20, name: "COM",   number: "3")
            .Pin("NO",   40,   4, PortSide.Right, 20, name: "NO",    number: "4")
            .Pin("NC",   40,  16, PortSide.Right, 20, name: "NC",    number: "5")
            .Attribute(AttributeKeys.Designator, "K?", 0, -32)
            .Build();

        library.Register(relay);

        // Nobody draws a forty-pin chip. Hand it the two pin lists instead:
        library.Register(ElectronicSymbols.CreateIc("Demo.MCU", "STM32G031",
            leftPins:  new[] { "VDD", "VSS", "NRST", "PA0", "PA1" },
            rightPins: new[] { "PA2", "PA3", "PB0", "SWDIO", "SWCLK" },
            bodyWidth: 120));

        // From here they are ordinary parts - place, wire, save, load.
        document.AddNodeAtPin("Demo.Relay", "A1", new Point(0, 0));
        """;
}
