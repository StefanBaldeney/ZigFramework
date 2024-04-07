using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ZigIDE3.Properties;

namespace ZigIDE3.ViewModel
{
    public class CodeViewModel : INotifyPropertyChanged
    {
        public CodeViewModel()
        {
            var path = Settings.Default.ZigPath;

            if (Directory.Exists(path))
            {
                LoadFilesFromZigPathAsync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        private IList<string> dateiListe= new List<string>();
        public IList<string> DateiListe => dateiListe;
    }
}