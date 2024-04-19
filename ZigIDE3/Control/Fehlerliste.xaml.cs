using System.Windows;
using System.Windows.Controls;

namespace ZigIDE3.Control
{
    /// <summary>Interaktionslogik für Fehlerliste.xaml</summary>
    public partial class Fehlerliste : UserControl
    {
        public Fehlerliste()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ErrorsProperty = DependencyProperty.Register(
            "Errors", typeof(string), typeof(Fehlerliste), new PropertyMetadata(default(string)));

        public string Errors
        {
            get { return (string)GetValue(ErrorsProperty); }
            set { SetValue(ErrorsProperty, value); }
        }

    }
}
