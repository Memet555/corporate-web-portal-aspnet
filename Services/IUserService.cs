using WebPortal.Models;

namespace WebPortal.Services
{
    public interface IUserService
    {
        User? ValidateUser(string username, string password);
        List<User> GetAllUsers();
        void AddUser(User user);
        void DeleteUser(int userId);
    }
}
