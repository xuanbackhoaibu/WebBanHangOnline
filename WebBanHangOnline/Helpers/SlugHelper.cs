using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WebBanHangOnline.Data;

namespace WebBanHangOnline.Helpers
{
    public static class SlugHelper
    {
        public static string Generate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            text = text.ToLowerInvariant();
            text = RemoveDiacritics(text);
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');

            return text;
        }

        public static async Task<string> GenerateUniqueSlugAsync(
            ApplicationDbContext context,
            string name,
            int? ignoreId = null)
        {
            var baseSlug = Generate(name);
            var slug = baseSlug;
            var i = 1;

            while (await context.Products.AnyAsync(p =>
                       p.Slug == slug &&
                       (!ignoreId.HasValue || p.ProductId != ignoreId)))
            {
                slug = $"{baseSlug}-{i++}";
            }

            return slug;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}