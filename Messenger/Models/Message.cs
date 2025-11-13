using System;

namespace messenger.Models
{
    public class Message
    {
        public string Text { get; set; }
        public string Sender { get; set; }
        public DateTime Time { get; set; }

        public Message(string text, string sender)
        {
            Text = text;
            Sender = sender;
            Time = DateTime.Now;
        }
    }
}
