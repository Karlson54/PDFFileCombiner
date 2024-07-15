using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;

namespace PDFFileCombiner_WPF_
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> selectedFileNames; // Обновленный список для хранения имен файлов
        private ObservableCollection<string> selectedFilePaths; // Список для хранения полных путей к файлам

        public MainWindow()
        {
            InitializeComponent();
            selectedFileNames = new ObservableCollection<string>();
            selectedFilePaths = new ObservableCollection<string>();
            FileListBox.ItemsSource = selectedFileNames; // Привязываем список к ListBox
        }

        private void FileListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void FileListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    if (Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        string fileName = Path.GetFileName(file);
                        selectedFileNames.Add(fileName); // Добавляем только имя файла в ListBox
                        selectedFilePaths.Add(file); // Добавляем полный путь к файлу в коллекцию
                    }
                    else
                    {
                        MessageBox.Show("Only PDF files are supported.");
                    }
                }
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FileListBox.SelectedIndex;
            if (selectedIndex > 0)
            {
                string selectedFileName = selectedFileNames[selectedIndex];
                string selectedFilePath = selectedFilePaths[selectedIndex];

                selectedFileNames.RemoveAt(selectedIndex);
                selectedFilePaths.RemoveAt(selectedIndex);

                selectedFileNames.Insert(selectedIndex - 1, selectedFileName);
                selectedFilePaths.Insert(selectedIndex - 1, selectedFilePath);

                FileListBox.SelectedIndex = selectedIndex - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FileListBox.SelectedIndex;
            if (selectedIndex < selectedFileNames.Count - 1)
            {
                string selectedFileName = selectedFileNames[selectedIndex];
                string selectedFilePath = selectedFilePaths[selectedIndex];

                selectedFileNames.RemoveAt(selectedIndex);
                selectedFilePaths.RemoveAt(selectedIndex);

                selectedFileNames.Insert(selectedIndex + 1, selectedFileName);
                selectedFilePaths.Insert(selectedIndex + 1, selectedFilePath);

                FileListBox.SelectedIndex = selectedIndex + 1;
            }
        }

        private void Combine_PDFs(object sender, RoutedEventArgs e)
        {
            if (!selectedFilePaths.Any())
            {
                MessageBox.Show("Please select PDF files to combine and compress.");
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string outputFilePath = !string.IsNullOrEmpty(FileName.Text) ?
                Path.Combine(desktopPath, FileName.Text + ".pdf") :
                Path.Combine(desktopPath, "default.pdf");

            MergeAndCompressPDFs(outputFilePath, selectedFilePaths.ToArray());
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
                if (!string.IsNullOrEmpty(FileNameForFolder.Text))
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
