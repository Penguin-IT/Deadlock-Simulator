using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deadlock_simulator.Models
{
    public class Resource
    {
        private int _resourceId;
        private string _resourceName;
        private bool _isShareable;
        private int _hierarchyOrder;
        private bool _isAvailable;

        public int ResourceId { get => _resourceId; set => _resourceId = value; }
        public string ResourceName { get => _resourceName; set => _resourceName = value; }
        public bool IsShareable { get => _isShareable; set => _isShareable = value; }
        public int HierarchyOrder { get => _hierarchyOrder; set => _hierarchyOrder = value; }
        public bool IsAvailable { get => _isAvailable; set => _isAvailable = value; }
    }
}
