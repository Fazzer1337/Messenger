using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using messenger.Models;
using messenger.Services;

namespace messenger
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<User> Users { get; set; }
        public string UserName { get; set; }
        private Dictionary<string, ObservableCollection<Message>> ChatMessages;
        private User? currentUser;
        private P2PChatService? chatService;
        private int localPort = 9000;
        private string remoteIp = "127.0.0.1";
        private int remotePort = 9001;

        public MainWindow() : this("Гость") { }

        public MainWindow(string userName)
        {
            InitializeComponent();
            UserName = userName;
            DataContext = this;

            this.Loaded += MainWindow_Loaded;

            Users = new ObservableCollection<User>
            {
                new User { Name = "Партнёр", Status = "Онлайн" }
            };

            ChatMessages = new Dictionary<string, ObservableCollection<Message>>();
            foreach (var user in Users)
                ChatMessages[user.Name] = new ObservableCollection<Message>();

            UsersList.ItemsSource = Users;
            UsersList.SelectionChanged += UsersList_SelectionChanged;

            if (Users.Count > 0)
                UsersList.SelectedIndex = 0;

            Title = $"Мессенджер — {UserName}";

            chatService = new P2PChatService(GetLocalPort());
            chatService.OnMessageReceived += OnMessageReceived;
            chatService.StartServer();

            SendUserPresence();

            LoadHistory();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var loginWindow = new UserNameWindow();
            loginWindow.Owner = this;
            if (loginWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(loginWindow.UserName))
            {
                UserName = loginWindow.UserName;
                Title = $"Мессенджер — {UserName}";
                DataContext = null;
                DataContext = this;
                SendUserPresence();
            }
            else
            {
                Close();
            }
            this.Loaded -= MainWindow_Loaded;
        }

        private int GetLocalPort()
        {
            int port;
            if (LocalPortTextBox != null && int.TryParse(LocalPortTextBox.Text, out port))
                return port;
            return localPort;
        }

        private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentUser = UsersList.SelectedItem as User;
            if (currentUser != null && ChatMessages.ContainsKey(currentUser.Name))
                MessagesList.ItemsSource = ChatMessages[currentUser.Name];
            else
                MessagesList.ItemsSource = null;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string text = MessageTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(text) && currentUser != null)
            {
                if (IpTextBox != null)
                    remoteIp = IpTextBox.Text.Trim();
                if (PortTextBox != null && int.TryParse(PortTextBox.Text, out int port))
                    remotePort = port;

                var msg = new Message(text, UserName)
                {
                    // свойство Type можно добавить в Message.cs если требуется, либо удалить эту строку
                };

                ChatMessages[currentUser.Name].Add(msg);
                MessageTextBox.Text = string.Empty;
                SaveHistory();

                if (chatService != null)
                    await chatService.SendMessageAsync(remoteIp, remotePort, msg);
            }
        }

        private void SendUserPresence()
        {
            if (chatService == null) return;

            var presenceMsg = new Message(UserName, UserName)
            {
                Text = "Online"
                // свойство Type можно добавить в Message.cs если требуется, либо удалить эту строку
            };

            chatService.SendMessageAsync(remoteIp, remotePort, presenceMsg);
        }

        private void OnMessageReceived(Message msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (msg.Text == "Online")
                {
                    if (!Users.Any(u => u.Name == msg.Sender))
                        Users.Add(new User { Name = msg.Sender, Status = "Онлайн" });
                }
                else
                {
                    if (!ChatMessages.ContainsKey(msg.Sender))
                        ChatMessages[msg.Sender] = new ObservableCollection<Message>();
                    ChatMessages[msg.Sender].Add(msg);
                    SaveHistory();
                }
            });
        }

        private void SaveHistory()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(ChatMessages, options);
            File.WriteAllText("chat_history.json", json);
        }

        private void LoadHistory()
        {
            if (!File.Exists("chat_history.json")) return;

            var json = File.ReadAllText("chat_history.json");
            var restored = JsonSerializer.Deserialize<Dictionary<string, ObservableCollection<Message>>>(json);
            if (restored != null)
                ChatMessages = restored;
        }
    }
}
