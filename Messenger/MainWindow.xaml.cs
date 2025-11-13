using messenger.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace messenger
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<User> Users { get; set; }
        private Dictionary<string, ObservableCollection<Message>> ChatMessages;
        private User? currentUser; // допускает null
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

            // Инициализация чатов для каждого пользователя
            ChatMessages = new Dictionary<string, ObservableCollection<Message>>();
            foreach (var user in Users)
                ChatMessages[user.Name] = new ObservableCollection<Message>();

            UsersList.ItemsSource = Users;
            UsersList.SelectionChanged += UsersList_SelectionChanged;
            UsersList.SelectedIndex = 0; // Выбрать "себя" или первого пользователя

            Title = $"Мессенджер — {_userName}";
        }

        // Обновление MessageList при смене собеседника
        private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentUser = UsersList.SelectedItem as User;
            if (currentUser != null)
                MessagesList.ItemsSource = ChatMessages[currentUser.Name];
            else
                MessagesList.ItemsSource = null;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && currentUser != null)
            {
                ChatMessages[currentUser.Name].Add(new Message(text, _userName));
                MessageTextBox.Text = string.Empty;

                // Отвечает бот, если выбран бот
                if (currentUser.Name == "Bot")
                    RespondFromBot();
            }
        }

        private async void RespondFromBot()
        {
            await Task.Delay(1000);
            ChatMessages["Bot"].Add(new Message("Это автоответ.", "Bot"));
        }
    }
}
