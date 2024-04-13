using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        
        public string ZigFilename
        {
            get => _zigFilename;
            set
            {
                if (value == _zigFilename) return;
                _zigFilename = value;
                OnPropertyChanged(nameof(ZigFilename));
                OnPropertyChanged(nameof(ZigFilePath));
            }
        }

        public string ZigFilePath => Path.Combine(Settings.Default.ZigPath, ZigFilename);

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand SelectionChangedCommand { get; }
        
        private string _sourceCode;

        public string Errors
        {
            get => _errors;
            set
            {
                if (value == _errors) return;
                _errors = value;
                OnPropertyChanged(nameof(Errors));
            }
        }

        public string SourceCode
        {
            get { return _sourceCode; }
            set
            {
                if (_sourceCode != value)
                {
                    _sourceCode = value;
                    OnPropertyChanged(nameof(SourceCode));
                    OnPropertyChanged(nameof(SourceCode));
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

        public async void Save()
        {
            saveSourceCode();
        }

        public async void Compile()
        {
            var releaseArgument= " -O " + Settings.Default.ReleaseType;
            var arguments = " build-exe " + this.ZigFilename + " " + releaseArgument;

            string ausgabe = await StarteProzessMitArgumentenUndLeseAusgabeAsync("zig", arguments);

            if (ausgabe.Equals(string.Empty))
            {
                var exeFile = getExeNameFromZigFile(this.ZigFilename);
                this.ZigExeFilename = exeFile;
                Settings.Default.ZigExeFilename = exeFile;
                // todo benachrichtigen, dass das Kompilieren geklappt hat
            }
            
            Console.WriteLine(ausgabe);
        }

        private string getExeNameFromZigFile(string zigFilename)
        {
            var pos = zigFilename.IndexOf(".zig");
            var exeName = zigFilename.Substring(0, pos) + ".exe";
            return exeName;
        }

        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public async void Run()
        {
            //var arguments = this.ZigExeFilename;

            var arguments = "";
            var exeFile = Settings.Default.ZigExeFilename;

            string ausgabe = await StarteProzessMitArgumentenUndLeseAusgabeAsync(exeFile, arguments);

            this.Output = ausgabe;
            
            Console.WriteLine(ausgabe);
        }

        public string ZigExeFilename
        {
            get => _zigExeFilename;
            set
            {
                if (value == _zigExeFilename) return;
                _zigExeFilename = value;
                OnPropertyChanged(nameof(ZigExeFilename));
            }
        }

        public async Task<string> StarteProzessMitArgumentenUndLeseAusgabeAsync(string pfadZumProgramm, string argumente)
        {
            // Konfiguriere die Startinformationen des Prozesses
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WorkingDirectory = Settings.Default.ZigPath,
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
        private string _zigFilename;
        private string _output="c";
        private string _errors;
        private string _status;
        private string _zigExeFilename;
        public IList<string> DateiListe => dateiListe;

        public string Output
        {
            get => _output;
            set
            {
                if (value == _output) return;
                _output = value;
                OnPropertyChanged(nameof(Output));
            }
        }

        private void saveSourceCode()
        {
            var currentFile = Settings.Default.CurrentZigFilename;
            if (string.IsNullOrEmpty(currentFile)) return;
            
            try
            {
                var path = Path.Combine(Settings.Default.ZigPath, currentFile);

                using (StreamWriter sw = new StreamWriter(path))
                {
                    var buffer = this.SourceCode.ToCharArray();
                    sw.WriteAsync(buffer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ein Fehler ist aufgetreten: {ex.Message}");
            }
        }

    }

    /// <summary></summary>
    [Obsolete()]
    public class ProcessRunner
    {
        public static string RunDOSProgram(string programPath, string arguments = "")
        {
            // Initialisieren eines neuen ProzessStartInfo-Objektes
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = programPath, // Der Pfad zum DOS-Programm
                Arguments = arguments, // Zusätzliche Argumente, falls erforderlich
                UseShellExecute = false, // Nicht die Shell verwenden, um den Prozess zu starten
                RedirectStandardOutput = true, // Umleitung der Standardausgabe aktivieren
                RedirectStandardError = true, // Umleitung der Standardfehlerausgabe aktivieren
                CreateNoWindow = true, // Verhindert die Erstellung eines Fensters
                StandardOutputEncoding = Encoding.UTF8, // Kodierung der Standardausgabe
                StandardErrorEncoding = Encoding.UTF8 // Kodierung der Standardfehlerausgabe
            };

            // Starten des Prozesses mit den zuvor definierten Startinformationen
            using (Process process = Process.Start(startInfo))
            {
                // Lesen der Standardausgabe
                string output = process.StandardOutput.ReadToEnd();
                // Warten, bis der Prozess beendet ist
                process.WaitForExit();

                // Optional: Lesen der Fehlerausgabe
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Error during process execution: {error}");
                }

                return output;
            }
        }
    }
}