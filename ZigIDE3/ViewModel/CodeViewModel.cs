using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

            CodeClipboardCommand = new RelayCommand(ExecuteClipboardCommand);

            if (Directory.Exists(path))
            {
                try
                {
                    LoadFilesFromZigPathAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }


        // todo clipboard implementieren
        private void ExecuteClipboardCommand(object obj)
        {
            try
            {
                var option = obj.ToString();
                switch(option)
                {
                    case "Cut":
                        var text = "Elvira Hugendubel";
                        Clipboard.SetText(text);
                        break;
                    case "Copy":
                        var text2 = "Elvira Hugendubel";
                        Clipboard.SetText(text2);
                        break;
                    case "Paste":
                        var insertText = Clipboard.GetText();
                        SourceCode += insertText;
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ExecuteLoadFileCommand(object obj)
        {
            var zigPath = Settings.Default.ZigPath;

            this.ZigFilename = (obj as FileInfo).Name;
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

        public string Status => Settings.Default.Status;

        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand SelectionChangedCommand { get; }
        
        public ICommand CodeClipboardCommand { get; }

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
            await Task.Delay(750);

            this.dateiListe = list;

            OnPropertyChanged(nameof(DateiListe));
        }

        private IEnumerable<FileInfo> loadFilesFromZigPath(string path)
        {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles(); // Dateien im Ordner

                var zigFiles = files.Where(item => item.Name.EndsWith(".zig"));
            
                return zigFiles;
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

        public void Paste()
        {

        }

        public void Save()
        {
            saveSourceCode();
        }

        public async void Compile()
        {
            Settings.Default.Status = "...";
            OnPropertyChanged(nameof(Status));

            this.Output=string.Empty;

            var releaseArgument= " -O " + Settings.Default.ReleaseType;
            var arguments = " build-exe " + this.ZigFilename + " " + releaseArgument;

            var ausgabe = await StarteProzessMitArgumentenUndLeseAusgabeAsync("zig", arguments);

            if (ausgabe.Item2.Equals(string.Empty))
            {
                this.Errors = null;
                Settings.Default.ZigExeFilename = getExeNameFromZigFile(this.ZigFilename);
                OnPropertyChanged(nameof(ZigExeFilename));
                // todo benachrichtigen, dass das Kompilieren geklappt hat
                Settings.Default.Status = "OK";
                OnPropertyChanged(nameof(Status));
            }
            else
            {
                this.Errors = ausgabe.Item2;
            }
            Console.WriteLine("Error: " + ausgabe.Item2);
        }

        private string getExeNameFromZigFile(string zigFilename)
        {
            var pos = zigFilename.IndexOf(".zig");
            var exeName = zigFilename.Substring(0, pos) + ".exe";
            return exeName;
        }
        
        public async void Run()
        {
            var arguments = "";
            var exeFile = Path.Combine(Settings.Default.ZigPath, Settings.Default.ZigExeFilename);
            var result = await StarteProzessMitArgumentenUndLeseAusgabeAsync(exeFile, arguments);

            this.Output = result.Item1;
            
            Console.WriteLine("Output: " + result.Item1);
            Console.WriteLine("Fehler: " + result.Item2);
        }

        public string ZigExeFilename => Settings.Default.ZigExeFilename;
        
        public async Task<Tuple<string, string>> StarteProzessMitArgumentenUndLeseAusgabeAsync(string pfadZumProgramm, string argumente)
        {
            Thread.Sleep(1000);

            // Konfiguriere die Startinformationen des Prozesses
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WorkingDirectory = Settings.Default.ZigPath,
                FileName = pfadZumProgramm,
                Arguments = argumente,
                UseShellExecute = false, // Ermöglicht die Umleitung der Standardausgabe
                RedirectStandardOutput = true, // Leitet die Standardausgabe um, sodass sie gelesen werden kann
                RedirectStandardError = true,
                CreateNoWindow = true // Verhindert das Erstellen eines Fensters für den Prozess
            };

            // Erstelle und starte den Prozess
            using (Process prozess = new Process())
            {
                prozess.StartInfo = startInfo;

                prozess.Start();

                // Lese die Standardausgabe des Prozesses
                var output = await prozess.StandardOutput.ReadToEndAsync();
                var error = await prozess.StandardError.ReadToEndAsync();

                // Warte, bis der Prozess beendet ist
                prozess.WaitForExit();

                return new Tuple<string, string>(output,error);
            }
        }

        private IEnumerable<FileInfo> dateiListe= new List<FileInfo>();
        private string _zigFilename;
        private string _output="default";
        private string _errors;
        private string _status;
        private string _zigExeFilename;
        
        public IEnumerable<FileInfo> DateiListe => dateiListe;

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

            return;
        }

        public async void StartTestRunner()
        {
            var absoluteZigFile = Path.Combine(Settings.Default.ZigPath, Settings.Default.CurrentZigFilename);

            var arguments = " test " + absoluteZigFile;
            // var exeFile = Path.Combine(Settings.Default.);
            var result = await StarteProzessMitArgumentenUndLeseAusgabeAsync("zig", arguments);

            this.Output = result.Item2;
            //this.Errors = result.Item2;

            Console.WriteLine("Output: " + result.Item1);
            // Console.WriteLine("Fehler: " + result.Item2);

            this.Errors = null;
        }

        public void Drop(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    try
                    {
                        string text = File.ReadAllText(files[0]);
                        this.SourceCode += text;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Fehler beim Lesen der Datei: " + ex.Message);
                    }
                }
            }

        }

        public void DragEnter(DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            // Überprüfen, ob die gezogenen Daten Dateien sind
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files)
                {
                    if (file.EndsWith(".zig")) e.Effects = DragDropEffects.Copy;
                    if (file.EndsWith(".txt")) e.Effects = DragDropEffects.Copy;
                }
                // Erlaubt dem Benutzer, die Datei hier abzulegen (zeigt einen Copy-Cursor)
            }
            else
            {
                // Nicht erlaubte Aktion (zeigt einen "Verboten"-Cursor)
            }
            e.Handled = true; // Gibt an, dass das Ereignis behandelt wurde
        }

        internal void ChangeZigPath(string nachricht)
        {
            throw new NotImplementedException();
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