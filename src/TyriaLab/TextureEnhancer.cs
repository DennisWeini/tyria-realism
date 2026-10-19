using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

sealed record TextureEnhanceReport(string Input, string Output, int Width, int Height, double Strength, string InputSha256, string OutputSha256, string Limitation);
sealed record TextureClassification(string File, int Width, int Height, double OpaqueFraction, double TransparentFraction, double MeanLuma, double LumaStdDev, double MeanSaturation, double MeanRed, double MeanGreen, double MeanBlue, string Category, bool Eligible, string Reason);
sealed record TexturePackItem(TextureClassification Classification, string InputSha256, string? Output, string? OutputSha256);
sealed record TexturePackReport(string Source, string Output, string Preset, double Strength, int Examined, int Enhanced, IReadOnlyDictionary<string,int> Categories, IReadOnlyList<TexturePackItem> Items, string Limitation);

static class TextureEnhancer {
    static readonly Regex HashName = new(@"^0x[0-9A-Fa-f]{8}\.png$", RegexOptions.Compiled);
    static string Hash(string p) { using var f = File.OpenRead(p); return Convert.ToHexString(SHA256.HashData(f)).ToLowerInvariant(); }
    static bool PowerOfTwo(int value) => value > 0 && (value & (value - 1)) == 0;

    public static TextureClassification Classify(string input) {
        input = Path.GetFullPath(input);
        if (!File.Exists(input)) throw new FileNotFoundException("Dump-Datei fehlt.", input);
        if (!HashName.IsMatch(Path.GetFileName(input))) throw new ArgumentException("Erwartet wird eine unveränderte ReShade-Hashdatei wie 0x1234ABCD.png.");
        using var source = new Bitmap(input);
        int w = source.Width, h = source.Height;
        int step = Math.Max(1, Math.Max(w, h) / 256);
        long samples = 0, opaque = 0, transparent = 0;
        double luma = 0, luma2 = 0, saturation = 0, red = 0, green = 0, blue = 0;
        for (int y = 0; y < h; y += step) for (int x = 0; x < w; x += step) {
            var p = source.GetPixel(x, y);
            double r = p.R / 255.0, g = p.G / 255.0, b = p.B / 255.0;
            double hi = Math.Max(r, Math.Max(g, b)), lo = Math.Min(r, Math.Min(g, b));
            double lum = .2126 * r + .7152 * g + .0722 * b;
            samples++; if (p.A >= 250) opaque++; if (p.A <= 5) transparent++;
            luma += lum; luma2 += lum * lum; saturation += hi == 0 ? 0 : (hi - lo) / hi; red += r; green += g; blue += b;
        }
        double inv = 1.0 / samples, meanLuma = luma * inv, variance = Math.Max(0, luma2 * inv - meanLuma * meanLuma);
        double meanSat = saturation * inv, meanR = red * inv, meanG = green * inv, meanB = blue * inv;
        double opaqueFraction = opaque * inv, transparentFraction = transparent * inv;
        string category, reason; bool eligible = false;
        bool normalLike = (meanB > meanR * 1.22 && meanB > meanG * 1.12 && meanSat > .25)
            || (meanR > meanB * 1.28 && meanG > meanB * 1.20 && meanSat > .42);
        if (w < 512 || h < 512 || !PowerOfTwo(w) || !PowerOfTwo(h)) { category = "SmallOrUI"; reason = "Kleiner als 512 px oder keine Zweierpotenz; wahrscheinlich UI, Atlas oder Hilfstextur."; }
        else if (opaqueFraction < .995) { category = "Transparent"; reason = "Enthält nennenswerte Transparenz; Vegetation, Effekte und UI werden geschützt."; }
        else if (normalLike) { category = "NormalMap"; reason = "Farbverteilung ähnelt einer Normal- oder Materialsteuertextur."; }
        else if (meanLuma < .07 || meanLuma > .93 || Math.Sqrt(variance) < .045) { category = "MaskOrEffect"; reason = "Zu dunkel, hell oder gleichförmig; wahrscheinlich Maske, Effekt oder Steuerdaten."; }
        else if (meanSat > .58) { category = "Unknown"; reason = "Sehr hohe Farbsättigung; konservativ nicht automatisch verändert."; }
        else { category = "EligibleDiffuse"; eligible = true; reason = "Deckende, strukturierte Materialtextur mit konservativ plausibler Farbbalance."; }
        return new TextureClassification(input, w, h, opaqueFraction, transparentFraction, meanLuma, Math.Sqrt(variance), meanSat, meanR, meanG, meanB, category, eligible, reason);
    }

