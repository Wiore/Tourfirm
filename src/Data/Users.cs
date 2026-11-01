using System;
using System.Collections.Generic;
using System.Text;
using DataBaseControl;

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
        public string Login { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
    public class UserData : UserBase
    {
        public int Id { get; set; }
        public int Role { get; set; } = 0;

    }
    public class NewUserData : UserBase
    {
        public string Password { get; set; } = "";

    }
    public class EnterUser
    {
        public EnterStatus EnterStatus { get; set; }
        public UserData? User { get; set; }
    }
}