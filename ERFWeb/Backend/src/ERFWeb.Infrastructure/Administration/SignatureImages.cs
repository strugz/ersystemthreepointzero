using ERFWeb.Application.Features.Administration;

namespace ERFWeb.Infrastructure.Administration;

/// <summary>Serves a stored signature image with its content type.</summary>
internal static class SignatureImages
{
    // Only PNG and JPEG are accepted on upload; the type is not stored, so read it from the header.
    public static SignatureImageDto ToDto(byte[] bytes)
    {
        var isPng = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
        return new SignatureImageDto(isPng ? "image/png" : "image/jpeg", bytes);
    }
}
