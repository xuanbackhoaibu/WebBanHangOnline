using System.Collections.Generic;
using System.Globalization;

namespace WebBanHangOnline.Helpers;

public static class CatalogTextHelper
{
    private static readonly Dictionary<string, string> EnglishTexts = new()
    {
        ["Đồ Nam"] = "Men",
        ["Đồ Nữ"] = "Women",
        ["Bé Trai"] = "Boys",
        ["Bé Gái"] = "Girls",
        ["Đen"] = "Black",
        ["Trắng"] = "White",
        ["Xanh"] = "Blue",
        ["Navy"] = "Navy",
        ["Áo sơ mi nam Oxford trắng"] = "Men's white Oxford shirt",
        ["Áo polo nam pique xanh navy"] = "Men's navy pique polo",
        ["Quần kaki nam slimfit be"] = "Men's beige slim-fit chinos",
        ["Áo thun basic nam cotton"] = "Men's basic cotton T-shirt",
        ["Đầm midi nữ hoa nhí"] = "Women's floral midi dress",
        ["Blazer nữ form ngắn"] = "Women's cropped blazer",
        ["Chân váy chữ A đen"] = "Black A-line skirt",
        ["Áo kiểu nữ tay phồng"] = "Women's puff-sleeve blouse",
        ["Set bé trai áo thun quần short"] = "Boys' T-shirt and shorts set",
        ["Áo khoác bé trai thể thao"] = "Boys' sporty jacket",
        ["Váy công chúa bé gái pastel"] = "Girls' pastel princess dress",
        ["Set bé gái áo blouse chân váy"] = "Girls' blouse and skirt set",
        ["Áo sơ mi Oxford form regular, dễ phối đi học, đi làm và gặp khách hàng."] = "Regular-fit Oxford shirt that is easy to style for school, work and meetings.",
        ["Polo chất pique thoáng, cổ đứng form gọn, phù hợp phong cách smart casual."] = "Breathable pique polo with a neat collar, perfect for smart casual looks.",
        ["Quần kaki co giãn nhẹ, dáng slimfit lịch sự cho công sở và đi chơi."] = "Light-stretch slim-fit chinos for office days and casual outings.",
        ["Áo thun cotton mềm, màu trung tính, dễ phối với jeans hoặc kaki."] = "Soft cotton T-shirt in neutral tones, easy to pair with jeans or chinos.",
        ["Đầm midi nhẹ nhàng, họa tiết hoa nhí, phù hợp đi làm và dạo phố."] = "Light floral midi dress for workdays and city walks.",
        ["Blazer form ngắn, chất đứng dáng, phối tốt với chân váy hoặc quần tây."] = "Cropped blazer with a structured feel, easy to pair with skirts or trousers.",
        ["Chân váy chữ A basic, dễ mặc, phù hợp phong cách tối giản."] = "Basic A-line skirt that is easy to wear and fits a minimal style.",
        ["Áo kiểu nữ tay phồng nhẹ, tạo điểm nhấn mềm mại cho trang phục hằng ngày."] = "Light puff-sleeve blouse that adds a soft accent to everyday outfits.",
        ["Set đồ năng động cho bé trai, chất liệu cotton thoáng mát."] = "Active boys' set made from cool, breathable cotton.",
        ["Áo khoác nhẹ cho bé trai, phù hợp đi học và hoạt động ngoài trời."] = "Light boys' jacket for school and outdoor activities.",
        ["Váy pastel dễ thương cho bé gái, phù hợp sinh nhật và dịp đặc biệt."] = "Lovely pastel dress for birthdays and special occasions.",
        ["Set phối sẵn gọn gàng, màu sáng, phù hợp đi học và đi chơi cuối tuần."] = "Ready-to-wear bright set for school and weekend outings."
    };

    public static bool IsEnglish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    public static string T(string vi, string en) => IsEnglish ? en : vi;

    public static string Display(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return IsEnglish && EnglishTexts.TryGetValue(value.Trim(), out var english)
            ? english
            : value;
    }
}
