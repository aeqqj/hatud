using Renci.SshNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace CampusGo.Web.Services;

public class ImageUploadService(IConfiguration config)
{
    private const int MaxDimension = 512;
    private const int TargetSizeBytes = 100 * 1024;

    public async Task<string> UploadProfilePictureAsync(Stream fileStream, Guid userId)
    {
        using var image = await Image.LoadAsync(fileStream);

        // Resize so the longer side is at most MaxDimension, preserving aspect ratio
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaxDimension, MaxDimension)
        }));

        var compressed = await CompressToTargetSizeAsync(image);

        var fileName = $"{userId}.jpg";
        UploadToSftp(compressed, fileName);

        var baseUrl = config["Sftp:PublicBaseUrl"]!.TrimEnd('/');
        return $"{baseUrl}/{fileName}";
    }

    private static async Task<byte[]> CompressToTargetSizeAsync(Image image)
    {
        var quality = 85;
        byte[] result;

        do
        {
            using var ms = new MemoryStream();
            await image.SaveAsync(ms, new JpegEncoder { Quality = quality });
            result = ms.ToArray();
            quality -= 10;
        } while (result.Length > TargetSizeBytes && quality > 20);

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