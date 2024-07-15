using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Ookii.Dialogs.Wpf;

namespace PDFFileCombiner_WPF_
{
    public partial class MainWindow : Window
    {
        private readonly string[] selectedFilePaths = new string[5];

        public MainWindow()
        {
            InitializeComponent();
        }

        private string? OpenPdfFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        private void SetSelectedFilePath(int buttonNumber, string selectedFilePath)
        {
            selectedFilePaths[buttonNumber - 1] = selectedFilePath;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            int buttonNumber = int.Parse(button.Tag.ToString());
            string selectedFilePath = OpenPdfFileDialog();

            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                SetSelectedFilePath(buttonNumber, selectedFilePath);
                button.Content = Path.GetFileName(selectedFilePath);
            }
        }


        private void Combine_PDFs(object sender, RoutedEventArgs e)
        {
            if (selectedFilePaths.All(file => string.IsNullOrEmpty(file)))
            {
                MessageBox.Show("Please select PDF files to combine and compress.");
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string outputFilePath = FileName.Text != string.Empty ?
                Path.Combine(desktopPath, FileName.Text + ".pdf") :
                Path.Combine(desktopPath, "default.pdf");

            MergeAndCompressPDFs(outputFilePath, selectedFilePaths);
            MessageBox.Show("PDF file(s) merged and compressed successfully.");
        }

        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new VistaFolderBrowserDialog();
            if (folderDialog.ShowDialog() == true)
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                string selectedFolderPath = folderDialog.SelectedPath;
                string outputFilePath;
                if (FileNameForFolder.Text != string.Empty)
                {
                    outputFilePath = Path.Combine(desktopPath, FileNameForFolder.Text + ".pdf");
                }
                else
                {
                    outputFilePath = Path.Combine(desktopPath, "default.pdf");
                }
                MergePDFsFromFolder(selectedFolderPath, outputFilePath);
            }
        }

        private void MergePDFsFromFolder(string folderPath, string outputFilePath)
        {
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf");
            if (pdfFiles.Length == 0)
            {
                MessageBox.Show("No PDF files found in the selected folder.");
                return;
            }

            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
            {
                Document document = new Document();
                PdfCopy copy = new PdfCopy(document, fs);

                document.Open();

                foreach (string pdfFile in pdfFiles)
                {
                    PdfReader reader = new PdfReader(pdfFile);
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        PdfImportedPage page = copy.GetImportedPage(reader, i);
                        copy.AddPage(page);
                    }
                    reader.Close();
                }

                document.Close();
            }

            MessageBox.Show("PDF files merged successfully.");
        }

        private static void MergeAndCompressPDFs(string outputFilePath, params string[] pdfFiles)
        {
            using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
            {
                Document document = new Document();
                PdfCopy copy = new PdfCopy(document, fs);

                document.Open();

                foreach (string pdfFile in pdfFiles)
                {
                    if (!string.IsNullOrEmpty(pdfFile))
                    {
                        string compressedPdfFile = Path.Combine(Path.GetTempPath(), "compressed.pdf");
                        CompressPDF(pdfFile, compressedPdfFile);

                        PdfReader reader = new PdfReader(compressedPdfFile);
                        copy.AddDocument(reader);
                        reader.Close();
                    }
                }

                document.Close();
            }
        }

        private static void CompressPDF(string inputPath, string outputPath)
        {
            using (FileStream inputStream = new FileStream(inputPath, FileMode.Open))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create))
            {
                PdfReader reader = new PdfReader(inputStream);
                PdfStamper stamper = new PdfStamper(reader, outputStream);

                PdfDictionary pageDict;
                for (int pageIndex = 1; pageIndex <= reader.NumberOfPages; pageIndex++)
                {
                    pageDict = reader.GetPageN(pageIndex);
                    pageDict.Remove(PdfName.ROTATE);
                }

                stamper.SetFullCompression();

                stamper.Close();
                reader.Close();
            }
        }
    }
}