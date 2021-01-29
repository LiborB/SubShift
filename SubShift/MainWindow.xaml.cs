using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WpfApp7.Properties;

namespace WpfApp7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> subFiles = new List<string>();
        private string initialDirectory = "";
        public MainWindow()
        {
            InitializeComponent();
            initialDirectory = Settings.Default.file_path;
            txt_Overwrite.Text = Settings.Default.overwrite_string;
            chkbox_Overwrite.IsChecked = Settings.Default.overwrite_checked;
            if (chkbox_Overwrite.IsChecked != true)
            {
                cont_OutputWrapper.Visibility = Visibility.Visible;
            }
        }

        private void btn_SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Microsoft.Win32.OpenFileDialog();
            picker.Filter = "Subtitle Files |*.srt";
            picker.Multiselect = true;
            if (!String.IsNullOrEmpty(initialDirectory))
            {
                picker.InitialDirectory = initialDirectory;
            }
            if (picker.ShowDialog() == true)
            {
                stackpanel_SelectedFiles.Children.Clear();
                foreach (var file in picker.FileNames)
                {
                    var text = new TextBlock();
                    text.Text = file;
                    stackpanel_SelectedFiles.Children.Add(text);
                }
                subFiles = picker.FileNames.ToList();
                if (String.IsNullOrEmpty(txt_OutputDir.Text) && picker.FileNames.Length > 0)
                {
                    txt_OutputDir.Text = Path.GetDirectoryName(picker.FileNames[0]);
                }
            }

        }

        private void btn_Convert_Click(object sender, RoutedEventArgs e)
        {
            if (subFiles.Count == 0 || !Int32.TryParse(textbox_Offset.Text, out _))
            {
                return;
            }
            var numCompleted = 0;
            var total = subFiles.Count;
            SetProgressText(numCompleted, total);
            var addToFilename = chkbox_Overwrite.IsChecked != true ? txt_Overwrite.Text : null;
            var outputDir = chkbox_Overwrite.IsChecked != true ? txt_OutputDir.Text : null;
            foreach (var file in subFiles)
            {
                ConvertFile(file, Convert.ToInt32(textbox_Offset.Text), addToFilename, outputDir).ContinueWith((x) =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        numCompleted++;
                        SetProgressText(numCompleted, total);
                    });

                });
            }

        }

        private void SetProgressText(int numCompleted, int total)
        {
            if (numCompleted == total)
            {
                txt_Progress.Text = $"Successfully converted {total} file(s)";
            }
            else
            {
                txt_Progress.Text = $"Converting file {numCompleted + 1} of {total}";
            }
        }

        private Task ConvertFile(string path, int offset, string addToFilename = null, string outputDir = null)
        {
            var reg = new Regex("^[0-9][0-9]:[0-9][0-9]:[0-9][0-9],[0-9][0-9][0-9]");
            return Task.Run(() =>
             {
                 var lines = File.ReadAllLines(path);
                 for (int i = 0; i < lines.Length; i++)
                 {
                     string line = lines[i];
                     var lineSplit = line.Split(' ');
                     if (reg.IsMatch(line))
                     {
                         var start = lineSplit[0];
                         start = start.Replace(",", ".");
                         var span = TimeSpan.Parse(start);
                         span = AddOffset(span, offset);
                         lineSplit[0] = span.ToString("hh\\:mm\\:ss\\,fff");

                         var end = lineSplit[2];
                         end = end.Replace(",", ".");
                         span = TimeSpan.Parse(end);
                         span = AddOffset(span, offset);
                         lineSplit[2] = span.ToString("hh\\:mm\\:ss\\,fff");
                         line = String.Join(" ", lineSplit);
                         lines[i] = line;
                     }
                 }
                 var basename = Path.GetFileNameWithoutExtension(path);
                 if (addToFilename != null)
                 {
                     basename += addToFilename;
                 }
                 string fullpath;
                 if (!String.IsNullOrEmpty(outputDir))
                 {
                     fullpath = Path.Combine(outputDir, basename + ".srt");
                 }
                 else
                 {
                     fullpath = Path.Combine(Path.GetDirectoryName(path), basename + ".srt");

                 }
                 File.WriteAllLines(fullpath, lines);
             });
        }

        private TimeSpan AddOffset(TimeSpan time, int offset)
        {
            if (offset > 0)
            {
                return time.Add(TimeSpan.FromMilliseconds(offset).Duration());
            }
            else
            {
                return time.Subtract(TimeSpan.FromMilliseconds(offset).Duration());
            }
        }

        private void chkbox_Overwrite_Click(object sender, RoutedEventArgs e)
        {
            if (chkbox_Overwrite.IsChecked != true)
            {
                cont_OutputWrapper.Visibility = Visibility.Visible;
            }
            else
            {
                cont_OutputWrapper.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (subFiles.Count > 0)
            {
                Settings.Default.file_path = Path.GetDirectoryName(subFiles[0]);
            }
            Settings.Default.overwrite_checked = chkbox_Overwrite.IsChecked == true;
            Settings.Default.overwrite_string = txt_Overwrite.Text;
            if (Int32.TryParse(textbox_Offset.Text, out int n))
            {
                Settings.Default.offset = n;
            }

            Settings.Default.Save();
        }

        private void textbox_Offset_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Int32.TryParse(e.Text, out _) && e.Text != "-";
        }

        private void btn_Offset_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            if (Int32.TryParse(textbox_Offset.Text, out int n))
            {
                var offsetToAdd = Convert.ToInt32(button.CommandParameter);
                n += offsetToAdd;
                textbox_Offset.Text = n.ToString();
            }
        }

        private void textbox_Offset_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(textbox_Offset.Text))
            {
                textbox_Offset.Text = "0";
            }
        }

        private void btn_SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    txt_OutputDir.Text = dialog.SelectedPath;
                }
            }

        }
    }
}
