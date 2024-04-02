using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ZigIDE3
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            
            this.Title = "ZigIDE3 by Stefan Brandt";
            this.Loaded += Window_Loaded;
            this.LocationChanged += Windows_Changed;

            this.DataContext = this;
        }
        
        private void Windows_Changed(object sender, EventArgs e)
        {
            var window = sender as MainWindow;

            Properties.Settings.Default.Left = window.Left;
            Properties.Settings.Default.Top = window.Top;

            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ZigPat
            var path= Properties.Settings.Default.ZigPath;

            LoadFilesFromDirectory(path);

            var top = Properties.Settings.Default.Top;
            var left = Properties.Settings.Default.Left;

            this.Top = top;
            this.Left = left;

        }

        private void FilesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Content));
        }

        private void LoadFilesFromDirectory(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles(); // Dateien im Ordner

                FilesListBox.Items.Clear(); // Bestehende Einträge löschen

                foreach (FileInfo file in files)
                {
                    FilesListBox.Items.Add(file.Name); // Dateiname zur ListBox hinzufügen
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Fehler beim Laden der Dateien: {ex.Message}");
            }
        }
        
        public void Beenden_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void Info_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://ziglang.org";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Webseite: {ex.Message}");
            }
        }

        static void p(string path)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "zig version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }
        
        public void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            string path = @"C:\Users\Stefa\source\repos\zig3\" + FilesListBox.SelectedItem.ToString();

            try
            {
                p(path);
                //Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Datei: {ex.Message}");
            }
        }

        #region Properties

        public string Content
        {
            get;
            set;
        }

        public IEnumerable<string> CommandItems => GetCommands();

        private IEnumerable<string> GetCommands()
        {
            return new string[] { "Apfel", "Birne"};
        }

        public string Content1 => GetContent();

        private string GetContent()
        {
            string content=string.Empty;

            try
            {
                var filePath = FilesListBox.SelectedValue.ToString();
                var absolutePath = Path.Combine(Properties.Settings.Default.ZigPath, filePath);
                content = File.ReadAllText(absolutePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Dateien: {ex.Message}");
                content = string.Empty;
            }
            finally
            {
            }

            return content;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
