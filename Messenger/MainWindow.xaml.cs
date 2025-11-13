using messenger.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace messenger
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<User> Users { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        private string _userName;

        // Конструктор по умолчанию для работы XAML
        public MainWindow() : this("Гость") { }

        // Основной конструктор для передачи имени пользователя
        public MainWindow(string userName)
        {
            InitializeComponent();
            _userName = userName;
            Users = new ObservableCollection<User>
            {
                new User { Name = _userName },
                new User { Name = "Bot", Status = "Онлайн" }
            };
            Messages = new ObservableCollection<Message>();
            UsersList.ItemsSource = Users;
            MessagesList.ItemsSource = Messages;
            Title = $"Мессенджер — {_userName}";
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                Messages.Add(new Message(text, _userName));
                MessageTextBox.Text = string.Empty;

                // Демонстрационный автоответ
                RespondFromBot();
            }
        }

        private async void RespondFromBot()
        {
            await Task.Delay(1000);
            Messages.Add(new Message("Это автоответ.", "Bot"));
        }
    }
}
