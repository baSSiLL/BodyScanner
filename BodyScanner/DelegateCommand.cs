using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Input;

namespace BodyScanner
{
    public class DelegateCommand : ICommand
    {
        public static readonly DelegateCommand UnavailableCommand = new DelegateCommand(new Action(() => { }), () => false);


        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            Contract.Requires(execute != null);

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public DelegateCommand(Action execute, Func<bool> canExecute = null)
            : this(execute != null ? new Action<object>(p => execute.Invoke()) : null,
                   canExecute != null ? new Func<object, bool>(p => canExecute.Invoke()) : null)
        {
            Contract.Requires(execute != null);
        }


        public void InvalidateCanExecute()
        {
            if (Application.Current != null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty)));
            }
            else
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        #region ICommand members

        public bool CanExecute(object parameter)
        {
            return canExecute == null ? true : canExecute.Invoke(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                execute.Invoke(parameter);
            }
        }

        #endregion
    }
}
