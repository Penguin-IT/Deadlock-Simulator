using Deadlock_simulator.Models;
using Deadlock_simulator.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;

public class MatrixRow : BaseViewModel
{
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

    public ObservableCollection<ResourceValue> Values { get; set; } = new();
}