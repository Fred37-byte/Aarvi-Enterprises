using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace NewCustomerWindow.xaml
{
    internal class XFontOptions
    {
        private PdfFontEncoding unicode;
        private XFontStyleEx bold;

        public XFontOptions(PdfFontEncoding unicode, XFontStyleEx bold)
        {
            this.unicode = unicode;
            this.bold = bold;
        }
    }
}