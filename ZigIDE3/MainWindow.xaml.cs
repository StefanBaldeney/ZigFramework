using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Document;
using ZigIDE3.Properties;
using ZigIDE3.ViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Windows.Threading;
using System.Linq;

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

            // folding
            
            // Annehmen, dass "textEditor" Ihre Instanz von TextEditor ist
            _foldingManager = FoldingManager.Install(Avalon.TextArea);
            _foldingUpdateTimer = new DispatcherTimer();
            
            Avalon.CaretOffset = 0;
            Avalon.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            Avalon.Drop += Avalon_Drop;
            Avalon.DragEnter += AvalonOnDragEnter;

            SetupFolding();

        }

        void SetupFolding()
        {
            _foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            _foldingUpdateTimer.Tick += (sender, e) => UpdateFoldings();
            _foldingUpdateTimer.Start();
        }
        
        void UpdateFoldings()
        {
            var newFoldings = new List<NewFolding>();
            var document = Avalon.Document;
            var startOffsets = new Stack<int>();
            var lineStarts = new Stack<int>();

            for (int offset = 0; offset < document.TextLength; offset++)
            {
                char c = document.GetCharAt(offset);
                if (c == '{')
                {
                    startOffsets.Push(offset);
                    lineStarts.Push(document.GetLineByOffset(offset).LineNumber);
                }
                else if (c == '}' && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    int startLine = lineStarts.Pop();
                    int endLine = document.GetLineByOffset(offset).LineNumber;

                    if (endLine > startLine) // Ensure that the braces span more than one line
                    {
                        newFoldings.Add(new NewFolding(startOffset, offset + 1));
                    }
                }
            }

            // Sort the foldings by start offset
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

            _foldingManager.UpdateFoldings(newFoldings, -1);
        }

        #region ChatGPT_Folding

        public class FoldingState
        {
            public int StartOffset { get; set; }
            public int EndOffset { get; set; }
            public bool IsFolded { get; set; }
        }

        void SerializeFoldingStates(string filePath, List<FoldingState> states)
        {
            // todo Json
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(states, options);
            File.WriteAllText(filePath, jsonString);
        }

        List<FoldingState> DeserializeFoldingStates(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Deserialize<List<FoldingState>>(jsonString, options);
        }

        void AllesEinklappen()
        {
            // Zum Einklappen aller Abschnitte
            foreach (var folding in _foldingManager.AllFoldings)
            {
                folding.IsFolded = true;
            }
        }

        void AllesAusklappen()
        {
            foreach (var folding in _foldingManager.AllFoldings)
            {
                folding.IsFolded = false;
            }
        }


        void ApplyFoldingStates(FoldingManager manager, List<FoldingState> states)
        {
            manager.UpdateFoldings(states.Select(s => new NewFolding(s.StartOffset, s.EndOffset)
            {
                // IsFolded = s.IsFolded
            }).ToList(), -1);
        }

        List<FoldingState> CaptureFoldingStates(FoldingManager manager)
        {
            var states = new List<FoldingState>();
            foreach (var folding in manager.AllFoldings)
            {
                states.Add(new FoldingState
                {
                    StartOffset = folding.StartOffset,
                    EndOffset = folding.EndOffset,
                    IsFolded = folding.IsFolded
                });
            }
            return states;
        }

        #endregion

        private void AvalonOnDragEnter(object sender, DragEventArgs e)
        {
            vmCode.DragEnter(e);
        }

        private void Avalon_Drop(object sender, DragEventArgs e)
        {
            vmCode.Drop(e);
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
            vmCode.ChangeZigPath(e.Nachricht);
            //vmStatus.ChangeZigPath(e.Nachricht);
        }
        private void VmMenu_ZigCompile(object sender, MyEventArgs e)
        {
            // todo async problem

            switch (e.Nachricht?.ToLower())
            {
                case "releasefast":
                    Settings.Default.ReleaseType = "ReleaseFast";
                    break;
                case "releasesmall":
                    Settings.Default.ReleaseType = "ReleaseSmall";
                    break;
                case "releasesafe":
                    Settings.Default.ReleaseType = "ReleaseSafe";
                    break;
                default:
                    Settings.Default.ReleaseType = "Debug";
                    break;
            }

            vmCode.Compile();
            //vmStatus.UpdateAll();
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
        //private StatusViewModel vmStatus;
        private CodeViewModel vmCode;
        
        private FoldingManager _foldingManager;
        private DispatcherTimer _foldingUpdateTimer;

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
    
    //public class BraceFoldingStrategy : AbstractFoldingStrategy
    //{
    //    public override IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
    //    {
    //        firstErrorOffset = -1;
    //        List<NewFolding> newFoldings = new List<NewFolding>();

    //        var startOffsets = new Stack<int>();
    //        for (int i = 0; i < document.TextLength; i++)
    //        {
    //            char c = document.GetCharAt(i);
    //            if (c == '{')
    //            {
    //                startOffsets.Push(i);
    //            }
    //            else if (c == '}' && startOffsets.Count > 0)
    //            {
    //                int startOffset = startOffsets.Pop();
    //                newFoldings.Add(new NewFolding(startOffset, i + 1));
    //            }
    //        }

    //        newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
    //        return newFoldings;
    //    }
    //}

}
