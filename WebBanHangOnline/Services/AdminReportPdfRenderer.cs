using System.Globalization;
using System.Text;
using WebBanHangOnline.Models.ViewModels;

namespace WebBanHangOnline.Services;

public sealed class AdminReportPdfRenderer
{
    public byte[] RenderRevenueReport(ReportIndexViewModel report)
    {
        var lines = BuildLines(report);
        var pages = lines.Chunk(34).ToList();
        var objects = new List<string> { string.Empty };
        var pageObjectIds = new List<int>();
        AddObject(objects, "<< /Type /Catalog /Pages 2 0 R >>");
        AddObject(objects, string.Empty);
        var fontObjectId = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var boldFontObjectId = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        foreach (var pageLines in pages)
        {
            var content = BuildPageContent(pageLines);
            var contentObjectId = AddObject(objects, $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            var pageObjectId = AddObject(objects, string.Empty);
            pageObjectIds.Add(pageObjectId);
            objects[pageObjectId] = $"""
                << /Type /Page
                   /Parent 2 0 R
                   /MediaBox [0 0 595 842]
                   /Resources << /Font << /F1 {fontObjectId} 0 R /F2 {boldFontObjectId} 0 R >> >>
                   /Contents {contentObjectId} 0 R
                >>
                """;
        }

        var kids = string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"));
        objects[2] = $"<< /Type /Pages /Kids [{kids}] /Count {pageObjectIds.Count} >>";

        return WritePdf(objects);
    }

    private static List<PdfLine> BuildLines(ReportIndexViewModel report)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var range = report.FromDate.HasValue || report.ToDate.HasValue
            ? $"{report.FromDate?.ToString("dd/MM/yyyy") ?? "..."} - {report.ToDate?.ToString("dd/MM/yyyy") ?? "..."}"
            : "Tat ca thoi gian";

        var lines = new List<PdfLine>
        {
            new("XUANBAC FASHION", true, 18),
            new("BAO CAO DOANH THU", true, 16),
            new($"Khoang ngay: {range}", false, 10),
            new($"Xuat luc: {DateTime.Now:dd/MM/yyyy HH:mm}", false, 10),
            new(" ", false, 8),
            new($"Tong doanh thu: {report.TotalRevenue.ToString("N0", vi)} VND", true, 12),
            new($"Tong don hop le: {report.TotalOrders:N0}", false, 11),
            new($"Gia tri trung binh: {report.AverageOrderValue.ToString("N0", vi)} VND", false, 11),
            new($"Tong giam gia: {report.TotalDiscount.ToString("N0", vi)} VND", false, 11),
            new(" ", false, 8),
            new("DOANH THU THEO NGAY", true, 12)
        };

        if (report.DailyRevenue.Any())
        {
            lines.Add(new("Ngay          Don    Doanh thu", true, 10));
            lines.AddRange(report.DailyRevenue.Select(item =>
                new PdfLine($"{item.Date:dd/MM/yyyy}   {item.Orders,4}   {item.Revenue.ToString("N0", vi),15} VND", false, 10)));
        }
        else
        {
            lines.Add(new("Chua co doanh thu trong khoang nay.", false, 10));
        }

        lines.Add(new(" ", false, 8));
        lines.Add(new("TOP SAN PHAM", true, 12));
        if (report.TopProducts.Any())
        {
            lines.Add(new("SL     Doanh thu          San pham", true, 10));
            lines.AddRange(report.TopProducts.Select(item =>
                new PdfLine($"{item.Quantity,4}   {item.Revenue.ToString("N0", vi),15} VND   {Ascii(item.ProductName, 46)}", false, 10)));
        }
        else
        {
            lines.Add(new("Chua co du lieu san pham.", false, 10));
        }

        lines.Add(new(" ", false, 8));
        lines.Add(new("DON HANG GAN NHAT", true, 12));
        lines.Add(new("Ma don       Ngay gio          Tong tien      TT don      TT tien", true, 9));
        lines.AddRange(report.Orders.Take(20).Select(order =>
            new PdfLine($"{order.OrderCode}   {order.OrderDate:dd/MM HH:mm}   {order.TotalAmount.ToString("N0", vi),12}   {Ascii(order.Status, 10),-10}   {Ascii(order.PaymentStatus, 10)}", false, 9)));

        return lines;
    }

    private static string BuildPageContent(IEnumerable<PdfLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("q");
        builder.AppendLine("0.96 0.95 0.93 rg 0 0 595 842 re f");
        builder.AppendLine("1 1 1 rg 36 34 523 774 re f");
        builder.AppendLine("0.85 0.31 0.24 rg 36 780 523 28 re f");
        builder.AppendLine("BT");

        var y = 756;
        foreach (var line in lines)
        {
            var font = line.Bold ? "F2" : "F1";
            builder.AppendLine($"/{font} {line.FontSize.ToString(CultureInfo.InvariantCulture)} Tf");
            builder.AppendLine($"1 0 0 1 56 {y} Tm");
            builder.AppendLine($"({Escape(Ascii(line.Text, 96))}) Tj");
            y -= line.FontSize + 7;
        }

        builder.AppendLine("ET");
        builder.AppendLine("Q");
        return builder.ToString();
    }

    private static byte[] WritePdf(List<string> objects)
    {
        var offsets = new List<int> { 0 };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };

        writer.WriteLine("%PDF-1.4");

        for (var i = 1; i < objects.Count; i++)
        {
            writer.Flush();
            offsets.Add((int)stream.Position);
            writer.WriteLine($"{i} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xrefOffset = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count}");
        writer.WriteLine("0000000000 65535 f ");
        for (var i = 1; i < offsets.Count; i++)
        {
            writer.WriteLine($"{offsets[i]:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefOffset);
        writer.WriteLine("%%EOF");
        writer.Flush();
        return stream.ToArray();
    }

    private static int AddObject(List<string> objects, string value)
    {
        objects.Add(value);
        return objects.Count - 1;
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static string Ascii(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(c switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => c <= 127 ? c : ' '
            });
        }

        var text = builder.ToString().Normalize(NormalizationForm.FormC);
        return text.Length <= maxLength ? text : text[..Math.Max(0, maxLength - 3)] + "...";
    }

    private sealed record PdfLine(string Text, bool Bold, int FontSize);
}
