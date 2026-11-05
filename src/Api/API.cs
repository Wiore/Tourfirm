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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

        [HttpGet("all-clients")]
        public async Task<IActionResult> GetAllClients([FromQuery] int userId)
        {
            if (!await _db.IsEmployee(userId))
                return Unauthorized("Доступ запрещен. Только для сотрудников.");

            var users = await _db.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("{clientId}/tours")]
        public async Task<IActionResult> GetClientTours(int clientId)
        {
            var tours = await _db.GetClientTours(clientId);
            return Ok(tours);
        }

        [HttpPut("tour/update")]
        public async Task<IActionResult> UpdateTour([FromBody] UpdateTourData data, [FromQuery] int userId)
        {
            if (!await _db.IsEmployee(userId))
                return Unauthorized("Доступ запрещен. Только для сотрудников.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool success = await _db.UpdateTour(data);

            if (success)
                return Ok(new { message = "Тур успешно обновлен" });
            else
                return NotFound(new { message = "Тур с таким ID не найден" });
        }

        [HttpPost("create-request")]
        public async Task<IActionResult> CreateRequest([FromBody] NewTourRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            int requestId = await _db.CreateTourRequest(request);
            return Ok(new { message = "Заявка создана", id = requestId });
        }

        [HttpPost("create-proposal")]
        public async Task<IActionResult> CreateProposal([FromBody] NewProposal proposal)
        {
            if (!await _db.IsEmployee(proposal.EmployeeId))
                return Unauthorized("Только сотрудники могут отвечать на заявки.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            int proposalId = await _db.CreateProposal(proposal);
            return Ok(new { message = "Предложение отправлено", id = proposalId });
        }

        [HttpGet("requests/active")]
        public async Task<IActionResult> GetActiveRequests()
        {
            var requests = await _db.GetActiveRequests();
            return Ok(requests);
        }

        [HttpGet("{clientId}/requests")]
        public async Task<IActionResult> GetClientRequests(int clientId)
        {
            var requests = await _db.GetClientRequests(clientId);
            return Ok(requests);
        }

        [HttpPost("proposal/{proposalId}/decide")]
        public async Task<IActionResult> DecideProposal(int proposalId, [FromQuery] bool approve)
        {
            bool success = await _db.DecideProposal(proposalId, approve);

            if (success)
            {
                string msg = approve ? "Тур успешно согласован!" : "Предложение отклонено";
                return Ok(new { message = msg });
            }
            return BadRequest("Не удалось обработать решение");
        }

        [HttpGet("request/{requestId}/proposals")]
        public async Task<IActionResult> GetProposals(int requestId)
        {
            var proposals = await _db.GetProposalsByRequest(requestId);
            return Ok(proposals);
        }

        [HttpPost("tour/add")]
        public async Task<IActionResult> AddTour([FromBody] NewTourData tourData, [FromQuery] int userId)
        {
            if (!await _db.IsEmployee(userId))
                return Unauthorized("Только сотрудники могут добавлять туры.");

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _db.AddTour(tourData.ClientId, tourData.RequestId, tourData.Title,
                              tourData.Country, tourData.StartDate, tourData.EndDate);

            return Ok(new { message = "Тур успешно добавлен клиенту" });
        }
    }
}