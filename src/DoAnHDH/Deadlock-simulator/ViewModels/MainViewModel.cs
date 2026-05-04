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
using System.IO;
using System.Text.Json;

namespace Deadlock_simulator.ViewModels
{
    public class MainViewModel : BaseViewModel
    {

        private ViewModel1 _coreLogic;
        // ===== INPUT   =====
        private string _selectedProcessName;
        public string SelectedProcessName
        {
            get => _selectedProcessName;
            set { _selectedProcessName = value; OnPropertyChanged(nameof(SelectedProcessName)); }
        }

        private string _selectedResourceName;
        public string SelectedResourceName
        {
            get => _selectedResourceName;
            set { _selectedResourceName = value; OnPropertyChanged(nameof(SelectedResourceName)); }
        }

        private int _requestAmount;
        public int RequestAmount
        {
            get => _requestAmount;
            set { _requestAmount = value; OnPropertyChanged(nameof(RequestAmount)); }
        }

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
        public bool IsSafeModeEnabled
        {
            get => _coreLogic.IsSafeModeEnabled;
            set
            {
                _coreLogic.IsSafeModeEnabled = value;

                OnPropertyChanged(nameof(IsSafeModeEnabled));
            }

        }
        public ObservableCollection<Resource> AvailableResources { get; set; } = new();

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
        public ICommand LoadJsonCommand { get; }
        public ICommand CheckBankerCommand { get; }
        public ICommand DetectDeadlockCommand { get; }
        public ICommand RecoverDeadlockCommand { get; }
        public ICommand RequestResourceCommand { get; }
        // ===== CONSTRUCTOR =====
        public MainViewModel()
        {
            _coreLogic = new ViewModel1();
            AddProcessCommand = new RelayCommand(AddProcess, null);
            AddResourceCommand = new RelayCommand(AddResource, null);
            LoadJsonCommand = new RelayCommand(LoadDataFromJson, null);
            CheckBankerCommand = new RelayCommand(ExecuteCheckBanker, null);
            DetectDeadlockCommand = new RelayCommand(ExecuteDetectDeadlock, null);
            RecoverDeadlockCommand = new RelayCommand(ExecuteRecoverDeadlock, null);
            RequestResourceCommand = new RelayCommand(ExecuteRequestResource, null);
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
            // NEW
            int newResId = _coreLogic.ListResource.Any() ? _coreLogic.ListResource.Max(x => x.ResourceId) + 1 : 1;
            int newHierarchy = _coreLogic.ListResource.Any() ? _coreLogic.ListResource.Max(x => x.HierarchyOrder) + 1 : 1;


            _coreLogic.ListResource.Add(new Resource
            {
                ResourceId = newResId,
                ResourceName = NewResourceName,
                Total = 10,
                IsShareable = false,
                HierarchyOrder = newHierarchy
            });


            foreach (var p in _coreLogic.ListProcess)
            {
                p.Allocation[newResId] = 0;
                p.Max[newResId] = 0;
            }



            Resources.Add(NewResourceName);
            // Allocation
            foreach (var row in AllocationMatrix)
                row.Values.Add(new ResourceValue { Name = NewResourceName, Value = 0 });
            // Max
            foreach (var row in MaxMatrix)
                row.Values.Add(new ResourceValue { Name = NewResourceName, Value = 0 });
            // Available
            Available.Add(new ResourceValue
            {
                Name = NewResourceName,
                Value = 10
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
            int newId = _coreLogic.ListProcess.Any() ? _coreLogic.ListProcess.Max(p => p.ProcessId) + 1 : 1;


            var newProcess = new Process
            {
                ProcessId = newId,
                ProcessName = NewProcessName,
                Allocation = new Dictionary<int, int>(),
                Max = new Dictionary<int, int>()
            };


            foreach (var r in _coreLogic.ListResource)
            {
                newProcess.Allocation[r.ResourceId] = 0;
                newProcess.Max[r.ResourceId] = 0;
            }


            _coreLogic.ListProcess.Add(newProcess);
            var row1 = new MatrixRow { ProcessName = NewProcessName };
            var row2 = new MatrixRow { ProcessName = NewProcessName };
            for (int i = 0; i < Resources.Count; i++)
            {
                row1.Values.Add(new ResourceValue { Name = Resources[i], Value = 0 });
                row2.Values.Add(new ResourceValue { Name = Resources[i], Value = 0 });
            }
            AllocationMatrix.Add(row1);
            MaxMatrix.Add(row2);
            NewProcessName = "";
        }

        private void LoadDataFromJson(object obj)
        {
            try
            {

                _coreLogic.LoadAllData();


                SyncCoreToUI();

                MessageBox.Show("Nạp dữ liệu JSON thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        // Hàm phiên dịch từ RAM  sang UI
        private void SyncCoreToUI()
        {
            Resources.Clear();
            AllocationMatrix.Clear();
            MaxMatrix.Clear();
            AvailableResources.Clear();


            var actualAvailable = _coreLogic.CalculateAvailable();
            foreach (var r in _coreLogic.ListResource)
            {
                var displayRes = new Resource
                {
                    ResourceName = r.ResourceName,
                    HierarchyOrder = r.HierarchyOrder,
                    IsShareable = r.IsShareable,
                    Total = actualAvailable.GetValueOrDefault(r.ResourceId, 0)
                };
                AvailableResources.Add(displayRes);
                Resources.Add(r.ResourceName);
            }

            BuildColumns();

            foreach (var p in _coreLogic.ListProcess)
            {
                var allocRow = new MatrixRow { ProcessName = p.ProcessName };
                var maxRow = new MatrixRow { ProcessName = p.ProcessName };

                foreach (var r in _coreLogic.ListResource)
                {
                    int val = p.Allocation.GetValueOrDefault(r.ResourceId, 0);


                    allocRow.Values.Add(new ResourceValue { Name = r.ResourceName, Value = val });

                    maxRow.Values.Add(new ResourceValue
                    {
                        Name = r.ResourceName,
                        Value = p.Max.GetValueOrDefault(r.ResourceId, 0)
                    });
                }

                AllocationMatrix.Add(allocRow);
                MaxMatrix.Add(maxRow);
            }


            UpdateMatrixColors();
        }
        private void SyncUIToCore()
        {

            foreach (var uiRow in MaxMatrix)
            {
                var coreProcess = _coreLogic.ListProcess.FirstOrDefault(p => p.ProcessName == uiRow.ProcessName);
                if (coreProcess == null) continue;

                foreach (var cell in uiRow.Values)
                {
                    var coreResource = _coreLogic.ListResource.FirstOrDefault(r => r.ResourceName == cell.Name);
                    if (coreResource != null)
                    {

                        coreProcess.Max[coreResource.ResourceId] = cell.Value;
                    }
                }
            }
        }
        private void ExecuteCheckBanker(object obj)
        {

            bool isSafe = _coreLogic.IsSafeState();

            if (isSafe)
            {

                var result = _coreLogic.GetSafeSequence();
                MessageBox.Show($"Hệ thống AN TOÀN!\nChuỗi cấp phát: {string.Join(" -> ", result.sequence)}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("CẢNH BÁO: Hệ thống KHÔNG an toàn (Nguy cơ Deadlock)!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ===== HÀM PHÁT HIỆN DEADLOCK =====
        private void ExecuteDetectDeadlock(object obj)
        {
            bool isDeadlock = _coreLogic.IsDeadlock();

            
            UpdateMatrixColors();

            if (isDeadlock)
            {
             
                string details = _coreLogic.GetDeadlockDetails();
                MessageBox.Show($"PHÁT HIỆN DEADLOCK!\n{details}", "Cảnh báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Hệ thống hiện tại KHÔNG có Deadlock.", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // ===== HÀM PHỤC HỒI DEADLOCK =====
        private void ExecuteRecoverDeadlock(object obj)
        {

            if (!_coreLogic.IsDeadlock())
            {
                MessageBox.Show("Hệ thống đang an toàn, không có Deadlock để phục hồi!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            _coreLogic.RecoverDeadlock();


            SyncCoreToUI();

            MessageBox.Show("Đã hoàn tất quá trình giải phóng Deadlock và cập nhật giao diện!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void UpdateMatrixColors()
        {
           
            var deadlockInfo = _coreLogic.GetDeadlockDetails();
            bool hasCycle = deadlockInfo.Contains("Chu trình");

            foreach (var row in AllocationMatrix)
            {
                var p = _coreLogic.ListProcess.FirstOrDefault(x => x.ProcessName == row.ProcessName);
                if (p == null) continue;

                foreach (var cell in row.Values)
                {
                    var r = _coreLogic.ListResource.FirstOrDefault(x => x.ResourceName == cell.Name);
                    if (r == null) continue;

                    cell.Status = "None";

                   
                    if (hasCycle && deadlockInfo.Contains(p.ProcessName))
                    {
                        cell.Status = "Deadlock";
                    }
                  
                    else if (p.WaitingResourceId == r.ResourceId)
                    {
                        cell.Status = "Waiting";
                    }
                 
                    else if (p.Allocation.GetValueOrDefault(r.ResourceId, 0) > 0)
                    {
                        cell.Status = "Holding";
                    }
                }
            }

            System.Windows.Data.CollectionViewSource.GetDefaultView(AllocationMatrix).Refresh();
        }
        // ===== HÀM CẤP PHÁT TÀI NGUYÊN  =====
        private void ExecuteRequestResource(object obj)
        {
            SyncUIToCore();
            if (string.IsNullOrEmpty(SelectedProcessName) || string.IsNullOrEmpty(SelectedResourceName) || RequestAmount <= 0)
            {
                MessageBox.Show("Vui lòng chọn Tiến trình, Tài nguyên và nhập Số lượng > 0!", "Lỗi nhập liệu");
                return;
            }

            var p = _coreLogic.ListProcess.FirstOrDefault(x => x.ProcessName == SelectedProcessName);
            var r = _coreLogic.ListResource.FirstOrDefault(x => x.ResourceName == SelectedResourceName);

            if (p != null && r != null)
            {

                bool success = _coreLogic.RequestResource(p, r.ResourceId, RequestAmount);

                if (success)
                {

                    MessageBox.Show($"Đã cấp phát thành công {RequestAmount} {r.ResourceName} cho {p.ProcessName}!");
                    SyncCoreToUI();
                }
                else
                {
                    UpdateMatrixColors();
                }
            }
        }
       
    }
}