using Employee.Application.Interfaces;
using Employee.Application.Interfaces.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace Employee.Web.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployee _employee;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployee employee, ILogger<EmployeeController> logger)
        {
            _employee = employee;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees(int page = 1, int pageSize = 10)
        {
            _logger.LogInformation("Paginated request received. Page: {Page}, PageSize: {PageSize}", page, pageSize);

            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var (data, total) = await _employee.GetEmployees(page, pageSize);

                _logger.LogInformation("Fetched {Count} records. Total: {Total}", data.Count, total);

                return Ok(new
                {
                    Data = data,
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database query failed: {Message}", ex.Message);
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            _logger.LogInformation("Import started. File name: {FileName}", file?.FileName);

            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            if (!file.FileName.EndsWith(".xlsx"))
                return BadRequest("Only .xlsx files are allowed.");

            try
            {
                var employees = new List<EmployeeDto>();
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            employees.Add(new EmployeeDto
                            {
                                EmployeeCode = "",
                                Name = worksheet.Cells[row, 2].Text,
                                BirthDate = DateTime.Parse(worksheet.Cells[row, 3].Text),
                                Age = (int)((DateTime.Now - DateTime.Parse(worksheet.Cells[row, 3].Text)).TotalDays / 365.25)
                            });

                            if (employees.Count % 1000 == 0)
                            {
                                await _employee.ImportEmployeesExcel(employees);
                                _logger.LogInformation("Processed {Rows} rows", employees.Count);
                                employees.Clear();
                            }
                        }
                    }
                }

                if (employees.Count > 0)
                {
                    await _employee.ImportEmployeesExcel(employees);
                }

                _logger.LogInformation("Import completed successfully.");
                return Ok(new { Message = "Import successful", TotalRows = employees.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed: {Message}", ex.Message);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}
