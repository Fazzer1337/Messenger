using System.Windows;

namespace messenger
{
    public partial class UserNameWindow : Window
    {
        public string UserName { get; private set; } = string.Empty;
        public UserNameWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UserNameTextBox.Text))
            {
                UserName = UserNameTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите имя пользователя.");
            }
        }
    }
}
