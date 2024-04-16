using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.WebSockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

    public class ZigErrorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;

            string[] zeilen = value.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var errorList = new List<Fehlermeldung>();

            var header = new Fehlermeldung();
            header.Zeile = "Zeile";
            header.Spalte = "Spalte";
            header.Message = "Fehlermeldung";
            errorList.Add(header);

            foreach (var zeile in zeilen)
            {
                var message = new Fehlermeldung();

                switch (zeile.Length)
                {
                    case 0: break;
                    case 1:
                        message.Message = zeile;
                        break;
                    case 2:
                        message.Message = zeile[0].ToString() + zeile[1].ToString();
                        break;
                    case 3:
                        message.Message = zeile[0].ToString();
                        break;
                    case 4:
                        message.Message = zeile[0].ToString();
                        break;
                    case 5:
                        message.Zeile = zeile[1].ToString();
                        message.Spalte = zeile[2].ToString();
                        message.Message = zeile[4].ToString();
                        break;
                    default:
                        break;
                }

                errorList.Add(message);
            }

            return errorList;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Fehlermeldung
    {
        public string Zeile { get; set; } ="1";
        public string Spalte { get; set; } = "0";
        public string Message { get; set; } = "type mismatch";
    }



}
