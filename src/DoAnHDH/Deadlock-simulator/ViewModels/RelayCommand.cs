using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Deadlock_simulator.ViewModels
{
    public class RelayCommand : ICommand
    {
        
        private readonly Action<object> _execute;

      
        private readonly Predicate<object> _canExecute;

       
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(execute.ToString(), "Action execute không được null.");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true; 
            }
            else
            {
                return _canExecute(parameter);
            }
        }

        
        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
        }

       
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
        

}
