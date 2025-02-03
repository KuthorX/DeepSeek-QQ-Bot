using System;
using System.IO;
using System.Threading.Tasks;

namespace QQBotCSharp;

public class QrCodeHandler
{
    public static async Task SaveQrCodeAsPng(byte[] qrCode, string filePath)
    {
        if (qrCode == null || qrCode.Length == 0)
        {
            throw new ArgumentException("QR code data is empty or null.", nameof(qrCode));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        try
        {
            // 将字节数组保存为 PNG 文件
            await File.WriteAllBytesAsync(filePath, qrCode);
            Console.WriteLine($"QR code saved successfully to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save QR code: {ex.Message}");
        }
    }
}