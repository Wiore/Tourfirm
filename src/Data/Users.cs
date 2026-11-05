using System;
using System.Collections.Generic;
using System.Text;
using DataBaseControl;
using System.ComponentModel.DataAnnotations;

namespace Users
{
    public enum EnterStatus
    {
        UserNotFound = -1,
        WrongPassword = 0,
        Success = 1
    }
    public class UserBase
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен включать от 3 до 50 символов")]
        public string Login { get; set; } = "";
        [Required(ErrorMessage = "Nickname обязателен")]
        [StringLength(50, ErrorMessage = "Nickname не должн превышать 50 символов")]
        public string NickName { get; set; } = "";
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат Email")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        public string Phone { get; set; } = "";
    }
    public class UserData : UserBase
    {
        public int Id { get; set; }
        public int Role { get; set; } = 0;

    }
    public class NewUserData : UserBase
    {
        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен быть не менее 6 символов")]
        public string Password { get; set; } = "";

    }
    public class EnterUser
    {
        public EnterStatus EnterStatus { get; set; }
        public UserData? User { get; set; }
    }
    public class NewTourRequest
    {
        [Required]
        public int ClientId { get; set; }
        [Required(ErrorMessage = "Укажите страну/город")]
        public string Destination { get; set; } = "";
        [Required]
        [Range(1, 30, ErrorMessage = "Длительность от 1 до 30 дней")]
        public int DurationDays { get; set; }
        public string Wishes { get; set; } = "";
    }
    public class NewProposal
    {
        [Required]
        public int RequestId { get; set; }
        [Required]
        public int EmployeeId { get; set; }
        [Required(ErrorMessage = "Напишите сопроводительный текст")]
        public string Message { get; set; } = "";
        public List<ProposalItemDto> Items { get; set; } = new();
    }
    public class ProposalItemDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
    }
    public class NewTourData
    {
        [Required]
        public int ClientId { get; set; }

        public int? RequestId { get; set; }

        [Required(ErrorMessage = "Укажите название тура")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Укажите страну")]
        public string Country { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
    }
    public class UpdateTourData
    {
        [Required]
        public int TourId { get; set; }
        [Required(ErrorMessage = "Укажите новое название тура")]
        public string Title { get; set; } = "";
        [Required(ErrorMessage = "Укажите страну")]
        public string Country { get; set; } = "";
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
    }
}