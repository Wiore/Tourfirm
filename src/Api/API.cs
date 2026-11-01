using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using DataBaseControl;
using Users;

namespace Turfirm.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly DataBase _db;

        public UsersController()
        {
            _db = new DataBase();
        }

        [HttpGet("{login}/{password}")]
        public async Task<IActionResult> EnterToSys(string login, string password)
        {
            EnterUser enterUser = await _db.EterToSys(login, password);
            switch (enterUser.EnterStatus)
            {
                case EnterStatus.Success:
                    return Ok(enterUser.User);
                case EnterStatus.WrongPassword:
                    return BadRequest("Неправильный пароль");
                case EnterStatus.UserNotFound:
                    return BadRequest("Пользователь не найден");
                default:
                    return BadRequest("Неизвестная ошибка");
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Registration([FromBody] NewUserData newUser)
        {
            bool IsRegistred = await _db.CheckLogin(newUser.Login);

            if (!IsRegistred)
            {
                return Ok(await _db.InsToUser(newUser));
            }
            else
            {
                return BadRequest("Пользователь уже зарегестрирован");
            }
        }
    }
}