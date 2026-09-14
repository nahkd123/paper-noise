namespace nahkd123.PaperNoise;

/// <summary>
/// Moving average filter
/// </summary>
public class MovingAverage(uint windowSize = 1)
{
    private readonly double[] buffer = new double[windowSize];
    private int index;

    public double Apply(double x)
    {
        buffer[index] = x;
        index = (index + 1) % buffer.Length;
        return buffer.Average();
    }
}
