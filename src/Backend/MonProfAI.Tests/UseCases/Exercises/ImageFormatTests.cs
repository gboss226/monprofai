using MonProfAI.Application.UseCases.Exercises;
using Xunit;

namespace MonProfAI.Tests.UseCases.Exercises;

public class ImageFormatTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0 }, "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png")]
    [InlineData(new byte[] { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' }, "image/gif")]
    [InlineData(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F', 1, 2, 3, 4, (byte)'W', (byte)'E', (byte)'B', (byte)'P' }, "image/webp")]
    [InlineData(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F' }, null)]
    [InlineData(new byte[] { 0xFF, 0xD8 }, null)]
    [InlineData(new byte[0], null)]
    public void Sniff_RecognisesSupportedImagesFromTheirFirstBytes(byte[] bytes, string? expected)
    {
        Assert.Equal(expected, ImageFormat.Sniff(bytes));
    }
}
