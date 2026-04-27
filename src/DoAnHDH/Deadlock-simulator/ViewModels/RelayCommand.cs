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
        // Action thực hiện khi Command được gọi
        private readonly Action<object> _execute;

        // Predicate kiểm tra xem Command có thể thực hiện hay không
        private readonly Predicate<object> _canExecute;

        /// <summary>a
        /// Constructor chính, nhận action và predicate
        /// </summary>
        /// <param name="execute">Hành động sẽ thực hiện khi command được gọi</param>
        /// <param name="canExecute">Điều kiện cho phép thực thi, có thể null</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(execute.ToString(), "Action execute không được null.");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        /// <summary>
        /// Kiểm tra xem Command có thể thực thi hay không
        /// </summary>
        /// <param name="parameter">Tham số truyền từ UI (có thể null)</param>
        /// <returns>true nếu có thể thực thi, false nếu không</returns>
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true; // Nếu không có điều kiện thì luôn có thể thực thi
            }
            else
            {
                return _canExecute(parameter);
            }
        }

        /// <summary>
        /// Thực hiện hành động khi Command được gọi
        /// </summary>
        /// <param name="parameter">Tham số truyền từ UI (có thể null)</param>
        public void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
        }

        /// <summary>
        /// Sự kiện báo cho UI khi trạng thái CanExecute thay đổi
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Gọi để cập nhật lại CanExecute, khiến UI kiểm tra lại trạng thái enable/disable
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
        

}
