using PortAudioSharp;
using Stream = PortAudioSharp.Stream;

namespace nahkd123.PaperNoise;

public class PaperNoise : IDisposable
{
    public const double SampleRate = 48000;
    public const uint FramesPerBuffer = 64;

    public double Volume { get; set; } = 1;
    public double Roughness { get; set; } = 1;
    public double Velocity { get; set; } = 0;
    public double Pressure { get; set; } = 0;

    private static bool initializedPortAudio = false;
    private readonly Stream audio;
    private readonly NoiseGenerator generator = new();
    private readonly MovingAverage velocityFilter = new((uint)(SampleRate * 0.1));
    private readonly MovingAverage pressureFilter = new((uint)(SampleRate * 0.1));

    public PaperNoise()
    {
        if (!initializedPortAudio)
        {
            PortAudio.LoadNativeLibrary();
            PortAudio.Initialize();
            initializedPortAudio = true;
        }

        if (PortAudio.DefaultOutputDevice == PortAudio.NoDevice)
        {
            throw new Exception("No audio output device");
        }

        audio = new(
            inParams: null,
            outParams: new()
            {
                device = PortAudio.DefaultOutputDevice,
                channelCount = 2,
                sampleFormat = SampleFormat.Float32,
                suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice).defaultLowOutputLatency,
                hostApiSpecificStreamInfo = nint.Zero
            },
            sampleRate: SampleRate,
            framesPerBuffer: FramesPerBuffer,
            streamFlags: StreamFlags.ClipOff,
            callback: Process,
            userData: null
        );

        audio.Start();
    }

    private StreamCallbackResult Process(nint input, nint output, uint frameCount, ref StreamCallbackTimeInfo timeInfo, StreamCallbackFlags flags, nint userdata)
    {
        unsafe
        {
            float* data = (float*)output;

            for (uint i = 0; i < frameCount; i++)
            {
                double velocity = Math.Clamp(velocityFilter.Apply(Velocity), 0, 1);
                double pressure = pressureFilter.Apply(Pressure);
                float sampled = (float)(generator.Sample((0.5 + velocity * 0.5) * Roughness) * velocity * pressure * Volume);
                *data++ = sampled;
                *data++ = sampled;
            }
        }

        return StreamCallbackResult.Continue;
    }

    public void Dispose()
    {
        audio.Stop();
        audio.Close();
        audio.Dispose();
    }
}
