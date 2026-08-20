using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaSchematic.Connectivity;
using AvaSchematic.Controls;
using AvaSchematic.Interaction;
using AvaSchematic.Model;
using AvaSchematic.Rendering;
using AvaSchematic.Serialization;
using AvaSchematic.Symbols;

namespace AvaSchematic.Demo;

public partial class MainWindow : Window
{
    private readonly SymbolLibrary _library = SymbolLibrary.CreateDefault();
    private bool _syncingInspector;
    private string[] _paletteCategories = Array.Empty<string>();

    public MainWindow()
    {
        InitializeComponent();
        WireUp();
        LoadSample(0);
    }

    // ---- wiring -----------------------------------------------------------------------------

    private void WireUp()
    {
        SampleBox.SelectionChanged += (_, _) => LoadSample(SampleBox.SelectedIndex);
        ThemeBox.SelectionChanged += (_, _) => ApplyTheme();
        RouteBox.SelectionChanged += (_, _) => ApplyRouteMode();

        SelectToolButton.Click += (_, _) => SetTool(new SelectTool());
        WireToolButton.Click += (_, _) => SetTool(CreateWireTool());
        PanToolButton.Click += (_, _) => SetTool(new PanTool());

        UndoButton.Click += (_, _) => { View.UndoStack.Undo(); View.InvalidateVisual(); UpdateStatus(); };
        RedoButton.Click += (_, _) => { View.UndoStack.Redo(); View.InvalidateVisual(); UpdateStatus(); };
        DeleteButton.Click += (_, _) => { View.DeleteSelection(); UpdateStatus(); };
        RotateButton.Click += (_, _) => View.RotateSelection();
        Rotate45Button.Click += (_, _) => View.RotateSelection(45);
        MirrorButton.Click += (_, _) => View.MirrorSelection();

        FitButton.Click += (_, _) => { View.ZoomToFit(); UpdateStatus(); };
        ZoomInButton.Click += (_, _) => { View.ZoomIn(); UpdateStatus(); };
        ZoomOutButton.Click += (_, _) => { View.ZoomOut(); UpdateStatus(); };

        GridButton.IsCheckedChanged += (_, _) => View.ShowGrid = GridButton.IsChecked == true;
        SnapButton.IsCheckedChanged += (_, _) => View.SnapToGrid = SnapButton.IsChecked == true;

        NetlistButton.Click += (_, _) => ShowNetlist();
        JsonButton.Click += (_, _) => RoundTripJson();

        PaletteFilter.TextChanged += (_, _) => BuildPalette();

        View.SelectionChanged += (_, _) => { UpdateInspector(); UpdateStatus(); };
        View.ToolChanged += (_, _) => UpdateToolButtons();
        View.Viewport.PropertyChanged += (_, _) => UpdateStatus();
        View.ItemDoubleTapped += (_, e) => UpdateInspector();

        TextField.TextChanged += (_, _) => ApplyInspector();
        DesignatorField.TextChanged += (_, _) => ApplyInspector();
        ValueField.TextChanged += (_, _) => ApplyInspector();
        LabelField.TextChanged += (_, _) => ApplyInspector();
        RotationField.ValueChanged += (_, _) => ApplyInspector();

        ArrowBox.ItemsSource = Enum.GetValues<ArrowHead>();
        LineStyleBox.ItemsSource = Enum.GetValues<LineStyle>();
        ArrowBox.SelectionChanged += (_, _) => ApplyInspector();
        LineStyleBox.SelectionChanged += (_, _) => ApplyInspector();

        AvoidOverlapCheck.IsCheckedChanged += (_, _) => ApplyRules();
        AvoidSymbolsCheck.IsCheckedChanged += (_, _) => ApplyRules();
        PinBranchCheck.IsCheckedChanged += (_, _) => ApplyRules();
        RotationStepField.ValueChanged += (_, _) => ApplyRules();
        TeeJunctionCheck.IsCheckedChanged += (_, _) => ApplyRules();
        CrossingJunctionCheck.IsCheckedChanged += (_, _) => ApplyRules();
        SegmentDragCheck.IsCheckedChanged += (_, _) => ApplyRules();
        JunctionBranchesField.ValueChanged += (_, _) => ApplyRules();
        CheckRulesButton.Click += (_, _) => ShowRuleReport();
    }

    // ---- drawing rules ------------------------------------------------------------------------

