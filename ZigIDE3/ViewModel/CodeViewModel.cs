using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ZigIDE3.Properties;
using ZigIDE3.Tool;

namespace ZigIDE3.ViewModel
{
    public class CodeViewModel : INotifyPropertyChanged
    {
        public CodeViewModel()
        {
            var path = Settings.Default.ZigPath;

            SelectionChangedCommand = new RelayCommand(ExecuteLoadFileCommand);

            if (Directory.Exists(path))
            {
                LoadFilesFromZigPathAsync();
            }
        }

        private void ExecuteLoadFileCommand(object obj)
        {
            var zigPath = Settings.Default.ZigPath;

            this.ZigFilename = obj as string;
            Settings.Default.CurrentZigFilename = this.ZigFilename;

            var filePath = Path.Combine(zigPath, ZigFilename);
            this.SourceCode = loadZigFile(filePath);
        }

        public string ZigFilename { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand SelectionChangedCommand { get; }
        
        private string _sourceCode;
        private string _output;

        public string SourceCode
        {
            get { return _sourceCode; }
            set
            {
                if (_sourceCode != value)
                {
                    _sourceCode = value;
                    OnPropertyChanged(nameof(SourceCode));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void LoadFilesFromZigPathAsync()
        {
            var zigPath = Settings.Default.ZigPath;
            var list = loadFilesFromZigPath(zigPath);
            await Task.Delay(500);

            this.dateiListe = list.ToList();

            OnPropertyChanged(nameof(DateiListe));
        }

        private IEnumerable<string> loadFilesFromZigPath(string path)
        {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles(); // Dateien im Ordner

                var list= new List<string>();

                foreach (FileInfo file in files)
                {
                    list.Add(file.Name);
                }

                return list;
        }

        private string loadZigFile(string filePath)
        {
            try
            {
                string fileContent = File.ReadAllText(filePath);
                return fileContent;
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Ein Fehler ist aufgetreten beim Lesen der Datei: {ex.Message}");
            }

            return null;
        }

        public async void Compile()
        {
            string ausgabe = await StarteProzessMitArgumentenUndLeseAusgabeAsync("zig", "build-exe " + ZigFilename );
            Console.WriteLine(ausgabe);
        }

        public async Task<string> StarteProzessMitArgumentenUndLeseAusgabeAsync(string pfadZumProgramm, string argumente)
        {
            // Konfiguriere die Startinformationen des Prozesses
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = pfadZumProgramm,
                Arguments = argumente,
                UseShellExecute = false, // Ermöglicht die Umleitung der Standardausgabe
                RedirectStandardOutput = true, // Leitet die Standardausgabe um, sodass sie gelesen werden kann
                CreateNoWindow = true // Verhindert das Erstellen eines Fensters für den Prozess
            };

            // Erstelle und starte den Prozess
            using (Process prozess = new Process())
            {
                prozess.StartInfo = startInfo;

                prozess.Start();

                // Lese die Standardausgabe des Prozesses
                string ausgabe = await prozess.StandardOutput.ReadToEndAsync();

                // Warte, bis der Prozess beendet ist
                prozess.WaitForExit();

                return ausgabe;
            }
        }

        private IList<string> dateiListe= new List<string>();
        public IList<string> DateiListe => dateiListe;
    }
}