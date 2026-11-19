using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DataBaseControl
{
    public class DataExporter
    {
        private readonly string _connectionString;

        public string OutDirectory { get; set; } = "out";

        public DataExporter(string connectionString = "Data Source=TourFirm.db")
        {
            _connectionString = connectionString;
        }

        public async Task<ExportResult> ExportAllAsync(string? outDir = null)
        {
            string dir = string.IsNullOrWhiteSpace(outDir) ? OutDirectory : outDir;

            List<Dictionary<string, object?>> records = await LoadRequestsNestedAsync();

            string jsonPath = Path.Combine(dir, "data.json");
            string csvPath  = Path.Combine(dir, "data.csv");
            string xmlPath  = Path.Combine(dir, "data.xml");
            string yamlPath = Path.Combine(dir, "data.yaml");

            await File.WriteAllTextAsync(jsonPath, ToJson(records), Encoding.UTF8);
            await File.WriteAllTextAsync(csvPath,  ToCsv(records),  Encoding.UTF8);
            await File.WriteAllTextAsync(xmlPath,  ToXml(records),  Encoding.UTF8);
            await File.WriteAllTextAsync(yamlPath, ToYaml(records), Encoding.UTF8);

            return new ExportResult(dir, jsonPath, csvPath, xmlPath, yamlPath, records.Count);
        }

        private async Task<List<Dictionary<string, object?>>> LoadRequestsNestedAsync()
        {
            var requests = new List<Dictionary<string, object?>>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, ClientId, Destination, DurationDays, Wishes,
                           Status, CreatedAt, AssignedEmployeeId
                    FROM TourRequests
                    ORDER BY Id;";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    requests.Add(new Dictionary<string, object?>
                    {
                        ["id"]                 = reader.GetInt32(0),
                        ["clientId"]           = reader.GetInt32(1),
                        ["destination"]        = reader.GetString(2),
                        ["durationDays"]       = reader.GetInt32(3),
                        ["wishes"]             = reader.IsDBNull(4) ? null : reader.GetString(4),
                        ["status"]             = reader.GetInt32(5),
                        ["createdAt"]          = reader.GetString(6),
                        ["assignedEmployeeId"] = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7),
                        ["proposals"]          = new List<Dictionary<string, object?>>()
                    });
                }
            }

            foreach (var req in requests)
            {
                int requestId = (int)req["id"]!;
                var proposals = (List<Dictionary<string, object?>>)req["proposals"]!;

                using var propCmd = connection.CreateCommand();
                propCmd.CommandText = @"
                    SELECT Id, EmployeeId, Message, CreatedAt, ClientDecision
                    FROM TourProposals
                    WHERE RequestId = @rid
                    ORDER BY Id;";
                propCmd.Parameters.AddWithValue("@rid", requestId);

                await using var propReader = await propCmd.ExecuteReaderAsync();
                while (await propReader.ReadAsync())
                {
                    int proposalId = propReader.GetInt32(0);
                    proposals.Add(new Dictionary<string, object?>
                    {
                        ["id"]             = proposalId,
                        ["employeeId"]     = propReader.GetInt32(1),
                        ["message"]        = propReader.IsDBNull(2) ? null : propReader.GetString(2),
                        ["createdAt"]      = propReader.GetString(3),
                        ["clientDecision"] = propReader.IsDBNull(4) ? null : (int?)propReader.GetInt32(4),
                        ["items"]          = await LoadItemsAsync(connection, proposalId)
                    });
                }
            }

            return requests;
        }

        private static async Task<List<Dictionary<string, object?>>> LoadItemsAsync(
            SqliteConnection connection, int proposalId)
        {
            var items = new List<Dictionary<string, object?>>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Title, Description, Price
                FROM ProposalItems
                WHERE ProposalId = @pid
                ORDER BY Id;";
            cmd.Parameters.AddWithValue("@pid", proposalId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new Dictionary<string, object?>
                {
                    ["id"]          = reader.GetInt32(0),
                    ["title"]       = reader.GetString(1),
                    ["description"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ["price"]       = reader.GetDouble(3)
                });
            }
            return items;
        }

        public static string ToJson(List<Dictionary<string, object?>> records)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(records, options);
        }

        public static string ToCsv(List<Dictionary<string, object?>> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("id,clientId,destination,durationDays,status,createdAt,wishes,proposals,proposalItems");

            foreach (var r in records)
            {
                var proposals = (List<Dictionary<string, object?>>)r["proposals"]!;
                var items = proposals
                    .SelectMany(p => (List<Dictionary<string, object?>>)p["items"]!)
                    .ToList();

                string proposalsSummary = string.Join("; ",
                    proposals.Select(p => $"#{p["id"]}: {p["message"]} ({DecisionText(p["clientDecision"])})"));

                string itemsSummary = string.Join("; ",
                    items.Select(i => $"{i["title"]} = {FormatPrice(i["price"])}"));

                sb.Append(CsvField(r["id"]));              sb.Append(',');
                sb.Append(CsvField(r["clientId"]));        sb.Append(',');
                sb.Append(CsvField(r["destination"]));     sb.Append(',');
                sb.Append(CsvField(r["durationDays"]));    sb.Append(',');
                sb.Append(CsvField(r["status"]));          sb.Append(',');
                sb.Append(CsvField(r["createdAt"]));       sb.Append(',');
                sb.Append(CsvField(r["wishes"]));          sb.Append(',');
                sb.Append(CsvField(proposalsSummary));     sb.Append(',');
                sb.Append(CsvField(itemsSummary));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string ToXml(List<Dictionary<string, object?>> records)
        {
            var root = new XElement("tourRequests");
            foreach (var r in records)
            {
                var reqEl = new XElement("tourRequest",
                    new XElement("id", r["id"]),
                    new XElement("clientId", r["clientId"]),
                    new XElement("destination", r["destination"]),
                    new XElement("durationDays", r["durationDays"]),
                    new XElement("status", r["status"]),
                    new XElement("createdAt", r["createdAt"]),
                    new XElement("wishes", r["wishes"])
                );

                var proposalsEl = new XElement("proposals");
                foreach (var p in (List<Dictionary<string, object?>>)r["proposals"]!)
                {
                    var propEl = new XElement("proposal",
                        new XElement("id", p["id"]),
                        new XElement("employeeId", p["employeeId"]),
                        new XElement("message", p["message"]),
                        new XElement("createdAt", p["createdAt"]),
                        new XElement("clientDecision", p["clientDecision"])
                    );

                    var itemsEl = new XElement("items");
                    foreach (var i in (List<Dictionary<string, object?>>)p["items"]!)
                    {
                        itemsEl.Add(new XElement("item",
                            new XElement("id", i["id"]),
                            new XElement("title", i["title"]),
                            new XElement("description", i["description"]),
                            new XElement("price", i["price"])
                        ));
                    }
                    propEl.Add(itemsEl);
                    proposalsEl.Add(propEl);
                }
                reqEl.Add(proposalsEl);
                root.Add(reqEl);
            }

            return new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                root
            ).ToString();
        }

        public static string ToYaml(List<Dictionary<string, object?>> records)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(records);
        }

        private static string DecisionText(object? decision) =>
            decision switch
            {
                null => "ожидает решения",
                1    => "согласовано",
                0    => "отклонено",
                _    => decision.ToString()!
            };

        private static string FormatPrice(object? price)
        {
            double value = price is double d ? d : Convert.ToDouble(price, CultureInfo.InvariantCulture);
            return value.ToString("0.##", CultureInfo.InvariantCulture) + " руб.";
        }

        private static string CsvField(object? value)
        {
            string s = value?.ToString() ?? "";
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }

    public record ExportResult(
        string Directory,
        string JsonPath,
        string CsvPath,
        string XmlPath,
        string YamlPath,
        int RecordsCount);
}
