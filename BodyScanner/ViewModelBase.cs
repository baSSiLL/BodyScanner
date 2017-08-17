using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace BodyScanner
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Contract.Requires(GetType().GetProperty(propertyName) != null);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            Contract.Requires(propertyNames != null);
            Contract.Requires(Contract.ForAll(propertyNames, pn => !string.IsNullOrEmpty(pn)));

            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Sets new property value and notifies about corresponding property change.
        /// </summary>
        /// <returns>
        /// <c>True</c> if new value differs from the old one and the property has actually been set; <c>false</c> otherwise.
        /// </returns>
        protected bool SetPropertyValue<T>(T value, ref T backingField, [CallerMemberName] string changedPropertyName = null)
        {
            if (!Equals(value, backingField))
            {
                backingField = value;
                OnPropertyChanged(changedPropertyName);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets new property value and notifies about changes of dependent properties.
        /// </summary>
        /// <returns>
        /// <c>True</c> if new value differs from the old one and the property has actually been set; <c>false</c> otherwise.
        /// </returns>
        protected bool SetPropertyValue<T>(T value, ref T backingField, params string[] changedPropertyNames)
        {
            if (!Equals(value, backingField))
            {
                backingField = value;
                foreach (var propertyName in changedPropertyNames)
                {
                    OnPropertyChanged(propertyName);
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
