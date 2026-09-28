using System.IO;
using System.Media;

namespace VibeClock.Audio;

/// <summary>A quiet, single struck bell, synthesized once as PCM. No external sound files.</summary>
public sealed class FinishSound : IDisposable
{
    private readonly MemoryStream stream;
    private readonly SoundPlayer player;

    public FinishSound()
    {
        const int sampleRate = 22050;
        const int samples = (int)(sampleRate * 0.85);
        stream = new MemoryStream(44 + samples * 2);
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8); writer.Write(36 + samples * 2); writer.Write("WAVEfmt "u8);
            writer.Write(16); writer.Write((short)1); writer.Write((short)1);
            writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write("data"u8); writer.Write(samples * 2);
            for (int i = 0; i < samples; i++)
            {
                double t = (double)i / sampleRate;
                double envelope = Math.Min(1, t / 0.006) * Math.Exp(-6 * t) * Math.Min(1, (0.85 - t) / 0.035);
                double wave = Math.Sin(2 * Math.PI * 880 * t) + 0.32 * Math.Sin(2 * Math.PI * 1760 * t) + 0.12 * Math.Sin(2 * Math.PI * 2640 * t);
                writer.Write((short)(wave * envelope * 8500));
            }
        }
        stream.Position = 0;
        player = new SoundPlayer(stream);
        player.Load();
    }

    public void Play()
    {
        try { player.Play(); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }

    public void Dispose() { player.Dispose(); stream.Dispose(); }
}
