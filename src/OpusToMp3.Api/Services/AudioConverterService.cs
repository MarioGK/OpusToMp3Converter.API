using System.Diagnostics;

namespace OpusToMp3.Api.Services;

public sealed class AudioConverterService
{
    private const string TempDirectory = "/dev/shm";
    private const int Mp3BitRate = 128;

    public async Task<string> ConvertOpusToMp3Async(string opusBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opusBase64);

        var opusBytes = Convert.FromBase64String(opusBase64);
        var fileId = Guid.NewGuid().ToString("N");
        var inputPath = Path.Combine(TempDirectory, $"{fileId}.opus");
        var outputPath = Path.Combine(TempDirectory, $"{fileId}.mp3");

        try
        {
            await File.WriteAllBytesAsync(inputPath, opusBytes);

            await RunFfmpegAsync(inputPath, outputPath);

            var mp3Bytes = await File.ReadAllBytesAsync(outputPath);
            return Convert.ToBase64String(mp3Bytes);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    private static async Task RunFfmpegAsync(string inputPath, string outputPath)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{inputPath}\" -b:a {Mp3BitRate}k -y \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {error}");
        }
    }
}
