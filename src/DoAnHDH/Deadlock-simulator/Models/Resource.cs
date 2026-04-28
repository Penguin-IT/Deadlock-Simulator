using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Deadlock_simulator.Models
{
    public class Resource
    {
        private int _resourceId;
        private string _resourceName;
        private bool _isShareable;
        private int _hierarchyOrder;
        private int _total;

        public int ResourceId { get => _resourceId; set => _resourceId = value; }
        public string ResourceName { get => _resourceName; set => _resourceName = value; }
        public bool IsShareable { get => _isShareable; set => _isShareable = value; }
        public int HierarchyOrder { get => _hierarchyOrder; set => _hierarchyOrder = value; }
        public int Total
        {
            get => _total;
            set
            {
                if (value < 0)
                {
                   MessageBox.Show(" Giá trị nhập vào không hợp lệ (Phải >= 0)");
                    return;
                }
                _total = value;
            }
        }
        private string _currentHolder;
        public string CurrentHolder 
        { 
            get => _currentHolder; 
            set => SetProperty(ref _currentHolder, value); 
        }

        public List<string> CurrentHolders { get; set; } = new List<string>();
        public Queue<Process> WaitingQueue { get; set; } = new Queue<Process>();
    }
} 

