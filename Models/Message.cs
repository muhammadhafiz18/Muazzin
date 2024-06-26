namespace WebAPI.Models
{
    public class Message
    {
        public string MessageId { get; set; }
        public Chat From { get; set; }
        public string Text { get; set; }
    }
}
