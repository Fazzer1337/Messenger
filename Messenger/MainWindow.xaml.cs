using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using messenger.Models;
using messenger.Services;

namespace messenger
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<User> Users { get; set; }
        public string UserName { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        private Dictionary<string, ObservableCollection<Message>> ChatMessages;
        private Dictionary<string, DateTime> UserLastSeen;
        private User? currentUser;
        private P2PChatService? chatService;
        private UdpDiscoveryService? udpDiscovery;
        private DispatcherTimer statusTimer;
        private int localPort = 9000;
        private string remoteIp = "127.0.0.1";
        private int remotePort = 9001;
        private int broadcastPort = 8001;
        private Random rnd = new Random();
        private const string BotName = "Умный Бэн";

        private string userStatus = "В сети";
        public string UserStatus
        {
            get => userStatus;
            set
            {
                if (userStatus != value)
                {
                    userStatus = value;
                    OnPropertyChanged(nameof(UserStatus));
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow() : this("Спам") { }

        public MainWindow(string userName)
        {
            InitializeComponent();
            UserName = userName;
            DataContext = this;

            this.Loaded += MainWindow_Loaded;

            Users = new ObservableCollection<User>
            {
                new User { Name = userName, Status = "В сети" },
                new User { Name = BotName, Status = "В сети" }
            };

            ChatMessages = new Dictionary<string, ObservableCollection<Message>>();
            UserLastSeen = new Dictionary<string, DateTime>();

            foreach (var user in Users)
            {
                ChatMessages[user.Name] = new ObservableCollection<Message>();
                UserLastSeen[user.Name] = DateTime.Now;
            }

            UsersList.ItemsSource = Users;
            UsersList.SelectionChanged += UsersList_SelectionChanged;

            UsersList.SelectedIndex = 0;
            currentUser = UsersList.SelectedItem as User;
            MessagesList.ItemsSource = ChatMessages[currentUser.Name];

            Title = $"Мессенджер — {UserName}";

            chatService = new P2PChatService(GetLocalPort());
            chatService.OnMessageReceived += OnMessageReceived;
            chatService.StartServer();

            udpDiscovery = new UdpDiscoveryService(broadcastPort);
            udpDiscovery.UserDiscovered += name =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (name == UserName || name == BotName)
                        return;
                    var user = Users.FirstOrDefault(u => u.Name == name);
                    if (user == null)
                    {
                        Users.Add(new User { Name = name, Status = "В сети" });
                        ChatMessages[name] = new ObservableCollection<Message>();
                        UserLastSeen[name] = DateTime.Now;
                    }
                    else
                    {
                        user.Status = "В сети";
                        UserLastSeen[name] = DateTime.Now;
                    }
                });
            };
            udpDiscovery.StartListening();
            udpDiscovery.BroadcastUserName(UserName);

            statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromSeconds(10);
            statusTimer.Tick += UpdateUserStatuses;
            statusTimer.Start();

            SendUserPresence();

            LoadHistory();
        }

        private void UpdateUserStatuses(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            foreach (var user in Users)
            {
                if (user.Name == UserName || user.Name == BotName)
                {
                    user.Status = "В сети";
                    continue;
                }
                if (UserLastSeen.TryGetValue(user.Name, out var lastSeen))
                {
                    user.Status = (now - lastSeen).TotalSeconds < 30 ? "В сети" : "Не в сети";
                }
                else
                {
                    user.Status = "Не в сети";
                }
            }
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

                var selfUser = Users.FirstOrDefault(u => u.Name == UserName);
                if (selfUser == null)
                {
                    Users.Insert(0, new User { Name = UserName, Status = "В сети" });
                    ChatMessages[UserName] = new ObservableCollection<Message>();
                    UserLastSeen[UserName] = DateTime.Now;
                }
                else
                {
                    selfUser.Status = "В сети";
                }

                UsersList.SelectedIndex = 0;
                currentUser = UsersList.SelectedItem as User;
                MessagesList.ItemsSource = ChatMessages[UserName];

                if (udpDiscovery != null)
                    udpDiscovery.BroadcastUserName(UserName);

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
            {
                MessagesList.ItemsSource = ChatMessages[currentUser.Name];
                ScrollMessagesListToEnd();
            }
            else
            {
                MessagesList.ItemsSource = null;
            }
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

                var msg = new Message(text, UserName);

                if (!ChatMessages.ContainsKey(currentUser.Name))
                    ChatMessages[currentUser.Name] = new ObservableCollection<Message>();

                ChatMessages[currentUser.Name].Add(msg);
                MessagesList.ItemsSource = ChatMessages[currentUser.Name];
                ScrollMessagesListToEnd();

                MessageTextBox.Text = string.Empty;
                SaveHistory();

                if (currentUser.Name == BotName)
                {
                    string[] botReplies = new[] { "Да", "Нет", "Возможно" };
                    var botMsg = new Message(botReplies[rnd.Next(botReplies.Length)], BotName);
                    ChatMessages[BotName].Add(botMsg);
                    if (currentUser.Name == BotName)
                        MessagesList.ItemsSource = ChatMessages[BotName];
                    ScrollMessagesListToEnd();
                    SaveHistory();
                    return;
                }

                if (chatService != null)
                    await chatService.SendMessageAsync(remoteIp, remotePort, msg);
            }
        }

        private void ScrollMessagesListToEnd()
        {
            if (MessagesList.Items.Count > 0)
            {
                var last = MessagesList.Items[MessagesList.Items.Count - 1];
                MessagesList.ScrollIntoView(last);
            }
        }

        private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                e.Handled = true;
                SendButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void SendUserPresence()
        {
            if (chatService == null) return;

            var presenceMsg = new Message(UserName, UserName)
            {
                Text = "Online"
            };

            chatService.SendMessageAsync(remoteIp, remotePort, presenceMsg);
        }

        private void OnMessageReceived(Message msg)
        {
            Dispatcher.Invoke(() =>
            {
                if (msg.Text == "Online")
                {
                    if (msg.Sender == UserName || msg.Sender == BotName)
                        return;
                    var user = Users.FirstOrDefault(u => u.Name == msg.Sender);
                    if (user == null)
                    {
                        Users.Add(new User { Name = msg.Sender, Status = "В сети" });
                        ChatMessages[msg.Sender] = new ObservableCollection<Message>();
                    }
                    else
                    {
                        user.Status = "В сети";
                    }
                    UserLastSeen[msg.Sender] = DateTime.Now;
                }
                else
                {
                    if (!ChatMessages.ContainsKey(msg.Sender))
                        ChatMessages[msg.Sender] = new ObservableCollection<Message>();
                    ChatMessages[msg.Sender].Add(msg);
                    if (currentUser != null && currentUser.Name == msg.Sender)
                    {
                        MessagesList.ItemsSource = ChatMessages[msg.Sender];
                        ScrollMessagesListToEnd();
                    }
                    SaveHistory();
                }
            });
        }

        private void SaveHistory()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var dictToSave = ChatMessages.Where(kvp => kvp.Key != BotName)
                                         .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var jsonWithoutBot = JsonSerializer.Serialize(dictToSave, options);
            File.WriteAllText("chat_history.json", jsonWithoutBot);
        }

        private void LoadHistory()
        {
            if (!File.Exists("chat_history.json")) return;
            var json = File.ReadAllText("chat_history.json");
            var restored = JsonSerializer.Deserialize<Dictionary<string, ObservableCollection<Message>>>(json);
            if (restored != null)
            {
                ChatMessages = restored;
                // Очищаем чат бота при загрузке
                ChatMessages[BotName] = new ObservableCollection<Message>();
            }
        }
    }
}
