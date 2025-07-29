using PdfSharp.Fonts;
using System;
using System.IO;

public class CustomFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "OpenSans-Regular.ttf");
        return File.ReadAllBytes(fontPath);
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo("OpenSans#Regular");
    }
}
