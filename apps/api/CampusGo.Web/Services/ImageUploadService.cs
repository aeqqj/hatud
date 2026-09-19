using Renci.SshNet;
using SkiaSharp;

namespace CampusGo.Web.Services;

public class ImageUploadService(IConfiguration config)
{
    private const int MaxDimension = 512;
    private const int TargetSizeBytes = 100 * 1024;

    public async Task<string> UploadProfilePictureAsync(Stream fileStream, Guid userId)
    {
        using var original = SKBitmap.Decode(fileStream);
        if (original is null)
        {
            throw new InvalidOperationException("The uploaded file could not be decoded as an image.");
        }

        using var resized = ResizeToMax(original, MaxDimension);
        var compressed = CompressToTargetSize(resized);

        var fileName = $"{userId}.jpg";
        UploadToSftp(compressed, fileName);

        var baseUrl = config["Sftp:PublicBaseUrl"]!.TrimEnd('/');
        return $"{baseUrl}/{fileName}";
    }

    private static SKBitmap ResizeToMax(SKBitmap source, int maxDimension)
    {
        var scale = (double)maxDimension / Math.Max(source.Width, source.Height);
        if (scale >= 1.0)
        {
            return source.Copy();
        }

        var newWidth = (int)(source.Width * scale);
        var newHeight = (int)(source.Height * scale);

        var info = new SKImageInfo(newWidth, newHeight);
        var resized = source.Resize(info, SKSamplingOptions.Default);
        return resized ?? source.Copy();
    }

    private static byte[] CompressToTargetSize(SKBitmap bitmap)
    {
        var quality = 85;
        byte[] result;

        using var image = SKImage.FromBitmap(bitmap);

        do
        {
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            result = data.ToArray();
            quality -= 10;
        }
        while (result.Length > TargetSizeBytes && quality > 20);

        return result;
    }

    private void UploadToSftp(byte[] data, string fileName)
    {
        using var client = new SftpClient(
            config["Sftp:Host"]!,
            int.Parse(config["Sftp:Port"]!),
            config["Sftp:Username"]!,
            config["Sftp:Password"]!);

        client.Connect();

        var remotePath = $"{config["Sftp:RemoteDirectory"]!.TrimEnd('/')}/{fileName}";
        using var ms = new MemoryStream(data);
        client.UploadFile(ms, remotePath, true);

        client.Disconnect();
    }
}