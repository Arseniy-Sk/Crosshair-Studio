using System.Windows;

namespace Simple_Customized_Crosshair_SCC.Views
{
    public partial class RenameDialog : Window
    {
        public string? NewName { get; set; }

        public RenameDialog(string currentName)
        {
            InitializeComponent();
            NewName = currentName;
            DataContext = this;
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewName))
            {
                DialogResult = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Please enter a valid name.", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
    }
}