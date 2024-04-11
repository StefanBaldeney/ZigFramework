// © Stefan Brandt

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using ZigIDE3.Properties;
using ZigIDE3.Tool;

namespace ZigIDE3.ViewModel
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        public ICommand MenuBeendenCommand { get; }
        public ICommand MenuOptionsCommand { get; }
        public ICommand MenuCompileCommand { get; }
        public ICommand MenuRunCommand { get; }

        #region  ReleaseTypes
        public ICommand MenuOptionDebugCommand { get; }
        public ICommand MenuOptionReleaseFastCommand { get; }
        public ICommand MenuOptionReleaseSmallCommand { get; }
        public ICommand MenuOptionReleaseSafeCommand { get; }

        #endregion


        #region Events

        public event EventHandler<MyEventArgs> ZigPathChanged;
        public event EventHandler<MyEventArgs> ZigFileCompile;

        protected virtual void OnMeinEventMitDaten(MyEventArgs e)
        {
            ZigPathChanged?.Invoke(this, e);
        }

        #endregion

        public MenuViewModel()
        {
            MenuBeendenCommand = new RelayCommand(ExecuteBeendenCommand);
            MenuOptionsCommand = new RelayCommand(ExecuteOptionsCommand);
            MenuCompileCommand = new RelayCommand(ExecuteCompileCommand);

            MenuRunCommand = new RelayCommand(ExecuteRunCommand);

            MenuOptionDebugCommand = new RelayCommand(ExecuteDebugCommand);
            MenuOptionReleaseFastCommand = new RelayCommand(ExecuteReleaseFast);
            MenuOptionReleaseSmallCommand = new RelayCommand(ExecuteReleaseSmall);
            MenuOptionReleaseSafeCommand = new RelayCommand(ExecuteReleaseSafe);
        }

        private void ExecuteReleaseSafe(object obj)
        {
            Settings.Default.ReleaseType = "ReleaseSafe";
        }

        private void ExecuteReleaseSmall(object obj)
        {
            Settings.Default.ReleaseType = "ReleaseSmall";
        }

        private void ExecuteReleaseFast(object obj)
        {
            Settings.Default.ReleaseType = "ReleaseFast";
        }

        private void ExecuteDebugCommand(object obj)
        {
            Settings.Default.ReleaseType = "Debug";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ExecuteBeendenCommand(object parameter)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ExecuteOptionsCommand(object parameter)
        {
            var dialog = new FolderBrowserDialog();

            // var di = DriveInfo.GetDrives();

            var path = Settings.Default.ZigPath;

            // dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            
            dialog.SelectedPath = path; 

            var result = dialog.ShowDialog();
            // Dateiauswahlfenster
            if (result == DialogResult.OK)
            {
                Settings.Default.ZigPath = dialog.SelectedPath;
                OnMeinEventMitDaten(new MyEventArgs() { Nachricht = dialog.SelectedPath });
            }

        }

        private void ExecuteRunCommand(object parameter)
        {
            var args = string.Empty;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WorkingDirectory = Properties.Settings.Default.ZigPath,
                FileName = "hello_world.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine("Output: " + result);
                    this.Output = result;
                }

                using (StreamReader reader = process.StandardError)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine("Error: " + result);
                    this.Error = result;
                }

            }
        }

        private void ExecuteCompileCommand(object parameter)
        {
            ZigFileCompile?.Invoke(this, new MyEventArgs());
            return;

            var args = string.Empty;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WorkingDirectory = Properties.Settings.Default.ZigPath,
                FileName = "zig",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine("Output: " + result);
                    this.Output = result;
                }

                using (StreamReader reader = process.StandardError)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine("Error: " + result);
                    this.Error = result;
                }
                
            }
        }

        public string Output { get; set; }
        public string Error { get; set; }
    }
}
