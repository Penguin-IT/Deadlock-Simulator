using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Deadlock_simulator.ViewModels
{
    public class BaseViewModel:INotifyPropertyChanged
    {
        // Sự kiện PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            // Kiểm tra xem có handler nào đăng ký chưa
            if (PropertyChanged != null)
            {
                // Tạo đối tượng EventArgs với tên property
                PropertyChangedEventArgs args =
                    new PropertyChangedEventArgs(propertyName);
                // Gọi tất cả các handler đã đăng ký
                PropertyChanged(this, args);
            }
        }
    }
}
