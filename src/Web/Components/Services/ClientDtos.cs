namespace Web.Components.Services
{
    // Лёгкие DTO для десериализации ответов API.
    // API отдаёт анонимные объекты (camelCase), System.Text.Json в Web-профиле
    // сравнивает имена свойств без учёта регистра — DTO с PascalCase подходят.

    /// <summary> Запись из GET /api/users/{clientId}/requests. </summary>
    public class ClientRequestDto
    {
        public int Id { get; set; }
        public string Destination { get; set; } = "";
        public int DurationDays { get; set; }
        public int Status { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    /// <summary> Запись из GET /api/users/{clientId}/tours. </summary>
    public class TourDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Country { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public int Status { get; set; }
        // Стоимость тура и состав услуг берутся из утверждённого предложения
        // по заявке, к которой привязан тур (см. GetClientTours в DataBase).
        public double TotalPrice { get; set; }
        public List<TourItemDto> Items { get; set; } = new();
    }

    /// <summary> Услуга (позиция предложения), входящая в тур. </summary>
    public class TourItemDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
    }

    /// <summary> Запись из GET /api/users/request/{requestId}/proposals. </summary>
    public class ProposalDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public int? Decision { get; set; }
        // Итоговая цена предложения и перечень услуг — чтобы клиент видел,
        // за что платит, до принятия решения.
        public double TotalPrice { get; set; }
        public List<TourItemDto> Items { get; set; } = new();
    }

    /// <summary> Запись из GET /api/users/requests/active. </summary>
    public class ActiveRequestDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Destination { get; set; } = "";
        public int DurationDays { get; set; }
        public int Status { get; set; }
    }
}
