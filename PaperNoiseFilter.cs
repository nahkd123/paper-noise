using System.Numerics;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;

namespace nahkd123.PaperNoise;

[PluginName("Paper Noise")]
public class PaperNoiseFilter : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
    [Property("Volume"), DefaultPropertyValue(1), ToolTip("How loud the paper noise is")]
    public double Volume
    {
        get => paper.Volume;
        set => paper.Volume = value;
    }

    [Property("Roughness"), DefaultPropertyValue(1), ToolTip("How rough the paper texture is\nThis affect the \"tone\" of the noise")]
    public double Roughness
    {
        get => paper.Roughness;
        set => paper.Roughness = value;
    }

    [TabletReference]
    public TabletReference Tablet
    {
        set => tabletSpec = value != null ? new()
        {
            PhysicalSize = new()
            {
                X = value.Properties.Specifications.Digitizer.Width / 1000, // convert from mm to meter
                Y = value.Properties.Specifications.Digitizer.Height / 1000,
            },
            LogicalSize = new()
            {
                X = value.Properties.Specifications.Digitizer.MaxX,
                Y = value.Properties.Specifications.Digitizer.MaxY,
            },
            LogicalPressure = value.Properties.Specifications.Pen.MaxPressure
        } : null;
    }

    public PipelinePosition Position => PipelinePosition.PreTransform;
    public event Action<IDeviceReport>? Emit;

    private readonly PaperNoise paper = new();
    private readonly HPETDeltaStopwatch stopwatch = new();
    private TabletSpec? tabletSpec = null;
    private Vector2? lastPosition = null;

    public void Consume(IDeviceReport report)
    {
        if (tabletSpec.HasValue && report is ITabletReport tabletReport && tabletReport.Pressure > 0)
        {
            var currPosition = tabletReport.Position * tabletSpec.Value.PhysicalSize / tabletSpec.Value.LogicalSize;
            var deltaTime = stopwatch.Restart();
            var distance = lastPosition.HasValue ? (currPosition - lastPosition.Value).Length() : 0;
            var pressure = tabletReport.Pressure / tabletSpec.Value.LogicalPressure;
            paper.Velocity = distance / deltaTime.TotalSeconds;
            paper.Pressure = pressure;
            lastPosition = currPosition;
        }
        else if (report is OutOfRangeReport || (report is ITabletReport tabletReport1 && tabletReport1.Pressure == 0))
        {
            stopwatch.Restart();
            lastPosition = null;
            paper.Velocity = 0;
            paper.Pressure = 0;
        }

        Emit?.Invoke(report);
    }

    public void Dispose()
    {
        paper.Dispose();
    }

    private struct TabletSpec
    {
        public Vector2 PhysicalSize;
        public Vector2 LogicalSize;
        public double LogicalPressure;
    }
}
