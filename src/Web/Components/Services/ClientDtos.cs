namespace Web.Components.Services
{
    public class ClientRequestDto
    {
        public int Id { get; set; }
        public string Destination { get; set; } = "";
        public int DurationDays { get; set; }
        public int Status { get; set; }
        public string CreatedAt { get; set; } = "";
    }

    public class TourDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Country { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public int Status { get; set; }
        public double TotalPrice { get; set; }
        public List<TourItemDto> Items { get; set; } = new();
    }

    public class TourItemDto
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
    }

    public class ProposalDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public int? Decision { get; set; }
        public double TotalPrice { get; set; }
        public List<TourItemDto> Items { get; set; } = new();
    }

    public class ActiveRequestDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Destination { get; set; } = "";
        public int DurationDays { get; set; }
        public int Status { get; set; }
    }
}
