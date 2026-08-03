using WebBanHangOnline.Helpers;

namespace WebBanHangOnline.Tests;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Áo Thun Nam Đẹp", "ao-thun-nam-dep")]
    [InlineData("  Váy nữ mùa hè!!!  ", "vay-nu-mua-he")]
    [InlineData("Giày Sneaker 2026", "giay-sneaker-2026")]
    public void Generate_NormalizesVietnameseText(string input, string expected)
    {
        var slug = SlugHelper.Generate(input);

        Assert.Equal(expected, slug);
    }

    [Fact]
    public void Generate_ReturnsEmptyString_WhenInputIsBlank()
    {
        Assert.Equal(string.Empty, SlugHelper.Generate("   "));
    }
}