    /// <summary>
    /// Pushes the checkboxes onto the document. Everything here starts at the default a schematic
    /// expects; the panel exists to show that each convention really is a switch.
    /// </summary>
    private void ApplyRules()
    {
        if (View.Document is not { } document || _syncingInspector)
            return;

        var rules = document.Rules;
        rules.AvoidParallelOverlap = AvoidOverlapCheck.IsChecked == true;
        rules.AvoidSymbols = AvoidSymbolsCheck.IsChecked == true;
        rules.CountPinsAsBranches = PinBranchCheck.IsChecked == true;
        rules.RotationStep = (double)(RotationStepField.Value ?? 90);
        RotationField.Increment = (decimal)rules.RotationStep;
        rules.JunctionOnTee = TeeJunctionCheck.IsChecked == true;
        rules.JunctionOnCrossing = CrossingJunctionCheck.IsChecked == true;
        rules.AllowSegmentDrag = SegmentDragCheck.IsChecked == true;
        rules.JunctionMinBranches = (int)(JunctionBranchesField.Value ?? 3);

        View.InvalidateVisual();
        UpdateStatus();
    }

    private void SyncRulesPanel()
    {
        if (View.Document is not { } document)
            return;

        bool previous = _syncingInspector;
        _syncingInspector = true;
        var rules = document.Rules;
        AvoidOverlapCheck.IsChecked = rules.AvoidParallelOverlap;
        AvoidSymbolsCheck.IsChecked = rules.AvoidSymbols;
        PinBranchCheck.IsChecked = rules.CountPinsAsBranches;
        RotationStepField.Value = (decimal)rules.RotationStep;
        RotationField.Increment = (decimal)rules.RotationStep;
        TeeJunctionCheck.IsChecked = rules.JunctionOnTee;
        CrossingJunctionCheck.IsChecked = rules.JunctionOnCrossing;
        SegmentDragCheck.IsChecked = rules.AllowSegmentDrag;
        JunctionBranchesField.Value = rules.JunctionMinBranches;
        _syncingInspector = previous;
    }

    private void ShowRuleReport()
    {
        if (View.Document is not { } document)
            return;

        var violations = new RuleChecker().Check(document);
        var junctions = new JunctionResolver().Resolve(document);

        var text = new StringBuilder();
        text.AppendLine($"{junctions.Count} junction dot(s)");
        foreach (var junction in junctions)
            text.AppendLine($"  ({junction.Position.X:0.#}, {junction.Position.Y:0.#})  " +
                            $"{junction.Branches} branches{(junction.IsTee ? "  tap" : string.Empty)}");

        text.AppendLine();
        text.AppendLine(violations.Count == 0
            ? "No rule violations."
            : $"{violations.Count} rule violation(s):");
        foreach (var violation in violations)
            text.AppendLine("  " + violation.Message);

        ReportBox.Text = text.ToString();
        StatusText.Text = violations.Count == 0
            ? "Rules satisfied"
            : $"{violations.Count} rule violation(s)";
    }

    private WireTool CreateWireTool()
    {
        var tool = new WireTool();
        // Diagrams get an arrow head, schematics do not - a wire is not directed.
        if (SampleBox.SelectedIndex is 1 or 2 or 3)
            tool.EndArrow = ArrowHead.Arrow;
        return tool;
    }

    private void SetTool(ITool tool)
    {
        View.ActiveTool = tool;
        View.Focus();
        UpdateToolButtons();
    }

    private void UpdateToolButtons()
    {
        string name = View.ActiveTool?.Name ?? "select";
        SelectToolButton.IsChecked = name == "select";
        WireToolButton.IsChecked = name == "wire";
        PanToolButton.IsChecked = name == "pan";
    }

    // ---- samples ----------------------------------------------------------------------------