    public static TexturePackReport BuildPack(string sourceFolder, string outputFolder, string preset) {
        sourceFolder = Path.GetFullPath(sourceFolder); outputFolder = Path.GetFullPath(outputFolder);
        if (!Directory.Exists(sourceFolder)) throw new DirectoryNotFoundException("texdump-Ordner fehlt: " + sourceFolder);
        if (Directory.Exists(outputFolder) && Directory.EnumerateFileSystemEntries(outputFolder).Any()) throw new ArgumentException("Ausgabeordner muss neu oder leer sein.");
        double strength = preset.Equals("Detail", StringComparison.OrdinalIgnoreCase) ? .90
            : preset.Equals("Natural", StringComparison.OrdinalIgnoreCase) ? .55
            : throw new ArgumentException("Preset muss Natural oder Detail sein.");
        Directory.CreateDirectory(outputFolder);
        var items = new List<TexturePackItem>();
        foreach (string input in Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)) {
            if (!HashName.IsMatch(Path.GetFileName(input))) continue;
            var classification = Classify(input); string inputHash = Hash(input);
            if (!classification.Eligible) { items.Add(new(classification, inputHash, null, null)); continue; }
            string output = Path.Combine(outputFolder, Path.GetFileName(input));
            var enhanced = Enhance(input, output, strength);
            items.Add(new(classification, inputHash, output, enhanced.OutputSha256));
        }
        var categories = items.GroupBy(x => x.Classification.Category).ToDictionary(g => g.Key, g => g.Count());
        var report = new TexturePackReport(sourceFolder, outputFolder, preset, strength, items.Count, items.Count(x => x.Classification.Eligible), categories, items,
            "Nur der aktuell aus GW2 erfasste lokale Bestand wurde untersucht. Automatische Kategorien sind konservative Heuristiken; tatsächliche Darstellung und Formatunterstützung müssen im Spiel geprüft werden.");
        File.WriteAllText(Path.Combine(outputFolder, "tyria-texture-manifest.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return report;
    }

    public static TextureEnhanceReport Enhance(string input, string output, double strength) {
        input = Path.GetFullPath(input); output = Path.GetFullPath(output);
        if (!File.Exists(input)) throw new FileNotFoundException("Dump-Datei fehlt.", input);
        if (!HashName.IsMatch(Path.GetFileName(input))) throw new ArgumentException("Erwartet wird eine unveränderte ReShade-Hashdatei wie 0x1234ABCD.png.");
        if (strength < 0 || strength > 2) throw new ArgumentOutOfRangeException(nameof(strength), "Stärke muss zwischen 0 und 2 liegen.");
        using var source = new Bitmap(input);
        if (source.Width < 4 || source.Height < 4 || source.Width > 8192 || source.Height > 8192) throw new ArgumentException("Nicht unterstützte Texturabmessung.");
        using var image = source.Clone(new Rectangle(0, 0, source.Width, source.Height), PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, image.Width, image.Height);
        var data = image.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        try {
            int stride = data.Stride, w = image.Width, h = image.Height;
            var bytes = new byte[Math.Abs(stride) * h]; System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            var original = (byte[])bytes.Clone();
            double LumaAt(int x, int y) { int p = y * stride + x * 4; return (0.0722 * original[p] + 0.7152 * original[p + 1] + 0.2126 * original[p + 2]) / 255.0; }
            static double Smooth(double a, double b, double x) { x = Math.Clamp((x - a) / (b - a), 0, 1); return x * x * (3 - 2 * x); }
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) {
                int p = y * stride + x * 4; int xl = Math.Max(0, x - 1), xr = Math.Min(w - 1, x + 1), yu = Math.Max(0, y - 1), yd = Math.Min(h - 1, y + 1);
                double l = LumaAt(x, y), blur = (LumaAt(xl, y) + LumaAt(xr, y) + LumaAt(x, yu) + LumaAt(x, yd) + 4 * l) / 8.0;
                double edge = l - blur, activity = Smooth(0.002, 0.035, Math.Abs(edge));
                double protect = Smooth(0.025, 0.10, l) * (1.0 - Smooth(0.86, 0.985, l));
                double deltaNorm = Math.Clamp(edge * strength * activity * protect, -0.030, 0.030);
                double headroom = Math.Max(0, 254 - Math.Max(original[p], Math.Max(original[p + 1], original[p + 2]))) / 255.0;
                double footroom = Math.Max(0, Math.Min(original[p], Math.Min(original[p + 1], original[p + 2])) - 1) / 255.0;
                deltaNorm = Math.Clamp(deltaNorm, -footroom, headroom);
                int delta = (int)Math.Round(deltaNorm * 255.0);
                bytes[p] = (byte)Math.Clamp(original[p] + delta, 0, 255);
                bytes[p + 1] = (byte)Math.Clamp(original[p + 1] + delta, 0, 255);
                bytes[p + 2] = (byte)Math.Clamp(original[p + 2] + delta, 0, 255);
                bytes[p + 3] = original[p + 3];
            }
            System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
        } finally { image.UnlockBits(data); }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        image.Save(output, ImageFormat.Png);
        var report = new TextureEnhanceReport(input, output, image.Width, image.Height, strength, Hash(input), Hash(output), "Same-size luminance detail only. Runtime replacement still depends on the source texture format supported by the ReShade example add-on.");
        File.WriteAllText(output + ".tyria.json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return report;
    }
}

