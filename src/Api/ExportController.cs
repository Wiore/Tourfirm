using DataBaseControl;
using Microsoft.AspNetCore.Mvc;

namespace Turfirm.Controllers
{
    /// <summary>
    /// API экспорта данных в форматы JSON, CSV, XML, YAML (лаб. работа №5).
    /// Позволяет запустить выгрузку и скачать готовые файлы.
    /// </summary>
    [ApiController]
    [Route("api/export")]
    public class ExportController : ControllerBase
    {
        private readonly DataBase _db;

        public ExportController()
        {
            _db = new DataBase();
        }

        /// <summary>
        /// Запускает экспорт: создаёт папку out и четыре файла data.* .
        /// Доступ только сотрудникам (Role = 1).
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> Run([FromQuery] int userId)
        {
            if (!await _db.IsEmployee(userId))
                return Unauthorized("Экспорт доступен только сотрудникам.");

            var exporter = new DataExporter();
            ExportResult result = await exporter.ExportAllAsync();

            return Ok(new
            {
                directory = result.Directory,
                recordsCount = result.RecordsCount,
                files = new[]
                {
                    new { format = "json", path = result.JsonPath },
                    new { format = "csv",  path = result.CsvPath  },
                    new { format = "xml",  path = result.XmlPath  },
                    new { format = "yaml", path = result.YamlPath }
                }
            });
        }

        /// <summary>
        /// Отдаёт готовый файл экспорта по формату (json/csv/xml/yaml).
        /// </summary>
        [HttpGet("download/{format}")]
        public IActionResult Download(string format)
        {
            string fmt = format.ToLowerInvariant();
            string relative = fmt switch
            {
                "json" => Path.Combine("out", "data.json"),
                "csv"  => Path.Combine("out", "data.csv"),
                "xml"  => Path.Combine("out", "data.xml"),
                "yaml" => Path.Combine("out", "data.yaml"),
                _      => ""
            };

            if (relative.Length == 0)
                return BadRequest("Неизвестный формат. Доступны: json, csv, xml, yaml.");

            // PhysicalFile требует абсолютный путь — разрешаем относительно
            // рабочей директории API (папка проекта при `dotnet run`).
            string file = Path.GetFullPath(relative);

            if (!System.IO.File.Exists(file))
                return NotFound("Файл ещё не создан. Сначала запустите экспорт: POST /api/export/run?userId=...");

            string contentType = fmt switch
            {
                "json" => "application/json",
                "csv"  => "text/csv",
                "xml"  => "application/xml",
                "yaml" => "text/yaml",
                _      => "application/octet-stream"
            };

            return PhysicalFile(file, contentType, $"data.{fmt}");
        }
    }
}
