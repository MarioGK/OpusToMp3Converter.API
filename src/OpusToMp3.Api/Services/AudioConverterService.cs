using System.Buffers;
using Concentus;
using Concentus.Oggfile;
using Microsoft.IO;
using NAudio.Lame;
using NAudio.Wave;

namespace OpusToMp3.Api.Services;

public sealed class AudioConverterService
{
    private static readonly RecyclableMemoryStreamManager StreamManager = new();

    private const int OpusSampleRate = 48000;
    private const int OpusChannels = 2;
    private const int Mp3BitRate = 128;
    private const int PcmBufferSize = 5760;

    public string ConvertOpusToMp3(string opusBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opusBase64);

        var opusBytes = Convert.FromBase64String(opusBase64);

        using var opusStream = StreamManager.GetStream("OpusInput", opusBytes, 0, opusBytes.Length);
        using var mp3Stream = StreamManager.GetStream("Mp3Output");

        ConvertOpusStreamToMp3(opusStream, mp3Stream);

        return Convert.ToBase64String(mp3Stream.GetBuffer(), 0, (int)mp3Stream.Length);
    }

    private static void ConvertOpusStreamToMp3(Stream opusStream, MemoryStream mp3Stream)
    {
        var decoder = OpusCodecFactory.CreateDecoder(OpusSampleRate, OpusChannels);
        var oggReader = new OpusOggReadStream(decoder, opusStream);

        var waveFormat = new WaveFormat(OpusSampleRate, 16, OpusChannels);
        using var mp3Writer = new LameMP3FileWriter(mp3Stream, waveFormat, Mp3BitRate);

        var byteBuffer = ArrayPool<byte>.Shared.Rent(PcmBufferSize * OpusChannels * sizeof(short));

        try
        {
            while (oggReader.HasNextPacket)
            {
                var samples = oggReader.DecodeNextPacket();
                if (samples == null || samples.Length == 0)
                    continue;

                var byteCount = samples.Length * sizeof(short);
                Buffer.BlockCopy(samples, 0, byteBuffer, 0, byteCount);
                mp3Writer.Write(byteBuffer, 0, byteCount);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteBuffer);
        }
    }
}
