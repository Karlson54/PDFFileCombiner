using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Ookii.Dialogs.Wpf;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace PDFFileCombiner_WPF_
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> selectedFileNames;
        private ObservableCollection<string> selectedFilePaths;
        private readonly PdfMergeService mergeService = new();
        private readonly GhostscriptCompressor compressor = new();

        public MainWindow()
        {
            InitializeComponent();
            selectedFileNames = new ObservableCollection<string>();
            selectedFilePaths = new ObservableCollection<string>();
            FileListBox.ItemsSource = selectedFileNames;
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

        private bool TryMergeAndCompress(IEnumerable<string> inputPaths, string outputPath)
        {
            string pdfSettings = (string)((ComboBoxItem)CompressionComboBox.SelectedItem).Tag;
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                mergeService.Merge(inputPaths, tempPath);
                compressor.Compress(tempPath, outputPath, pdfSettings);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
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
                        selectedFileNames.Add(fileName);
                        selectedFilePaths.Add(file);
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
            if (selectedIndex >= 0 && selectedIndex < selectedFileNames.Count - 1)
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
        
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = FileListBox.SelectedIndex;
            if (selectedIndex < 0)
                return;

            selectedFileNames.RemoveAt(selectedIndex);
            selectedFilePaths.RemoveAt(selectedIndex);

            if (selectedFileNames.Count > 0)
                FileListBox.SelectedIndex = Math.Min(selectedIndex, selectedFileNames.Count - 1);
        }

        private void Combine_PDFs(object sender, RoutedEventArgs e)
        {
            if (!selectedFilePaths.Any())
            {
                MessageBox.Show("Please select PDF files to combine and compress.");
                return;
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string outputFilePath = !string.IsNullOrEmpty(FileName.Text)
                ? Path.Combine(desktopPath, FileName.Text + ".pdf")
                : Path.Combine(desktopPath, "default.pdf");

            if (TryMergeAndCompress(selectedFilePaths, outputFilePath))
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
            string[] pdfFiles = Directory.GetFiles(folderPath, "*.pdf")
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (pdfFiles.Length == 0)
            {
                MessageBox.Show("No PDF files found in the selected folder.");
                return;
            }

            if (TryMergeAndCompress(pdfFiles, outputFilePath))
                MessageBox.Show("PDF files merged and compressed successfully.");
        }
    }
}