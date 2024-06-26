using Microsoft.EntityFrameworkCore;
using WebAPI.Models;

public class MyAppDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("server=DESKTOP-SVRE260; database=Muazzin; Integrated Security=true; Encrypt=False");
    }
    public DbSet<Chat> Chats { get; set; }
}
