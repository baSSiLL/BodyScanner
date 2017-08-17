using System.Windows;

namespace BodyScanner
{
    class UserInteractionService
    {
        public void ShowError(string message)
        {
            MessageBox.Show(message, Properties.Resources.ApplicationName,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
