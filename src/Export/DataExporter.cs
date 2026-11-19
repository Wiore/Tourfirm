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
    /// <summary>
    /// Лабораторная работа №5. Форматы данных: JSON, CSV, XML, YAML.
    ///
    /// Универсальный экспортёр: выбирает одну «главную» таблицу (TourRequests),
    /// связанную с другими (TourProposals → ProposalItems), извлекает записи
    /// SQL-запросом и сериализует их сразу в четыре формата в папку out/.
    ///
    /// Универсальность: все сериализаторы (ToJson / ToCsv / ToXml / ToYaml) работают
    /// с абстрактной структурой <c>List&lt;Dictionary&lt;string, object?&gt;&gt;</c>, где значениями
    /// могут быть скаляры или вложенные списки таких же словарей. Чтобы сменить
    /// таблицу или структуру вложенности, достаточно переписать один метод
    /// <see cref="LoadRequestsNestedAsync"/> — форматы трогать не нужно.
    /// </summary>
    public class DataExporter
    {
        private readonly string _connectionString;

        /// <summary> Папка для выгрузки (создаётся автоматически, если её нет). </summary>
        public string OutDirectory { get; set; } = "out";

        public DataExporter(string connectionString = "Data Source=TourFirm.db")
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Полный цикл экспорта: создаёт папку out, грузит данные, пишет 4 файла.
        /// </summary>
        public async Task<ExportResult> ExportAllAsync(string? outDir = null)
        {
            string dir = string.IsNullOrWhiteSpace(outDir) ? OutDirectory : outDir;
            Directory.CreateDirectory(dir); // требование методички: папка создаётся сама

            // Шаг методички: SQL-запрос по связанной таблице с вложенными данными.
            List<Dictionary<string, object?>> records = await LoadRequestsNestedAsync();

            // Шаг методички: четыре файла в папке out.
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

        // =====================================================================
        //  Загрузка данных из БД.
        //  Единственное место, привязанное к конкретной таблице: меняем SQL —
        //  меняем структуру экспорта, сериализаторы остаются прежними.
        // =====================================================================

        /// <summary>
        /// Загружает все заявки и вкладывает в каждую связанные предложения,
        /// а в каждое предложение — его позиции. Отражает схему связей:
        /// TourRequests (1) ← (N) TourProposals (1) ← (N) ProposalItems.
        /// </summary>
        private async Task<List<Dictionary<string, object?>>> LoadRequestsNestedAsync()
        {
            var requests = new List<Dictionary<string, object?>>();

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Главная таблица: все заявки.
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
                        ["proposals"]          = new List<Dictionary<string, object?>>() // заполним ниже
                    });
                }
            }

            // Связанная таблица 1: предложения по каждой заявке + позиции в них.
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

        /// <summary> Связанная таблица 2: позиции конкретного предложения. </summary>
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

        // =====================================================================
        //  Сериализаторы. Универсальные: работают с любой структурой словарей.
        // =====================================================================

        /// <summary> JSON: вложенные объекты и массивы сохраняются как есть. </summary>
        public static string ToJson(List<Dictionary<string, object?>> records)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Не прятать кириллицу в \uXXXX — файл читается человеком.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(records, options);
        }

        /// <summary>
        /// CSV: формат не поддерживает вложенность, поэтому поля связанных таблиц
        /// объединяются в одну ячейку через «;» — как в примере методички со skills.
        /// </summary>
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

        /// <summary>
        /// XML: вложенность отражается подтегами (proposals → proposal → items → item),
        /// по аналогии с примером методички (student → address → skills).
        /// </summary>
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

        /// <summary> YAML: вложенность задаётся отступами, списки — дефисами. </summary>
        public static string ToYaml(List<Dictionary<string, object?>> records)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return serializer.Serialize(records);
        }

        // ----- Вспомогательные -----

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

        /// <summary>
        /// Экранирование поля CSV по RFC 4180: если в значении есть запятая,
        /// кавычка или перевод строки — оборачиваем в кавычки и удваиваем внутренние.
        /// </summary>
        private static string CsvField(object? value)
        {
            string s = value?.ToString() ?? "";
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }

    /// <summary> Результат экспорта: пути к четырём файлам и количество записей. </summary>
    public record ExportResult(
        string Directory,
        string JsonPath,
        string CsvPath,
        string XmlPath,
        string YamlPath,
        int RecordsCount);
}
