using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ZigIDE3.ViewModel;

namespace ZigIDE3
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            
            this.Title = "ZigIDE3 by Stefan Brandt";
            this.LocationChanged += Windows_LocationChanged;
            this.Loaded += Window_Loaded;
        }
        
        private void Windows_LocationChanged(object sender, EventArgs e)
        {
            var window = sender as MainWindow;

            Properties.Settings.Default.Left = window.Left;
            Properties.Settings.Default.Top = window.Top;
            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var path= Properties.Settings.Default.ZigPath;

            var top = Properties.Settings.Default.Top;
            var left = Properties.Settings.Default.Left;

            this.Top = Math.Max(0,top);
            this.Left = Math.Max(0, left); 

            vmMenu = MyMenu.DataContext as MenuViewModel;
            vmStatus = MyStatus.DataContext as StatusViewModel;
            vmCode = MyCode.DataContext as CodeViewModel;

            vmMenu.ZigPathChanged += VmMenu_ZigPathChanged;
            vmMenu.ZigFileCompile += VmMenu_ZigCompile;
            vmMenu.ZigFileRun += VmCode_ZigRun;
            
            vmMenu.ZigFileSave += VmMenuOnZigFileSave;

            vmMenu.ZigTestRunner += VmMenuZigTestRunner;

            // Avalon
            string resourceName = "ZigIDE3.avalon.Zig.xshd";

            var assembly = Assembly.GetExecutingAssembly();
            
            // syntax highlighting
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (XmlReader reader = new XmlTextReader(stream))
                    {
                        try
                        {
                            Avalon.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Fehler in avalon syntax highlighting file Zig.xshd");
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Konnte die Syntaxhervorhebungs-Ressource nicht finden: " + resourceName);
                }
            }
        }

        private void VmMenuZigTestRunner(object sender, MyEventArgs e)
        {
            vmCode.StartTestRunner();
        }

        private void VmCode_ZigRun(object sender, MyEventArgs e)
        {
            vmCode.Run();
        }

        private void VmMenuOnZigFileSave(object sender, MyEventArgs e)
        {
            vmCode.Save();
        }

        private void VmMenu_ZigPathChanged(object sender, MyEventArgs e)
        {
            vmStatus.ChangeZigPath(e.Nachricht);
        }
        private void VmMenu_ZigCompile(object sender, MyEventArgs e)
        {
            // todo async problem
            vmCode.Compile();
            vmStatus.UpdateAll();
        }

        private void FilesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            this.ZigFile = e.AddedItems[0].ToString();

            OnPropertyChanged(nameof(SourceCode));
        }

        // file ohne path
        public string ZigFile { get; set; }

        private void LoadFilesFromDirectory(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                FileInfo[] files = dir.GetFiles(); // Dateien im Ordner

                //FilesListBox.Items.Clear(); // Bestehende Einträge löschen

                foreach (FileInfo file in files)
                {
                    //FilesListBox.Items.Add(file.Name); // Dateiname zur ListBox hinzufügen
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
        
        public void _OpenFile_Click(object sender, RoutedEventArgs e)
        {
            //string path = @"C:\Users\Stefa\source\repos\zig3\" + FilesListBox.SelectedItem.ToString();
            string path = string.Empty; // FilesListBox.SelectedItem.ToString();

            try
            {
                // Compile(path);
                // Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen der Datei: {ex.Message}");
            }
        }

        public void Kompilieren_Click(object sender, RoutedEventArgs e)
        {
            var zigin = Properties.Settings.Default.ZigPath;
            var zigout = Properties.Settings.Default.ZigOut;

            var absolutePath = Path.Combine(zigin, this.ZigFile);

            // _Compile(absolutePath);

            // OnPropertyChanged(nameof(ZigOutput));
        }

        #region Properties

        public IEnumerable<string> CommandItems => GetCommands();

        private IEnumerable<string> GetCommands()
        {
            return new string[] { "Apfel", "Birne"};
        }

        private string content;

        private MenuViewModel vmMenu;
        private StatusViewModel vmStatus;
        private CodeViewModel vmCode;
        
        public string SourceCode
        {
            get { return GetContent(); }
            set
            {
                    content = value;
                    SetContent();
                    //OnPropertyChanged(nameof(SourceCode));
            }
        }

        private void SetContent()
        {
            // in die Datei schreiben
        }

        private string GetContent()
        {
            string content=string.Empty;

            try
            {
                var filePath = string.Empty; // FilesListBox.SelectedValue?.ToString();

                if (filePath != null)
                {
                    var absolutePath = Path.Combine(Properties.Settings.Default.ZigPath, filePath);
                    content = File.ReadAllText(absolutePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //MessageBox.Show($"Fehler beim Laden der Dateien: {ex.Message}");
                //content = string.Empty;
            }
            finally
            {
                // nicht hier OnPropertyChanged("Content");
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
