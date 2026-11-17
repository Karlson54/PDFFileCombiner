using System;
using System.Diagnostics;
using System.IO;

namespace PDFFileCombiner_WPF_
{
    public class GhostscriptCompressor
    {
        private static readonly string GsDirectory = Path.Combine(AppContext.BaseDirectory, "gs");
        private static readonly string GsExecutable = Path.Combine(GsDirectory, "bin", "gswin64c.exe");

        public void Compress(string inputPath, string outputPath, string pdfSettings)
        {
            if (!File.Exists(GsExecutable))
                throw new FileNotFoundException("Не найден Ghostscript: " + GsExecutable);

            var startInfo = new ProcessStartInfo(GsExecutable)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add(
                $"-I{Path.Combine(GsDirectory, "lib")};{Path.Combine(GsDirectory, "Resource", "Init")};{Path.Combine(GsDirectory, "Resource", "Font")}");
            startInfo.ArgumentList.Add("-sDEVICE=pdfwrite");
            startInfo.ArgumentList.Add($"-dPDFSETTINGS={pdfSettings}");
            startInfo.ArgumentList.Add("-dCompatibilityLevel=1.5");
            startInfo.ArgumentList.Add("-dPDFSETTINGS=/ebook");
            startInfo.ArgumentList.Add("-dNOPAUSE");
            startInfo.ArgumentList.Add("-dBATCH");
            startInfo.ArgumentList.Add("-dQUIET");
            startInfo.ArgumentList.Add($"-sOutputFile={outputPath}");
            startInfo.ArgumentList.Add(inputPath);

            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException("Не удалось запустить Ghostscript.");

            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Ghostscript завершился с ошибкой: {error}");
        }
    }
}