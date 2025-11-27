using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Threading.Channels;

namespace LaserSplellBot
{
    public class ApplicationContext : DbContext, IDisposable
    {
        private readonly string _connectionString = @"Host=localhost;Port=5432;Database=laserspell;Username=postgres;Password=1590;";
        private NpgsqlConnection connection;
        public ApplicationContext(bool reset = false) 
        {
            connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            if (reset)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }

            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Channel> Channels { get; set; }

        public static async Task<bool> NewChannel(Channel channel)
        {
            await using var db = new ApplicationContext();

            try
            {
                var exist = await db.Channels.AnyAsync(x => x.ChatId == channel.ChatId);
                if (!exist)
                {
                    await db.Channels.AddAsync(channel);
                    await db.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception e)
            {
                await db.DisposeAsync();                
                Console.WriteLine(e);
                throw;
            }

            return false;
        }

        public static async Task<List<User>?> GetUsers()
        {
            await using var db = new ApplicationContext();

            try
            {
                return db.Users.ToList();
            }
            catch (Exception e)
            {
                await db.DisposeAsync();
                Console.WriteLine(e);
                return null;
            }
            
        }

        public static async Task<List<Channel>?> GetChannel(long chatId)
        {
            await using var db = new ApplicationContext();

            try
            {
                return db.Channels.ToList();
            }
            catch (Exception e)
            {
                await db.DisposeAsync();
                Console.WriteLine(e);
                return null;
            }
        }

        public static async Task UpdUser(User user)
        {
            await using var db = new ApplicationContext();

            try
            {
                db.Users.Update(user);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                await db.DisposeAsync();
                Console.WriteLine(e);
                throw;
            }
        }

        public static async Task<User?> GetUser(long chatId)
        {
            await using var db = new ApplicationContext();

            try
            {
                return await db.Users.FirstOrDefaultAsync(x => x.ChatId == chatId);
            }
            catch (Exception e)
            {
                await db.DisposeAsync();
                Console.WriteLine(e);
                return null;
            }
        }

        public static async Task AddUser(User user)
        {
            await using var db = new ApplicationContext();

            try
            {
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static async Task AddPost(Post post)
        {
            throw new NotImplementedException();
        }
    }

    public class Channel    
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long AdminChatId { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateDelete { get; set; }
        public string Text { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public long AuthorChatId { get; set; }
        public string PhotoId { get; set; } = string.Empty;

    }

    public class User
    {
        public int Id { get; set; }
        public long ChatId { get; set; }
        public bool PostCreate { get; set; } = false;
        public Role Role { get; set; } = Role.User;
        public string Name { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime DateUpdate { get; set; } = DateTime.UtcNow;

    }

    public enum Role
    {
        Admin, User, Infl
    }
}
