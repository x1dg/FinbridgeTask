using Finbridge.Core.Models;
using Finbridge.Data;
using Finbridge.Api.Dtos;

namespace Finbridge.Api.Services
{
    public class UserService
    {
        private readonly FinbridgeDbContext _context;

        public UserService(FinbridgeDbContext context)
        {
            _context = context;
        }

        public User CreateUser(CreateUserDto dto)
        {
            var user = new User
            {
                FullName = dto.FullName,
                DateOfBirth = dto.DateOfBirth,
                PlaceOfBirth = dto.PlaceOfBirth,
                Balance = 0
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public User? GetUserById(int id)
        {
            return _context.Users.Find(id);
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users.ToList();
        }
    }
}