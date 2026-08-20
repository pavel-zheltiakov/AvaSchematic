using System;
using Avalonia;
using Avalonia.Media;
using AvaSchematic.Model;
using AvaSchematic.Symbols;
using AvaSchematic.Symbols.Libraries;

namespace AvaSchematic.Demo;

/// <summary>
/// Sample documents. They double as API documentation: the same control, the same document type and
/// the same tools produce a mindmap, an architecture drawing, a flowchart and a real schematic -
/// only the symbol library, the route mode and the styling change.
/// </summary>
public static class Samples
{
    private static Color Rgb(uint value)
        => Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);

    // ---- mindmap ------------------------------------------------------------------------------

    public static SchematicDocument CreateMindMap(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Mind map", GridSize = 5 };

        var root = doc.AddNode(BasicShapes.Capsule, new Point(-90, -26), "Schematic control");
        root.Size = new Size(180, 52);
        root.Style.Fill = Rgb(0x2D3A8C);
        root.Style.Foreground = Colors.White;
        root.Style.FontSize = 15;
        root.Style.Bold = true;
        root.Style.Stroke = Rgb(0x1B2560);

        AddBranch(doc, root, "Geometry", Rgb(0x1F7A5A), new Point(-420, -190),
            new[] { "Symbols", "Primitives", "Transforms" });

        AddBranch(doc, root, "Connections", Rgb(0xB2632A), new Point(260, -190),
            new[] { "Orthogonal", "Curved", "Junctions" });

        AddBranch(doc, root, "Editing", Rgb(0x8C2D5A), new Point(-420, 150),
            new[] { "Select", "Wire", "Undo" });

        AddBranch(doc, root, "Domains", Rgb(0x2A5FB2), new Point(260, 150),
            new[] { "Mindmap", "Architecture", "Electronics" });

        return doc;
    }

    private static void AddBranch(SchematicDocument doc, SchematicNode root, string title, Color color,
        Point origin, string[] leaves)
    {
        var branch = doc.AddNode(BasicShapes.RoundedRectangle, origin, title);
        branch.Size = new Size(160, 44);
        branch.Style.Fill = color;
        branch.Style.Foreground = Colors.White;
        branch.Style.FontSize = 13;
        branch.Style.Bold = true;
        branch.Style.Stroke = color;

        Connect(doc, root, branch, color, 2.4);

        // Leaves fan out sideways from the branch and are centred on it, so each connector leaves
        // the branch edge facing its own leaf instead of threading past the others.
        const double leafWidth = 150;
        const double leafHeight = 36;
        const double leafPitch = 56;
        double leafX = origin.X < 0 ? origin.X - leafWidth - 50 : origin.X + 160 + 50;
        double branchCenterY = origin.Y + 22;

        for (int i = 0; i < leaves.Length; i++)
        {
            double leafY = branchCenterY - leafHeight / 2 + (i - (leaves.Length - 1) / 2.0) * leafPitch;
            var leaf = doc.AddNode(BasicShapes.Capsule, new Point(leafX, leafY), leaves[i]);
            leaf.Size = new Size(leafWidth, leafHeight);
            leaf.Style.Fill = Colors.White;
            leaf.Style.Stroke = color;
            leaf.Style.Foreground = Rgb(0x22262E);
            leaf.Style.FontSize = 12;

            Connect(doc, branch, leaf, color, 1.6);
        }
    }

    /// <summary>
    /// Mindmap branches attach to whole shapes rather than to a named port, so the line anchor
    /// slides around the bubble whenever either end is dragged.
    /// </summary>
    private static void Connect(SchematicDocument doc, SchematicNode from, SchematicNode to, Color color, double thickness)
    {
        var connection = new SchematicConnection(
            ConnectionEndpoint.ToShape(from),
            ConnectionEndpoint.ToShape(to))
        {
            Mode = RouteMode.Curved
        };
        connection.Style.Stroke = color;
        connection.Style.StrokeThickness = thickness;
        doc.Add(connection);
    }

    // ---- architecture -------------------------------------------------------------------------

    public static SchematicDocument CreateArchitecture(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Architecture", GridSize = 10 };

        var user = doc.AddNode(ArchitectureSymbols.Actor, new Point(-460, 40), "Customer");
        var web = Service(doc, new Point(-300, 20), "Web app", "React");
        var gateway = Service(doc, new Point(-90, 20), "API gateway", "Envoy");
        var auth = Service(doc, new Point(-90, -140), "Auth service", "OIDC");
        var orders = Service(doc, new Point(150, -60), "Order service", "Go");
        var billing = Service(doc, new Point(150, 110), "Billing service", "Java");

        var queue = doc.AddNode(ArchitectureSymbols.Queue, new Point(400, -40), "orders.events");
        queue.Size = new Size(150, 46);

        var db = doc.AddNode(ArchitectureSymbols.Database, new Point(390, 100), "Postgres");
        db.Size = new Size(110, 100);

        var cache = doc.AddNode(ArchitectureSymbols.Cache, new Point(390, 240), "Redis");
        cache.Size = new Size(110, 100);

        var stripe = doc.AddNode(ArchitectureSymbols.ExternalSystem, new Point(150, 280), "Stripe");
        stripe.Size = new Size(150, 60);

        Flow(doc, user, web, "HTTPS");
        Flow(doc, web, gateway, "REST");
        Flow(doc, gateway, auth, "verify");
        Flow(doc, gateway, orders, "gRPC");
        Flow(doc, gateway, billing, "gRPC");
        Flow(doc, orders, queue, "publish");
        Flow(doc, orders, db, "SQL");
        Flow(doc, billing, db, "SQL");
        Flow(doc, billing, stripe, "charge", dashed: true);
        Flow(doc, orders, cache, "cache", dashed: true);

        var boundary = doc.AddNode(ArchitectureSymbols.Boundary, new Point(-130, -190), "Platform");
        boundary.Size = new Size(700, 500);
        boundary.ZIndex = -10;
        boundary.Style.Stroke = Rgb(0x8A93A6);

        return doc;
    }

    private static SchematicNode Service(SchematicDocument doc, Point position, string title, string stereotype)
    {
        var node = doc.AddNode(ArchitectureSymbols.Service, position, title);
        node.Size = new Size(150, 64);
        node.Style.Fill = Rgb(0xEAF1FB);
        node.Style.Stroke = Rgb(0x3A6DB0);
        node.Style.FontSize = 13;
        node.Style.Bold = true;

        var attribute = node.SetAttribute(AttributeKeys.Stereotype, $"«{stereotype}»");
        attribute.Offset = new Point(50, 52);
        attribute.HorizontalAlignment = TextHAlign.Center;
        attribute.VerticalAlignment = TextVAlign.Center;
        attribute.Style.FontSize = 9;
        attribute.Style.Foreground = Rgb(0x5A6478);
        return node;
    }

    private static void Flow(SchematicDocument doc, SchematicNode from, SchematicNode to, string label,
        bool dashed = false)
    {
        var connection = new SchematicConnection(
            ConnectionEndpoint.ToShape(from),
            ConnectionEndpoint.ToShape(to))
        {
            Mode = RouteMode.Orthogonal,
            EndArrow = ArrowHead.Arrow,
            ArrowSize = 11,
            CornerRadius = 6,
            Label = label
        };
        connection.Style.Stroke = Rgb(0x46506A);
        connection.Style.StrokeThickness = 1.6;
        connection.Style.FontSize = 10;
        if (dashed)
            connection.Style.LineStyle = LineStyle.Dash;
        doc.Add(connection);
    }

    // ---- flowchart ----------------------------------------------------------------------------

    public static SchematicDocument CreateFlowchart(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Flowchart", GridSize = 10 };

        var start = Shape(doc, FlowchartSymbols.Terminator, new Point(-70, -260), "Start", 140, 50);
        var read = Shape(doc, FlowchartSymbols.Data, new Point(-90, -170), "Read order", 180, 60);
        var check = Shape(doc, FlowchartSymbols.Decision, new Point(-100, -60), "In stock?", 200, 90);
        var reserve = Shape(doc, FlowchartSymbols.Process, new Point(-90, 80), "Reserve items", 180, 60);
        var backorder = Shape(doc, FlowchartSymbols.Process, new Point(200, -45), "Create backorder", 180, 60);
        var charge = Shape(doc, FlowchartSymbols.PredefinedProcess, new Point(-90, 190), "Charge card", 180, 60);
        var store = Shape(doc, FlowchartSymbols.Database, new Point(-90, 300), "Persist order", 180, 70);
        var stop = Shape(doc, FlowchartSymbols.Terminator, new Point(-70, 410), "Done", 140, 50);

        Step(doc, start, "S", read, "N");
        Step(doc, read, "S", check, "N");
        Step(doc, check, "S", reserve, "N", "yes");
        Step(doc, check, "E", backorder, "W", "no");
        Step(doc, reserve, "S", charge, "N");
        Step(doc, charge, "S", store, "N");
        Step(doc, store, "S", stop, "N");
        Step(doc, backorder, "S", store, "E");

        return doc;
    }

    private static SchematicNode Shape(SchematicDocument doc, string key, Point position, string text,
        double width, double height)
    {
        var node = doc.AddNode(key, position, text);
        node.Size = new Size(width, height);
        node.Style.Fill = Rgb(0xFFFFFF);
        node.Style.Stroke = Rgb(0x33415C);
        node.Style.FontSize = 12;
        return node;
    }

    private static void Step(SchematicDocument doc, SchematicNode from, string fromPort,
        SchematicNode to, string toPort, string? label = null)
    {
        var connection = doc.Connect(from, fromPort, to, toPort);
        connection.EndArrow = ArrowHead.Arrow;
        connection.ArrowSize = 10;
        connection.CornerRadius = 8;
        connection.Style.Stroke = Rgb(0x33415C);
        if (label is not null)
        {
            connection.Label = label;
            connection.LabelPosition = 0.25;
            connection.Style.FontSize = 10;
        }
    }

    // ---- electronics --------------------------------------------------------------------------

    /// <summary>
    /// A small but real circuit: a decoupled rail, an RC input filter, a non-inverting op-amp stage,
    /// an NPN LED driver and a microcontroller block.
    /// <para>
    /// Every part is placed by pin rather than by bounding box, so all pins land on the 10 unit grid
    /// and every wire is a clean straight run. This is the placement idiom the control is designed
    /// for - see <see cref="SchematicDocument.AddNodeAtPin"/>.
    /// </para>
    /// </summary>
    public static SchematicDocument CreateElectronics(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Amplifier and driver", GridSize = 10 };

        // --- rail decoupling ---------------------------------------------------------------
        var vcc0 = Power(doc, -500, -40, "+5V");
        var c1 = Part(doc, ElectronicSymbols.CapacitorPolarized, "1", -500, 0, "C1", "100u", rotation: 90);
        var gnd0 = Gnd(doc, -500, 80);
        doc.Connect(vcc0, "1", c1, "1");
        doc.Connect(c1, "2", gnd0, "1");

        // --- RC input filter ---------------------------------------------------------------
        var input = doc.AddNodeAtPin(ElectronicSymbols.NetLabel, "1", new Point(-500, 200),
            mirror: MirrorMode.Horizontal);
        input.Value = "IN";

        var r1 = Part(doc, ElectronicSymbols.ResistorIec, "1", -460, 200, "R1", "10k");
        var c2 = Part(doc, ElectronicSymbols.Capacitor, "1", -400, 240, "C2", "1n", rotation: 90);
        var gnd1 = Gnd(doc, -400, 320);

        doc.Connect(input, "1", r1, "1");
        doc.Connect(r1, "2", c2, "1");
        doc.Connect(c2, "2", gnd1, "1");

        // --- non-inverting amplifier -------------------------------------------------------
        var opamp = doc.AddNodeAtPin(ElectronicSymbols.OpAmp, "IN+", new Point(-300, 200));
        opamp.Designator = "U1A";
        opamp.Value = "TL072";

        var r2 = Part(doc, ElectronicSymbols.ResistorIec, "2", -210, 80, "R2", "10k");
        var r3 = Part(doc, ElectronicSymbols.ResistorIec, "1", -340, 220, "R3", "10k", rotation: 90);
        var gnd2 = Gnd(doc, -340, 300);
        var vcc1 = Power(doc, -270, 130, "+5V");
        var gnd3 = Gnd(doc, -270, 260);

        doc.Connect(r1, "2", opamp, "IN+");
        doc.Connect(opamp, "OUT", r2, "2");     // feedback out and over the top
        doc.Connect(r2, "1", opamp, "IN-");
        doc.Connect(opamp, "IN-", r3, "1");     // gain leg down to ground
        doc.Connect(r3, "2", gnd2, "1");
        doc.Connect(vcc1, "1", opamp, "V+");
        doc.Connect(opamp, "V-", gnd3, "1");

        // --- LED driver --------------------------------------------------------------------
        var r4 = Part(doc, ElectronicSymbols.ResistorIec, "1", -180, 190, "R4", "4k7");
        var q1 = Part(doc, ElectronicSymbols.NpnTransistor, "B", -90, 190, "Q1", "BC547");
        var r5 = Part(doc, ElectronicSymbols.ResistorIec, "2", -60, 100, "R5", "330", rotation: 90);
        var led = Part(doc, ElectronicSymbols.Led, "2", -60, 20, "D1", "GREEN", rotation: 90);
        var vcc2 = Power(doc, -60, -70, "+5V");
        var gnd4 = Gnd(doc, -60, 260);

        doc.Connect(opamp, "OUT", r4, "1");
        doc.Connect(r4, "2", q1, "B");
        doc.Connect(q1, "C", r5, "2");
        doc.Connect(r5, "1", led, "2");
        doc.Connect(led, "1", vcc2, "1");
        doc.Connect(q1, "E", gnd4, "1");

        // --- microcontroller ---------------------------------------------------------------
        // A part symbol built at runtime from nothing but a pin list - the usual way an
        // application turns a datasheet into a usable schematic symbol.
        var mcuSymbol = ElectronicSymbols.CreateIc("el/mcu-demo", "STM32G031",
            new[] { "VDD", "VSS", "NRST", "PA0", "PA1" },
            new[] { "PA2", "PA3", "PB0", "SWDIO", "SWCLK" },
            120);
        library.Register(mcuSymbol);

        var mcu = doc.AddNodeAtPin("el/mcu-demo", "L0", new Point(100, 120));
        mcu.Designator = "U2";

        var vcc3 = Power(doc, 60, 60, "+3V3");
        var gnd5 = Gnd(doc, 60, 240);
        doc.Connect(vcc3, "1", mcu, "L0");
        doc.Connect(mcu, "L1", gnd5, "1");

        var pwm = doc.AddNodeAtPin(ElectronicSymbols.NetLabel, "1", new Point(320, 120));
        pwm.Value = "PWM";
        doc.Connect(mcu, "R0", pwm, "1");

        return doc;
    }

    /// <summary>Places a two- or three-pin part by one of its pins and fills in its fields.</summary>
    private static SchematicNode Part(SchematicDocument doc, string symbolKey, string pinKey,
        double x, double y, string designator, string value, double rotation = 0)
    {
        var node = doc.AddNodeAtPin(symbolKey, pinKey, new Point(x, y), rotation);
        node.Designator = designator;
        if (!string.IsNullOrEmpty(value))
            node.Value = value;

        if (Math.Abs(rotation % 180) > 1)
            MoveFieldsBeside(node);

        return node;
    }

    /// <summary>
    /// Puts the designator and value to the right of a vertically mounted part. Field offsets live
    /// in symbol definition space, so for a part turned 90 degrees the world "right" direction is
    /// definition "-y" - which is why the offsets below look transposed.
    /// </summary>
    private static void MoveFieldsBeside(SchematicNode node)
    {
        if (node.GetAttribute(AttributeKeys.Designator) is { } designator)
        {
            designator.Offset = new Point(-14, -28);
            designator.HorizontalAlignment = TextHAlign.Left;
            designator.VerticalAlignment = TextVAlign.Center;
        }
        if (node.GetAttribute(AttributeKeys.Value) is { } value)
        {
            value.Offset = new Point(10, -28);
            value.HorizontalAlignment = TextHAlign.Left;
            value.VerticalAlignment = TextVAlign.Center;
        }
    }

    private static SchematicNode Power(SchematicDocument doc, double x, double y, string rail)
    {
        var node = doc.AddNodeAtPin(ElectronicSymbols.PowerRail, "1", new Point(x, y));
        node.Value = rail;
        return node;
    }

    private static SchematicNode Gnd(SchematicDocument doc, double x, double y, double rotation = 0)
        => doc.AddNodeAtPin(ElectronicSymbols.Ground, "1", new Point(x, y), rotation);

    // ---- UML class diagram --------------------------------------------------------------------

    public static SchematicDocument CreateUml(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Class diagram", GridSize = 10 };

        var item = ClassBox(doc, new Point(-260, -180), "SchematicItem",
            "+ Id : Guid\n+ Layer : string\n+ ZIndex : int",
            "+ GetBounds()\n+ Translate(delta)");

        var node = ClassBox(doc, new Point(-460, 40), "SchematicNode",
            "+ Symbol\n+ Position\n+ Ports\n+ Attributes",
            "+ GetTransform()\n+ FindPort(key)");

        var connection = ClassBox(doc, new Point(-200, 40), "SchematicConnection",
            "+ Start : Endpoint\n+ End : Endpoint\n+ Waypoints",
            "+ GetRoute()");

        var junction = ClassBox(doc, new Point(60, 40), "SchematicJunction",
            "+ Position\n+ Diameter", "");

        var port = ClassBox(doc, new Point(-460, 300), "Port",
            "+ Key : string\n+ Side\n+ Kind", "+ WorldPosition");

        Generalization(doc, node, item);
        Generalization(doc, connection, item);
        Generalization(doc, junction, item);
        Composition(doc, node, port, "1", "0..*");
        Association(doc, connection, port, "2", "");

        return doc;
    }

    private static SchematicNode ClassBox(SchematicDocument doc, Point position, string name,
        string fields, string methods)
    {
        var node = doc.AddNode(ArchitectureSymbols.ClassBox, position);
        node.Size = new Size(200, 130);
        node.Style.Fill = Rgb(0xFFFDF3);
        node.Style.Stroke = Rgb(0x4A4335);
        node.SetAttribute("Name", name).Style.Bold = true;
        node.SetAttribute("Fields", fields);
        node.SetAttribute("Methods", methods);

        var nameAttribute = node.GetAttribute("Name")!;
        nameAttribute.Style.FontSize = 13;
        foreach (var key in new[] { "Fields", "Methods" })
        {
            var attribute = node.GetAttribute(key)!;
            attribute.Style.FontSize = 9;
        }
        return node;
    }

    private static SchematicConnection Relation(SchematicDocument doc, SchematicNode from, SchematicNode to)
    {
        var connection = new SchematicConnection(
            ConnectionEndpoint.ToShape(from),
            ConnectionEndpoint.ToShape(to))
        {
            Mode = RouteMode.Orthogonal,
            ArrowSize = 13
        };
        connection.Style.Stroke = Rgb(0x4A4335);
        connection.Style.FontSize = 10;
        doc.Add(connection);
        return connection;
    }

    private static void Generalization(SchematicDocument doc, SchematicNode child, SchematicNode parent)
        => Relation(doc, child, parent).EndArrow = ArrowHead.Triangle;

    private static void Composition(SchematicDocument doc, SchematicNode whole, SchematicNode part,
        string wholeLabel, string partLabel)
    {
        var connection = Relation(doc, whole, part);
        connection.StartArrow = ArrowHead.Diamond;
        connection.StartLabel = wholeLabel;
        connection.EndLabel = partLabel;
    }

    private static void Association(SchematicDocument doc, SchematicNode a, SchematicNode b,
        string aLabel, string bLabel)
    {
        var connection = Relation(doc, a, b);
        connection.EndArrow = ArrowHead.OpenArrow;
        connection.Style.LineStyle = LineStyle.Dash;
        connection.StartLabel = aLabel;
        connection.EndLabel = bLabel;
    }
    // ---- wiring rules -------------------------------------------------------------------------

    /// <summary>
    /// A worked example of the drawing conventions, one case per row. Everything here comes out of
    /// the defaults - nothing on this sheet sets a rule - so it doubles as a check that a fresh
    /// document already behaves the way a schematic is supposed to.
    /// </summary>
    public static SchematicDocument CreateWiringRules(SymbolLibrary library)
    {
        var doc = new SchematicDocument(library) { Name = "Wiring rules", GridSize = 10 };

        double crossing = Case(doc, 0, "Crossing: no dot", "the lines are not joined");
        Wire(doc, new Point(60, crossing), new Point(260, crossing));
        Wire(doc, new Point(160, crossing - 25), new Point(160, crossing + 25));

        double tap = Case(doc, 1, "Tap: dot", "one line ends on the other");
        Wire(doc, new Point(60, tap), new Point(260, tap));
        Wire(doc, new Point(160, tap), new Point(160, tap + 50));

        double corner = Case(doc, 2, "Corner: no dot", "two ends are only a bend");
        Wire(doc, new Point(60, corner), new Point(160, corner));
        Wire(doc, new Point(160, corner), new Point(160, corner + 50));

        double star = Case(doc, 3, "Four ways: dot", "three or four branches join");
        var centre = new Point(160, star);
        Wire(doc, centre, new Point(60, star));
        Wire(doc, centre, new Point(260, star));
        Wire(doc, centre, new Point(160, star - 30));
        Wire(doc, centre, new Point(160, star + 30));

        // Four links that would all take the same elbow and share one vertical run. Each is given a
        // lane of its own instead, which is the rule the router enforces on every sheet.
        Label(doc, new Point(430, 15), "No parallel runs",
            "four links that would share one vertical run, each given its own lane");
        for (int i = 0; i < 4; i++)
        {
            var source = doc.AddNodeAtPin(ElectronicSymbols.ResistorIec, "1", new Point(430, 120 + i * 60));
            source.Designator = "R" + (i + 1);
            var target = doc.AddNodeAtPin(ElectronicSymbols.ResistorIec, "1", new Point(820, 400 + i * 60));
            target.Designator = "R" + (i + 5);

            var link = doc.Connect(source, "2", target, "1");
            link.Style.Stroke = Rgb(i switch
            {
                0 => 0x2563EB,
                1 => 0x059669,
                2 => 0xD97706,
                _ => 0xDC2626
            });
        }

        // Two wires arriving at one pin. Three things touch, but only two lines leave the point, so
        // there is nothing for a dot to disambiguate.
        double pin = Case(doc, 4, "Two wires on a pin: no dot", "a pin is a terminal, not a branch");
        var terminal = doc.AddNodeAtPin(ElectronicSymbols.ResistorIec, "1", new Point(160, pin), rotation: 90);
        terminal.Designator = "R9";
        Wire(doc, new Point(60, pin), new Point(160, pin));
        Wire(doc, new Point(260, pin), new Point(160, pin));

        // A part standing in the way of a link it has nothing to do with: the line goes round it.
        Label(doc, new Point(430, 660), "Lines go round parts",
            "a link never crosses a symbol it is not wired to");
        var source10 = doc.AddNodeAtPin(ElectronicSymbols.ResistorIec, "1", new Point(430, 760));
        source10.Designator = "R10";
        var target10 = doc.AddNodeAtPin(ElectronicSymbols.ResistorIec, "1", new Point(830, 760));
        target10.Designator = "R11";
        var blocker = doc.AddNode(ElectronicSymbols.NpnTransistor, new Point(610, 725));
        blocker.Designator = "Q1";
        doc.Connect(source10, "2", target10, "1");

        return doc;
    }

    /// <summary>Labels one row of the rule sheet and returns the y its wires should sit on.</summary>
    private static double Case(SchematicDocument doc, int row, string title, string subtitle)
    {
        double y = 90 + row * 170;
        Label(doc, new Point(60, y - 75), title, subtitle);
        return y;
    }

    private static void Label(SchematicDocument doc, Point at, string title, string subtitle)
    {
        var node = doc.AddNode(BasicShapes.TextOnly, at, title);
        node.Size = new Size(260, 18);
        node.Style.FontSize = 13;
        node.Style.Bold = true;

        var note = doc.AddNode(BasicShapes.TextOnly, new Point(at.X, at.Y + 18), subtitle);
        note.Size = new Size(300, 16);
        note.Style.FontSize = 11;
        note.Style.Foreground = Rgb(0x6B7280);
    }

    private static SchematicConnection Wire(SchematicDocument doc, Point from, Point to)
    {
        var wire = new SchematicConnection(ConnectionEndpoint.At(from), ConnectionEndpoint.At(to))
        {
            Mode = RouteMode.Orthogonal
        };
        doc.Add(wire);
        return wire;
    }
}
