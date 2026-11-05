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
                    Nickname TEXT NOT NULL,
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
            command.CommandText = @"SELECT Id, Nickname, Password, Email, Phone, Role FROM Users WHERE Login = @login";
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
                            NickName = reader.GetString(1),
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

            string sql = @"INSERT INTO Users (Login, Password, Nickname, Email, Phone, Role, OfficeId, WorkstationId) 
                   VALUES (@login, @password, @nickname, @email, @phone, 0, NULL, NULL)";

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            command.Parameters.AddWithValue("@login", newUserDat.Login.ToLower());
            command.Parameters.AddWithValue("@password", newUserDat.Password);
            command.Parameters.AddWithValue("@nickname", newUserDat.NickName);
            command.Parameters.AddWithValue("@email", newUserDat.Email);
            command.Parameters.AddWithValue("@phone", newUserDat.Phone);

            await command.ExecuteNonQueryAsync();

            using var idCommand = connection.CreateCommand();
            idCommand.CommandText = "SELECT last_insert_rowid()";

            int id = Convert.ToInt32(await idCommand.ExecuteScalarAsync());

            return (new UserData() { Id = id, Email=newUserDat.Email,Login=newUserDat.Login,Phone=newUserDat.Phone,NickName=newUserDat.NickName});
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

        public async Task<int> CreateTourRequest(NewTourRequest request)
        {
            using var connection = await GetConnection();
            string sql = @"INSERT INTO TourRequests (ClientId, Destination, DurationDays, Wishes, Status, CreatedAt) 
                   VALUES (@clientId, @dest, @days, @wishes, 0, CURRENT_TIMESTAMP)";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@clientId", request.ClientId);
            command.Parameters.AddWithValue("@dest", request.Destination);
            command.Parameters.AddWithValue("@days", request.DurationDays);
            command.Parameters.AddWithValue("@wishes", request.Wishes);

            await command.ExecuteNonQueryAsync();

            using var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid()";
            return Convert.ToInt32(await idCmd.ExecuteScalarAsync());
        }

        public async Task<List<object>> GetClientRequests(int clientId)
        {
            var list = new List<object>();
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"SELECT Id, Destination, DurationDays, Status, CreatedAt 
                            FROM TourRequests WHERE ClientId = @id ORDER BY CreatedAt DESC";
            command.Parameters.AddWithValue("@id", clientId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader.GetInt32(0),
                    Destination = reader.GetString(1),
                    DurationDays = reader.GetInt32(2),
                    Status = reader.GetInt32(3),
                    CreatedAt = reader.GetString(4)
                });
            }
            return list;
        }

        public async Task<int> CreateProposal(NewProposal proposal)
        {
            using var connection = await GetConnection();

            string sql = @"INSERT INTO TourProposals (RequestId, EmployeeId, Message, CreatedAt) 
                   VALUES (@reqId, @empId, @msg, CURRENT_TIMESTAMP)";
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@reqId", proposal.RequestId);
            command.Parameters.AddWithValue("@empId", proposal.EmployeeId);
            command.Parameters.AddWithValue("@msg", proposal.Message);
            await command.ExecuteNonQueryAsync();

            using var idCmd = connection.CreateCommand();
            idCmd.CommandText = "SELECT last_insert_rowid()";
            int proposalId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());

            foreach (var item in proposal.Items)
            {
                using var itemCmd = connection.CreateCommand();
                itemCmd.CommandText = @"INSERT INTO ProposalItems (ProposalId, Title, Description, Price) 
                                VALUES (@pId, @title, @desc, @price)";
                itemCmd.Parameters.AddWithValue("@pId", proposalId);
                itemCmd.Parameters.AddWithValue("@title", item.Title);
                itemCmd.Parameters.AddWithValue("@desc", item.Description);
                itemCmd.Parameters.AddWithValue("@price", item.Price);
                await itemCmd.ExecuteNonQueryAsync();
            }

            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = "UPDATE TourRequests SET Status = 2 WHERE Id = @reqId";
            updateCmd.Parameters.AddWithValue("@reqId", proposal.RequestId);
            await updateCmd.ExecuteNonQueryAsync();

            return proposalId;
        }


        public async Task<bool> DecideProposal(int proposalId, bool approve)
        {
            using var connection = await GetConnection();

            int decision = approve ? 1 : 0;
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE TourProposals SET ClientDecision = @dec WHERE Id = @id";
            cmd.Parameters.AddWithValue("@dec", decision);
            cmd.Parameters.AddWithValue("@id", proposalId);
            await cmd.ExecuteNonQueryAsync();

            if (approve)
            {
                using var updateReq = connection.CreateCommand();
                updateReq.CommandText = @"UPDATE TourRequests SET Status = 3 
                                  WHERE Id = (SELECT RequestId FROM TourProposals WHERE Id = @id)";
                updateReq.Parameters.AddWithValue("@id", proposalId);
                await updateReq.ExecuteNonQueryAsync();
            }

            return true;
        }

        public async Task AddTour(int clientId, int? requestId, string title, string country, DateTime start, DateTime end)
        {
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO Tours (ClientId, RequestId, Title, Country, StartDate, EndDate, Status) 
                            VALUES (@clientId, @reqId, @title, @country, @start, @end, 0)";
            command.Parameters.AddWithValue("@clientId", clientId);
            command.Parameters.AddWithValue("@reqId", (object?)requestId ?? DBNull.Value);
            command.Parameters.AddWithValue("@title", title);
            command.Parameters.AddWithValue("@country", country);
            command.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd HH:mm:ss"));
            await command.ExecuteNonQueryAsync();
        }
        public async Task<List<object>> GetActiveRequests()
        {
            var list = new List<object>();
            using var connection = await GetConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, ClientId, Destination, DurationDays, Status FROM TourRequests WHERE Status IN (0, 1, 2)";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader.GetInt32(0),
                    ClientId = reader.GetInt32(1),
                    Destination = reader.GetString(2),
                    DurationDays = reader.GetInt32(3),
                    Status = reader.GetInt32(4)
                });
            }
            return list;
        }

        public async Task<List<UserData>> GetAllUsers()
        {
            var list = new List<UserData>();
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT Id, Login, Nickname, Email, Phone, Role FROM Users WHERE Role = 0";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new UserData
                {
                    Id = reader.GetInt32(0),
                    Login = reader.GetString(1),
                    NickName = reader.GetString(2),
                    Email = reader.GetString(3),
                    Phone = reader.GetString(4),
                    Role = reader.GetInt32(5)
                });
            }
            return list;
        }

        public async Task<List<object>> GetClientTours(int clientId)
        {
            var list = new List<object>();
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"SELECT Id, Title, Country, StartDate, EndDate, Status 
                            FROM Tours WHERE ClientId = @id ORDER BY StartDate DESC";
            command.Parameters.AddWithValue("@id", clientId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Country = reader.GetString(2),
                    StartDate = reader.GetString(3),
                    EndDate = reader.GetString(4),
                    Status = reader.GetInt32(5)
                });
            }
            return list;
        }

        public async Task<bool> UpdateTour(UpdateTourData data)
        {
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = @"UPDATE Tours 
                            SET Title = @title, Country = @country, StartDate = @start, EndDate = @end 
                            WHERE Id = @id";

            command.Parameters.AddWithValue("@title", data.Title);
            command.Parameters.AddWithValue("@country", data.Country);
            command.Parameters.AddWithValue("@start", data.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@end", data.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@id", data.TourId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        public async Task<List<object>> GetProposalsByRequest(int requestId)
        {
            var list = new List<object>();
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT Id, Message, ClientDecision FROM TourProposals WHERE RequestId = @id";
            command.Parameters.AddWithValue("@id", requestId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int proposalId = reader.GetInt32(0);
                string message = reader.GetString(1);

                list.Add(new { Id = proposalId, Message = message, Decision = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2) });
            }
            return list;
        }

        public async Task<bool> IsEmployee(int userId)
        {
            using var connection = await GetConnection();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT Role FROM Users WHERE Id = @id";
            command.Parameters.AddWithValue("@id", userId);

            var result = await command.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result) == 1;
            }
            return false;
        }
    }
}