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
        public ObservableCollection<string> Resources { get; set; } = new();
        public ObservableCollection<MatrixRow> Matrix { get; set; } = new();
        public ObservableCollection<DataGridColumn> Columns { get; set; } = new();

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
            Columns.Clear();

            // Process column
            Columns.Add(new DataGridTextColumn
            {
                Header = "Process",
                Binding = new Binding("ProcessName"),
                Width = 120,

                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            });

            // Resource columns
            for (int i = 0; i < Resources.Count; i++)
            {
                int index = i;

                Columns.Add(new DataGridTextColumn
                {
                    Header = Resources[index],
                    Width = 60,

                    Binding = new Binding($"Values[{index}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },

                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
                        {
                            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center),
                            new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                        }
                    },

                    EditingElementStyle = new Style(typeof(TextBox))
                    {
                        Setters =
                        {
                            new Setter(TextBox.TextAlignmentProperty, TextAlignment.Center)
                        }
                    }
                });
            }

            OnPropertyChanged(nameof(Columns)); 
        }

        // ===== ADD RESOURCE =====
        private void AddResource(object ob)
        {
            if (string.IsNullOrWhiteSpace(NewResourceName)) return;

            Resources.Add(NewResourceName);

            // thêm cột vào tất cả process
            foreach (var row in Matrix)
            {
                row.Values.Add(0);
            }

            BuildColumns(); // Cập nhật UI

            NewResourceName = "";
        }

        // ===== ADD PROCESS =====
        private void AddProcess(object ob)
        {
            if (string.IsNullOrWhiteSpace(NewProcessName)) return;

            var row = new MatrixRow
            {
                ProcessName = NewProcessName
            };

            // tạo số cột = số resource
            for (int i = 0; i < Resources.Count; i++)
            {
                row.Values.Add(0);
            }

            Matrix.Add(row);

            NewProcessName = "";
        }
    }
}