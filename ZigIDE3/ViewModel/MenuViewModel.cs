using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ZigIDE3.Tool;

namespace ZigIDE3.ViewModel
{
    public class MenuViewModel : INotifyPropertyChanged
    {
        public ICommand MenuBeendenCommand { get; }

        public MenuViewModel()
        {
            MenuBeendenCommand = new RelayCommand(ExecuteBeendenCommand);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ExecuteBeendenCommand(object parameter)
        {
            Application.Current.Shutdown();
        }

    }
}
