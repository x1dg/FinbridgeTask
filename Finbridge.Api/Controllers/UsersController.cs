using Finbridge.Api.Dtos;
using Finbridge.Api.Services;
using Finbridge.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Finbridge.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;

        public UsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public ActionResult<User> CreateUser(CreateUserDto dto)
        {
            var user = _userService.CreateUser(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetUserById(int id)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetAllUsers()
        {
            return _userService.GetAllUsers().ToList();
        }
    }
}