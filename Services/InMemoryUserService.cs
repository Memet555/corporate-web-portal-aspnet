using WebPortal.Models;

namespace WebPortal.Services
{
    public class InMemoryUserService : IUserService
    {
        private readonly List<User> _users;

        public InMemoryUserService()
        {
            _users = new List<User>
            {
                new User { Id = 1, Username = "admin", Password = "admin123", Role = "Admin" },
                new User { Id = 2, Username = "user", Password = "user123", Role = "User" }
            };
        }

        public User? ValidateUser(string username, string password)
        {
            return _users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
                u.Password == password);
        }

        public List<User> GetAllUsers()
        {
            return _users.ToList();
        }

        public void AddUser(User user)
        {
            user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
            _users.Add(user);
        }

        public void DeleteUser(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                _users.Remove(user);
            }
        }
    }
}