    private void LoadSample(int index)
    {
        var document = index switch
        {
            0 => Samples.CreateMindMap(_library),
            1 => Samples.CreateArchitecture(_library),
            2 => Samples.CreateFlowchart(_library),
            3 => Samples.CreateUml(_library),
            4 => Samples.CreateElectronics(_library),
            5 => Samples.CreateWiringRules(_library),
            _ => new SchematicDocument(_library) { Name = "Empty", GridSize = 10 }
        };

        bool electronics = index is 4 or 5;

        // The renderer preset is the only switch between "diagram" and "schematic" behaviour.
        var options = electronics
            ? SchematicRenderOptions.ForElectronics()
            : SchematicRenderOptions.ForDiagrams();
        CopyOptions(options, View.RenderOptions);

        View.Palette.GridStyle = electronics ? GridStyle.Dots : GridStyle.Lines;
        View.DefaultRouteMode = index switch
        {
            0 => RouteMode.Curved,
            _ => RouteMode.Orthogonal
        };
        RouteBox.SelectedIndex = View.DefaultRouteMode switch
        {
            RouteMode.Direct => 1,
            RouteMode.Curved => 2,
            _ => 0
        };

        _paletteCategories = index switch
        {
            0 => new[] { "Basic" },
            1 => new[] { "Architecture", "Basic" },
            2 => new[] { "Flowchart", "Basic" },
            3 => new[] { "Architecture", "Basic" },
            4 or 5 => new[] { "Passive", "Discrete", "Analog", "Logic", "Power", "Sources", "Annotation", "Connectors", "Integrated circuits", "Electromechanical", "RF" },
            _ => Array.Empty<string>()
        };

        View.Document = document;
        SyncRulesPanel();
        SetTool(new SelectTool());
        BuildPalette();

        // Fitting needs a laid out control, so defer one frame.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            View.ZoomToFit();
            UpdateStatus();
        }, Avalonia.Threading.DispatcherPriority.Background);

        ReportBox.Text = string.Empty;
        UpdateInspector();
    }

    private static void CopyOptions(SchematicRenderOptions from, SchematicRenderOptions to)
    {
        to.ShowPorts = from.ShowPorts;
        to.ShowUnconnectedPins = from.ShowUnconnectedPins;
        to.ShowPinNames = from.ShowPinNames;
        to.ShowPinNumbers = from.ShowPinNumbers;
        to.ShowAutoJunctions = from.ShowAutoJunctions;
        to.ShowAttributes = from.ShowAttributes;
        to.ShowLabels = from.ShowLabels;
        to.ShowJunctions = from.ShowJunctions;
    }

    private void ApplyTheme()
    {
        var theme = ThemeBox.SelectedIndex switch
        {
            1 => SchematicTheme.CreateDark(),
            2 => SchematicTheme.CreateBlueprint(),
            _ => SchematicTheme.CreateLight()
        };
        theme.GridStyle = View.Palette.GridStyle;
        View.Palette = theme;
    }

    private void ApplyRouteMode()
    {
        var mode = RouteBox.SelectedIndex switch
        {
            1 => RouteMode.Direct,
            2 => RouteMode.Curved,
            _ => RouteMode.Orthogonal
        };
        View.DefaultRouteMode = mode;
        if (View.Selection.OfType<SchematicConnection>().Any())
            View.SetRouteMode(mode);
    }

    // ---- palette ----------------------------------------------------------------------------

    private void BuildPalette()
    {
        PalettePanel.Children.Clear();
        string filter = PaletteFilter.Text?.Trim() ?? string.Empty;

        var groups = _library.ByCategory()
            .Where(g => _paletteCategories.Length == 0 || _paletteCategories.Contains(g.Key))
            .OrderBy(g => Array.IndexOf(_paletteCategories, g.Key));

        foreach (var group in groups)
        {
            var matching = group
                .Where(d => filter.Length == 0 ||
                            d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                            d.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count == 0)
                continue;

            PalettePanel.Children.Add(new TextBlock
            {
                Text = group.Key.ToUpperInvariant(),
                Classes = { "section" }
            });

            foreach (var definition in matching)
            {
                var button = new Button
                {
                    Content = definition.Name,
                    Classes = { "palette" },
                    Tag = definition
                };
                button.Click += OnPaletteClick;
                PalettePanel.Children.Add(button);
            }
        }
    }

    private void OnPaletteClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SymbolDefinition definition })
            return;

        var tool = new PlaceTool(definition,
            definition.LabelPlacement == LabelPlacement.None ? null : definition.Name)
        {
            Continuous = true
        };
        SetTool(tool);
        StatusText.Text = $"Placing {definition.Name} — click to drop, R rotates, Esc cancels";
    }

    // ---- inspector --------------------------------------------------------------------------

    private void UpdateInspector()
    {
        _syncingInspector = true;
        try
        {
            var item = View.Selection.Count == 1 ? View.Selection[0] : null;
            InspectorContent.IsVisible = item is not null;
            InspectorEmpty.IsVisible = item is null;

            if (item is null)
                return;

            InspectorKind.Text = item switch
            {
                SchematicNode n => $"Node · {n.Symbol?.Name ?? n.SymbolKey}",
                SchematicConnection => "Connection",
                SchematicJunction => "Junction",
                _ => item.GetType().Name
            };

            bool isNode = item is SchematicNode;
            bool isConnection = item is SchematicConnection;

            TextField.IsEnabled = isNode;
            DesignatorField.IsEnabled = isNode;
            ValueField.IsEnabled = isNode;
            RotationField.IsEnabled = isNode;
            LabelField.IsEnabled = isConnection;
            ArrowBox.IsEnabled = isConnection;
            LineStyleBox.IsEnabled = true;

            if (item is SchematicNode node)
            {
                TextField.Text = node.Text;
                DesignatorField.Text = node.Designator ?? string.Empty;
                ValueField.Text = node.Value ?? string.Empty;
                RotationField.Value = (decimal)node.Rotation;
                LabelField.Text = string.Empty;
                LineStyleBox.SelectedItem = node.StyleOrNull?.LineStyle ?? LineStyle.Solid;
            }
            else if (item is SchematicConnection connection)
            {
                TextField.Text = string.Empty;
                DesignatorField.Text = string.Empty;
                ValueField.Text = string.Empty;
                LabelField.Text = connection.Label;
                ArrowBox.SelectedItem = connection.EndArrow;
                LineStyleBox.SelectedItem = connection.StyleOrNull?.LineStyle ?? LineStyle.Solid;
            }
        }
        finally
        {
            _syncingInspector = false;
        }
    }

    private void ApplyInspector()
    {
        if (_syncingInspector || View.Selection.Count != 1)
            return;

        switch (View.Selection[0])
        {
            case SchematicNode node:
                node.Text = TextField.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(DesignatorField.Text) || node.GetAttribute(AttributeKeys.Designator) is not null)
                    node.Designator = DesignatorField.Text ?? string.Empty;
                if (!string.IsNullOrEmpty(ValueField.Text) || node.GetAttribute(AttributeKeys.Value) is not null)
                    node.Value = ValueField.Text ?? string.Empty;
                // Held to the same step as the rotate button and the handle: the field is a way to
                // type an angle, not a way round the rule. Lower "Rotation step" to allow finer ones.
                if (RotationField.Value is { } rotation)
                    node.Rotation = View.Document?.Rules.SnapRotation((double)rotation) ?? (double)rotation;
                if (LineStyleBox.SelectedItem is LineStyle nodeLineStyle)
                    node.Style.LineStyle = nodeLineStyle;
                break;

            case SchematicConnection connection:
                connection.Label = LabelField.Text ?? string.Empty;
                if (ArrowBox.SelectedItem is ArrowHead arrow)
                    connection.EndArrow = arrow;
                if (LineStyleBox.SelectedItem is LineStyle lineStyle)
                    connection.Style.LineStyle = lineStyle;
                break;
        }

        View.InvalidateVisual();
    }

    // ---- reports ----------------------------------------------------------------------------

    private void ShowNetlist()
    {
        if (View.Document is null)
            return;

        var netlist = new NetlistExtractor().Extract(View.Document);
        var text = new StringBuilder();
        text.AppendLine($"{netlist.Nets.Count} nets");
        text.AppendLine();
        text.Append(netlist.ToText());

        if (netlist.UnconnectedTerminals.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Unconnected pins ({netlist.UnconnectedTerminals.Count}):");
            foreach (var terminal in netlist.UnconnectedTerminals.Take(40))
                text.AppendLine("  " + terminal.Reference);
        }

        ReportBox.Text = text.ToString();
        StatusText.Text = $"Extracted {netlist.Nets.Count} nets";
    }

    /// <summary>Saves the document to JSON and loads it straight back, proving the format round-trips.</summary>
    private void RoundTripJson()
    {
        if (View.Document is null)
            return;

        var json = SchematicJson.Save(View.Document, new SchematicJson.SaveOptions { EmbedSymbols = true });
        var reloaded = SchematicJson.Load(json, _library);

        View.Document = reloaded;
        ReportBox.Text = json.Length > 20000 ? json.Substring(0, 20000) + "\n…" : json;
        StatusText.Text = $"Round-tripped {json.Length:N0} bytes · {reloaded.Items.Count} items";
        View.ZoomToFit();
    }

    // ---- status -----------------------------------------------------------------------------

    private void UpdateStatus()
    {
        ZoomText.Text = $"{View.Viewport.Zoom * 100:0}%";
        int count = View.Selection.Count;
        StatusText.Text = count switch
        {
            0 => View.Document is null ? "Ready" : $"{View.Document.Items.Count} items",
            1 => Describe(View.Selection[0]),
            _ => $"{count} items selected"
        };
    }

    private static string Describe(SchematicItem item) => item switch
    {
        SchematicNode node => string.IsNullOrEmpty(node.Text)
            ? $"{node.Symbol?.Name ?? node.SymbolKey} {node.Designator}".Trim()
            : $"{node.Symbol?.Name ?? node.SymbolKey} · {node.Text}",
        SchematicConnection connection => $"Connection · {connection.Mode}" +
                                          (string.IsNullOrEmpty(connection.Label) ? "" : $" · {connection.Label}"),
        SchematicJunction => "Junction",
        _ => item.GetType().Name
    };
}
