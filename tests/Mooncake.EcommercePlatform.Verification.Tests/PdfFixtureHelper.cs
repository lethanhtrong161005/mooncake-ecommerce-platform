namespace Mooncake.EcommercePlatform.Verification.Tests;

using System.Text;

internal static class PdfFixtureHelper
{
    internal static byte[] Create(string catalog = "", string? extraObject = null)
    {
        const string text = "BT (/OpenAction /JS /JavaScript /EmbeddedFile /Launch ordinary text) Tj ET";
        var objects = new List<string> {
            $"<< /Type /Catalog /Pages 2 0 R {catalog} >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 600 800] /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(text)} >>\nstream\n{text}\nendstream"
        };
        if (extraObject != null) objects.Add(extraObject);
        using var output = new MemoryStream();
        void Write(string value) => output.Write(Encoding.ASCII.GetBytes(value));
        Write("%PDF-1.7\n"); var offsets = new List<long>();
        for (var i = 0; i < objects.Count; i++) {
            offsets.Add(output.Position); Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position; Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) Write($"{offset:D10} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }
}
