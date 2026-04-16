using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deadlock_simulator.Models
{
    public class Process
    {
        private int _processId;
        private string _processName;
        private string _status;
        private int? _holdingResourceId;
        private int? _waitingResourceId;

        public int ProcessId { get => _processId; set => _processId = value; }
        public string ProcessName { get => _processName; set => _processName = value; }
        public string Status { get => _status; set => _status = value; }
        public int? HoldingResourceId { get => _holdingResourceId; set => _holdingResourceId = value; }
        public int? WaitingResourceId { get => _waitingResourceId; set => _waitingResourceId = value; }
    }
}
