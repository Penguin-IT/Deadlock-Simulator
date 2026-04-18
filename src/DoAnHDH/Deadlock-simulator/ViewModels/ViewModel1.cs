using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Deadlock_simulator.Models;
using Deadlock_simulator.Services;

namespace Deadlock_simulator.ViewModels
{
    public class ViewModel1 : BaseViewModel
    {
        private DatabaseService _db;

        private ObservableCollection<Resource> listResource;
        public ObservableCollection<Resource> ListResource
        {
            get => listResource;
            set
            {
                listResource = value;
                OnPropertyChanged(nameof(ListResource));
            }
        }

        private ObservableCollection<Process> listProcess;
        public ObservableCollection<Process> ListProcess
        {
            get => listProcess;
            set
            {
                listProcess = value;
                OnPropertyChanged(nameof(ListProcess));
            }
        }

        //khởi tạo dữ liệu
        public ViewModel1()
        {
            _db = new DatabaseService();

            ListResource = new ObservableCollection<Resource>();
            ListProcess = new ObservableCollection<Process>();


        }
        // Tính toán tài nguyên có sẵn dựa trên tổng số lượng và số lượng đã cấp phát
        private Dictionary<int, int> CalculateAvailable()
        {
            var available = new Dictionary<int, int>();

            foreach (var r in ListResource)
            {
                int allocated = ListProcess
                    .Where(p => p.Allocation.ContainsKey(r.ResourceId))
                    .Sum(p => p.Allocation[r.ResourceId]);

                available[r.ResourceId] = r.Total - allocated;
            }

            return available;
        }
        // Kiểm tra nếu tài nguyên có thể dùng chung thì không cần xét đến nó trong việc phát hiện deadlock
        private bool IsShareableResource(Resource resource)
        {
            return resource != null && resource.IsShareable;
        }

       
       
    }
}
