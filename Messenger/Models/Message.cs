using System;

namespace messenger.Models
{
    public enum MessageType
    {
        Text,
        Presence
    }

    public class Message
    {
        public string Text { get; set; }
        public string Sender { get; set; }
        public DateTime Time { get; set; }
        public MessageType Type { get; set; }

        public Message()
        {
            Time = DateTime.Now;
        }

        public Message(string text, string sender, MessageType type = MessageType.Text)
        {
            Text = text;
            Sender = sender;
            Time = DateTime.Now;
            Type = type;
        }

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss}] {Sender}: {Text}";
        }
    }
}
