namespace Alloca.Application.Common.Interfaces;

public interface IQrCodeService
{
    /// <summary>Returns a PNG byte array encoding the given payload.</summary>
    byte[] GeneratePng(string payload, int pixelsPerModule = 6);
}
