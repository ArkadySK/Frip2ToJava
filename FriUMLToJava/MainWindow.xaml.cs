using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using Path = System.IO.Path;

namespace FriUMLToJava
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _outputFolder = Path.Combine(Environment.CurrentDirectory, "Output");
        private UMLConverter _UMLConverter;
        private string _filePath = "";
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(_outputFolder))
                openConvertedFolderButton.IsEnabled = false;
        }

        
        private void UpdateFileSelection(string filePath)
        {
            filePathTextBox.Text = "Loaded file:\n" + filePath;
            _filePath = filePath;
            projectNameTextBox.Text = Path.GetFileNameWithoutExtension(filePath);
        }

        private void openFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".xml"; 
            dialog.Filter = "UML FRI2 Solution (.frip2)|*.frip2|XML file (.xml)|*.xml"; 
            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value)
                return;

            string file = dialog.FileName;

            UpdateFileSelection(file);
        }

        private async void convertButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_filePath))
            {
                MessageBox.Show("Please select a file!", "Cannot convert file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _UMLConverter = new UMLConverter();

            filePathTextBox.Text = "Conversion started...";
            try
            {
                await _UMLConverter.ConvertFileAsync(_filePath);
                filePathTextBox.Text = "Successfully converted!";
                await _UMLConverter.WriteToJavaAsync(projectNameTextBox.Text, _outputFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                filePathTextBox.Text = "Error when converting: " + ex.Message;
            }


            if (!Directory.Exists(_outputFolder))
                openConvertedFolderButton.IsEnabled = false;
            else
                openConvertedFolderButton.IsEnabled = true;
        }

        private void openConvertedFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if(Directory.Exists(_outputFolder))
                Process.Start("explorer", _outputFolder + Path.DirectorySeparatorChar);
        }

        private void Border_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var file = files[0];

                UpdateFileSelection(file);
            }
        }
    }
}
