using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZigIDE3.Properties;

namespace ZigIDE3.ViewModel
{
    public class StatusViewModel : INotifyPropertyChanged
    {
        public StatusViewModel()
        {
            zigPath = Settings.Default.ZigPath;
        }

        public string zigPath;
        public string ZigPath => zigPath;

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

        public void ChangeZigPath(string nachricht)
        {
            SetField(ref zigPath, nachricht, nameof(ZigPath));
        }
    }
}