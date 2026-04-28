using Deadlock_simulator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deadlock_simulator.Models
{
    public class ResourceValue : BaseViewModel
    {
        private string _Name;

        public string Name
        {
            get { return _Name; }
            set { 
                _Name = value; 
            OnPropertyChanged(nameof(Name));
            }
        }

        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }
}
