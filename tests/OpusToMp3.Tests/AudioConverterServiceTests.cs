using Concentus;
using Concentus.Enums;
using Concentus.Oggfile;
using OpusToMp3.Api.Services;

namespace OpusToMp3.Tests;

public class AudioConverterServiceTests
{
    private readonly AudioConverterService _service = new();

    [Test]
    public async Task ConvertOpusToMp3_WithNullInput_ThrowsArgumentException()
    {
        await Assert.That(() => _service.ConvertOpusToMp3(null!))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ConvertOpusToMp3_WithEmptyInput_ThrowsArgumentException()
    {
        await Assert.That(() => _service.ConvertOpusToMp3(string.Empty))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ConvertOpusToMp3_WithWhitespaceInput_ThrowsArgumentException()
    {
        await Assert.That(() => _service.ConvertOpusToMp3("   "))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ConvertOpusToMp3_WithInvalidBase64_ThrowsFormatException()
    {
        await Assert.That(() => _service.ConvertOpusToMp3("not-valid-base64!!!"))
            .Throws<FormatException>();
    }

    [Test]
    public async Task ConvertOpusToMp3_WithValidOpus_ReturnsMp3Base64()
    {
        var opusBase64 = GenerateTestOpusBase64();

        var result = _service.ConvertOpusToMp3(opusBase64);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length).IsGreaterThan(0);

        var mp3Bytes = Convert.FromBase64String(result);
        await Assert.That(mp3Bytes.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ConvertOpusToMp3_Mp3OutputStartsWithValidHeader()
    {
        var opusBase64 = GenerateTestOpusBase64();

        var result = _service.ConvertOpusToMp3(opusBase64);
        var mp3Bytes = Convert.FromBase64String(result);

        // MP3 files start with either ID3 tag (0x49, 0x44, 0x33) or frame sync (0xFF, 0xFB)
        var startsWithId3 = mp3Bytes.Length >= 3 && mp3Bytes[0] == 0x49 && mp3Bytes[1] == 0x44 && mp3Bytes[2] == 0x33;
        var startsWithFrameSync = mp3Bytes.Length >= 2 && mp3Bytes[0] == 0xFF && (mp3Bytes[1] & 0xE0) == 0xE0;

        await Assert.That(startsWithId3 || startsWithFrameSync).IsTrue();
    }

    [Test]
    public async Task ConvertOpusToMp3_WithRealSampleFile_ProducesPlayableMp3()
    {
        var testFilesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestFiles");
        var opusFilePath = Path.Combine(testFilesDir, "sample.opus");

        if (!File.Exists(opusFilePath))
        {
            Assert.Fail($"Test file not found: {opusFilePath}");
            return;
        }

        var opusBytes = await File.ReadAllBytesAsync(opusFilePath);
        var opusBase64 = Convert.ToBase64String(opusBytes);

        var result = _service.ConvertOpusToMp3(opusBase64);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length).IsGreaterThan(0);

        var mp3Bytes = Convert.FromBase64String(result);
        await Assert.That(mp3Bytes.Length).IsGreaterThan(0);

        // Save MP3 to disk for manual quality testing
        var outputPath = Path.Combine(testFilesDir, "output.mp3");
        await File.WriteAllBytesAsync(outputPath, mp3Bytes);

        Console.WriteLine($"MP3 saved to: {outputPath}");
        Console.WriteLine($"Original Opus size: {opusBytes.Length} bytes");
        Console.WriteLine($"Converted MP3 size: {mp3Bytes.Length} bytes");
    }

    private static string GenerateTestOpusBase64()
    {
        const int sampleRate = 48000;
        const int channels = 2;
        const int durationMs = 100;
        const int samplesPerChannel = sampleRate * durationMs / 1000;

        var pcmSamples = new short[samplesPerChannel * channels];

        for (var i = 0; i < samplesPerChannel; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 1000);
            pcmSamples[i * channels] = sample;
            pcmSamples[i * channels + 1] = sample;
        }

        using var memoryStream = new MemoryStream();
        var encoder = OpusCodecFactory.CreateEncoder(sampleRate, channels, OpusApplication.OPUS_APPLICATION_AUDIO);
        encoder.Bitrate = 64000;

        var oggWriter = new OpusOggWriteStream(encoder, memoryStream);
        oggWriter.WriteSamples(pcmSamples, 0, pcmSamples.Length);
        oggWriter.Finish();

        return Convert.ToBase64String(memoryStream.ToArray());
    }
}
