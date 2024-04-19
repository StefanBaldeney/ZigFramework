using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace ZigIDE3.Control
{
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
                if (string.IsNullOrEmpty(zeile)) 
                    break;

                var message = new Fehlermeldung();

                string[] spalten = zeile.Split(new[] { ":" }, StringSplitOptions.None);

                switch (spalten.Length)
                {
                    case 0: break;
                    case 1:
                    case 2:
                        message.Spalte = "";
                        message.Zeile = "";
                        message.Message = zeile;
                        break;
                    case 3:
                    case 4:
                        message.Message = zeile[0].ToString();
                        break;
                    case 5:
                        message.Zeile = spalten[1].ToString();
                        message.Spalte = spalten[2].ToString();
                        message.Message = spalten[4].ToString();
                        break;
                    default:
                        message.Zeile = "";
                        message.Spalte = "";
                        message.Message = zeile;
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
}