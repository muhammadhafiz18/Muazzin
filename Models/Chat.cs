namespace WebAPI.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string UserID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? CityOfUser { get; set; }
    }
}
