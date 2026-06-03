using Finbridge.Api.Services;
using Finbridge.Api.Dtos;
using Finbridge.Core.Models;
using Finbridge.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finbridge.Tests
{
    public class UserServiceTests
    {
        private FinbridgeDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<FinbridgeDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            var context = new FinbridgeDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public void CreateUser_ShouldCreateUserWithZeroBalance()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userService = new UserService(context);
            var dto = new CreateUserDto
            {
                FullName = "Test User",
                DateOfBirth = new DateTime(1990, 1, 1),
                PlaceOfBirth = "Test City"
            };

            // Act
            var user = userService.CreateUser(dto);

            // Assert
            Assert.NotNull(user);
            Assert.Equal("Test User", user.FullName);
            Assert.Equal(new DateTime(1990, 1, 1), user.DateOfBirth);
            Assert.Equal("Test City", user.PlaceOfBirth);
            Assert.Equal(0, user.Balance);
        }

        [Fact]
        public void GetUserById_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userService = new UserService(context);
            
            var dto = new CreateUserDto
            {
                FullName = "Test User",
                DateOfBirth = new DateTime(1990, 1, 1),
                PlaceOfBirth = "Test City"
            };
            
            var createdUser = userService.CreateUser(dto);

            // Act
            var user = userService.GetUserById(createdUser.Id);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(createdUser.Id, user.Id);
            Assert.Equal("Test User", user.FullName);
        }

        [Fact]
        public void GetUserById_ShouldReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userService = new UserService(context);

            // Act
            var user = userService.GetUserById(999);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public void GetAllUsers_ShouldReturnAllUsers()
        {
            // Arrange
            using var context = CreateInMemoryDbContext();
            var userService = new UserService(context);
            
            var user1 = userService.CreateUser(new CreateUserDto
            {
                FullName = "User 1",
                DateOfBirth = new DateTime(1990, 1, 1),
                PlaceOfBirth = "City 1"
            });
            
            var user2 = userService.CreateUser(new CreateUserDto
            {
                FullName = "User 2",
                DateOfBirth = new DateTime(1991, 1, 1),
                PlaceOfBirth = "City 2"
            });

            // Act
            var users = userService.GetAllUsers().ToList();

            // Assert
            Assert.Equal(2, users.Count);
            Assert.Contains(users, u => u.Id == user1.Id);
            Assert.Contains(users, u => u.Id == user2.Id);
        }
    }
}