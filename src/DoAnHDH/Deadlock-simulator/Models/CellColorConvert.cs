using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Deadlock_simulator.Models
{
    public class CellColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
           
            if (values.Length == 2 && values[0] is MatrixRow row && values[1] is string columnName)
            {
                
                var cellData = row.Values.FirstOrDefault(v => v.Name == columnName);

                if (cellData != null)
                {
                    
                    switch (cellData.Status)
                    {
                        case "Holding": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Xanh
                        case "Waiting": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")); // Vàng
                        case "Deadlock": return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Đỏ
                    }
                }
            }
            return Brushes.Transparent; 
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
