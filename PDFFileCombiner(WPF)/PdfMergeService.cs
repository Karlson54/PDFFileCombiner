using System.Collections.Generic;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace PDFFileCombiner_WPF_
{
    public class PdfMergeService
    {
        public void Merge(IEnumerable<string> inputPaths, string outputPath)
        {
            using var stream = new FileStream(outputPath, FileMode.Create);
            var document = new Document();
            var copy = new PdfSmartCopy(document, stream);

            copy.SetFullCompression();                         // до document.Open()
            copy.CompressionLevel = PdfStream.BEST_COMPRESSION;

            document.Open();

            foreach (string path in inputPaths)
            {
                using var reader = new PdfReader(path);
                copy.AddDocument(reader);
            }

            document.Close();
        }
    }
}