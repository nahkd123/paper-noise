namespace nahkd123.PaperNoise;

public class NoiseGenerator
{
    private readonly Random random = new();
    private double y1;
    private double y2;
    private double t = 0;

    public NoiseGenerator()
    {
        y1 = random.NextDouble();
        y2 = random.NextDouble();
    }

    public double Sample(double dt)
    {
        var ya = SineInterp(y1, y2, t);
        var yb = SineInterp(y1, y2, t + dt);
        t += dt;

        if (t >= 1)
        {
            t %= 1;
            y1 = y2;
            y2 = random.NextDouble();
        }

        // not a mistake: this is not the derivative
        return yb - ya;
    }

    private static double SineEasing(double x) => x < 0 ? 0 : x > 1 ? 1 : (Math.Cos((x + 1) * Math.PI) + 1) / 2;
    private static double SineInterp(double a, double b, double t)
    {
        var y = SineEasing(t);
        return a * (1 - y) + b * y;
    }
}
