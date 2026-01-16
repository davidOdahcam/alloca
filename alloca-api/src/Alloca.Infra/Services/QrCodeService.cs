using Alloca.Application.Common.Interfaces;
using QRCoder;

namespace Alloca.Infra.Services;

public class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(pixelsPerModule);
    }
}
