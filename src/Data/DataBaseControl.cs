using System;
using Microsoft.Data.Sqlite;
using Users;

namespace DataBaseControl
{
    public class DataBase
    {
        private string _connectionString = "Data Source=TourFirm.db";

        private async Task<SqliteConnection> GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                await pragma.ExecuteNonQueryAsync();
            }

            return connection;
        }

        public async Task GenerateDataBase()
        {
            using var connection = await GetConnection();

            string sql = @"
                DROP TABLE IF EXISTS ProposalItems;
                DROP TABLE IF EXISTS Tours;
                DROP TABLE IF EXISTS TourProposals;
                DROP TABLE IF EXISTS TourRequests;
                DROP TABLE IF EXISTS Users;
                DROP TABLE IF EXISTS Offices;

                CREATE TABLE Offices (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Address TEXT NOT NULL
                );

                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Login TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Phone TEXT NOT NULL,
                    Role INTEGER NOT NULL,
                    OfficeId INTEGER,
                    WorkstationId TEXT,
                    FOREIGN KEY (OfficeId) REFERENCES Offices(Id) ON DELETE SET NULL
                );

                CREATE TABLE TourRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClientId INTEGER NOT NULL,
                    Destination TEXT NOT NULL,
                    DurationDays INTEGER NOT NULL,
                    Wishes TEXT,
                    Status INTEGER NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    AssignedEmployeeId INTEGER,
                    FOREIGN KEY (ClientId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (AssignedEmployeeId) REFERENCES Users(Id) ON DELETE SET NULL
                );

                CREATE TABLE TourProposals (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    RequestId INTEGER NOT NULL,
                    EmployeeId INTEGER NOT NULL,
                    Message TEXT,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ClientDecision INTEGER,
                    FOREIGN KEY (RequestId) REFERENCES TourRequests(Id) ON DELETE CASCADE,
                    FOREIGN KEY (EmployeeId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE ProposalItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProposalId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Description TEXT,
                    Price REAL NOT NULL,
                    FOREIGN KEY (ProposalId) REFERENCES TourProposals(Id) ON DELETE CASCADE
                );

                CREATE TABLE Tours (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClientId INTEGER NOT NULL,
                    RequestId INTEGER,
                    Title TEXT NOT NULL,
                    Country TEXT NOT NULL,
                    StartDate DATETIME NOT NULL,
                    EndDate DATETIME NOT NULL,
                    Status INTEGER NOT NULL,
                    FOREIGN KEY (ClientId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (RequestId) REFERENCES TourRequests(Id) ON DELETE SET NULL
                );
            ";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("База создана");
        }
        public async Task<EnterUser> EterToSys(string login, string password)
        {
            using var connection = await GetConnection();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT Id, FullName, Password, Email, Phone, Role FROM Users WHERE Login = @login";
            command.Parameters.AddWithValue("@login",login);
            using var reader = await command.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                if (password == reader.GetString(2))
                {
                    return new EnterUser()
                    {
                        EnterStatus = EnterStatus.Success,
                        User = new UserData
                        {
                            Id = reader.GetInt32(0),
                            FullName = reader.GetString(1),
                            Email = reader.GetString(3),
                            Phone = reader.GetString(4),
                            Role = reader.GetInt32(5)
                        }
                    };
                }
                return new EnterUser() { EnterStatus=EnterStatus.WrongPassword};
            }
            return new EnterUser() { EnterStatus=EnterStatus.UserNotFound};
        }
        public async Task<UserData> InsToUser(NewUserData newUserDat)
        {
            using var connection = await GetConnection();

            string sql = @"INSERT INTO Users (Login, Password, FullName, Email, Phone, Role, OfficeId, WorkstationId) 
                   VALUES (@login, @password, @fullname, @email, @phone, 0, NULL, NULL)";

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@login", newUserDat.Login.ToLower());
            command.Parameters.AddWithValue("@password", newUserDat.Password);
            command.Parameters.AddWithValue("@fullname", newUserDat.FullName);
            command.Parameters.AddWithValue("@email", newUserDat.Email);
            command.Parameters.AddWithValue("@phone", newUserDat.Phone);

            await command.ExecuteNonQueryAsync();

            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";

            int id = Convert.ToInt32(await idCommand.ExecuteScalarAsync());

            return (new UserData() { Id = id, Email=newUserDat.Email,Login=newUserDat.Login,Phone=newUserDat.Phone,FullName=newUserDat.FullName});
        }
        public async Task<bool> CheckLogin(string login)
        {
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = @login";
            command.Parameters.AddWithValue("@login", login.ToLower());

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result)>0;
        }
        public async Task InsToOffices(string name, string address)
        {
            using var conection = await GetConnection();

            string sql = @"INSERT INTO Offices (Name, Address)
                VALUES (@name, @address)";
            
            using var command = conection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@address", address);

            await command.ExecuteNonQueryAsync();
            Console.WriteLine($"Офис {name} добавлен");
        }
    }
}