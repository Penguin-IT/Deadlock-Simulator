using Deadlock_simulator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Deadlock_simulator.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // ===== INPUT =====
        private string _newProcessName;
        public string NewProcessName
        {
            get => _newProcessName;
            set
            {
                _newProcessName = value;
                OnPropertyChanged(nameof(NewProcessName));
            }
        }

        private string _newResourceName;
        public string NewResourceName
        {
            get => _newResourceName;
            set
            {
                _newResourceName = value;
                OnPropertyChanged(nameof(NewResourceName));
            }
        }

        // ===== DATA =====
        public ObservableCollection<MatrixRow> AllocationMatrix { get; set; } = new();
        public ObservableCollection<MatrixRow> MaxMatrix { get; set; } = new();
        public ObservableCollection<DataGridColumn> AllocationColumns { get; set; } = new();
        public ObservableCollection<DataGridColumn> MaxColumns { get; set; } = new();
        public ObservableCollection<string> Resources { get; set; } = new();
        public ObservableCollection<ResourceValue> Available { get; set; } = new();

        // ===== COMMAND =====
        public ICommand AddProcessCommand { get; }
        public ICommand AddResourceCommand { get; }

        // ===== CONSTRUCTOR =====
        public MainViewModel()
        {
            AddProcessCommand = new RelayCommand(AddProcess, null);
            AddResourceCommand = new RelayCommand(AddResource, null);

            BuildColumns();
        }

        // ===== BUILD COLUMNS =====
        private void BuildColumns()
        {
            AllocationColumns.Clear();
            MaxColumns.Clear();

            // ===== CỘT PROCESS =====
            var processCol = new DataGridTextColumn
            {
                Header = "Process",
                Binding = new Binding("ProcessName"),
                Width = 120
            };

            AllocationColumns.Add(processCol);
            MaxColumns.Add(new DataGridTextColumn
            {
                Header = "Process",
                Binding = new Binding("ProcessName"),
                Width = 120
            });

            // ===== CỘT RESOURCE =====
            for (int i = 0; i < Resources.Count; i++)
            {
                int index = i;

                var col1 = new DataGridTextColumn
                {
                    Header = Resources[index],
                    Binding = new Binding($"Values[{index}].Value")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };

                var col2 = new DataGridTextColumn
                {
                    Header = Resources[index],
                    Binding = new Binding($"Values[{index}].Value")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    }
                };

                AllocationColumns.Add(col1);
                MaxColumns.Add(col2);
            }
        }

        // ===== ADD RESOURCE =====
        private void AddResource(object ob)
        {
            if (string.IsNullOrWhiteSpace(NewResourceName)) return;

            if (Resources.Any(r => r.Equals(NewResourceName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Resource đã tồn tại!!!");
                return;
            }
            Resources.Add(NewResourceName);
            // Allocation
            foreach (var row in AllocationMatrix)
                row.Values.Add(new ResourceValue { Value = 0 });
            // Max
            foreach (var row in MaxMatrix)
                row.Values.Add(new ResourceValue { Value = 0 });
            // Available
            Available.Add(new ResourceValue
            {
                Name = NewResourceName,
                Value = 0
            });
            BuildColumns();
            NewResourceName = "";
        }

        // ===== ADD PROCESS =====
        private void AddProcess(object ob)
        {
            if (string.IsNullOrWhiteSpace(NewProcessName)) return;

            if (AllocationMatrix.Any(p => p.ProcessName.Equals(NewProcessName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Process đã tồn tại!!!");
                return;
            }
            var row1 = new MatrixRow { ProcessName = NewProcessName };
            var row2 = new MatrixRow { ProcessName = NewProcessName };
            for (int i = 0; i < Resources.Count; i++)
            {
                row1.Values.Add(new ResourceValue { Value = 0 });
                row2.Values.Add(new ResourceValue { Value = 0 });
            }
            AllocationMatrix.Add(row1);
            MaxMatrix.Add(row2);
            NewProcessName = "";
        }
    }
}