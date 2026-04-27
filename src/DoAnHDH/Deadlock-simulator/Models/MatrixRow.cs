using System.Collections.ObjectModel;
using System.ComponentModel;

public class MatrixRow : INotifyPropertyChanged
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


    private string _processName;
    public string ProcessName
    {
        get => _processName;
        set
        {
            _processName = value;
            OnPropertyChanged(nameof(ProcessName));
        }
    }

    public ObservableCollection<int> Values { get; set; } = new();
}