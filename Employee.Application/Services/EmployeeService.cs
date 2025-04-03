using Dapper;
using Employee.Application.Interfaces;
using Employee.Application.Interfaces.Dto;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Employee.Application.Services
{
    public class EmployeeService: IEmployee
    {
        private readonly string? _connectionString;
        public EmployeeService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task ImportEmployeesExcel(List<EmployeeDto> employees)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                int maxId = 0;
                using (var command = new SqlCommand("SELECT ISNULL(MAX(CAST(SUBSTRING(EmployeeCode, 4, LEN(EmployeeCode) - 3) AS INT)), 0) FROM Employees", connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    maxId = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }

                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "Employees";
                    bulkCopy.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("BirthDate", "BirthDate");

                    var dataTable = new DataTable();
                    dataTable.Columns.Add("EmployeeCode", typeof(string));
                    dataTable.Columns.Add("Name", typeof(string));
                    dataTable.Columns.Add("BirthDate", typeof(DateTime));

                    foreach (var employee in employees)
                    {
                        maxId++;
                        string employeeCode = "NV_" + maxId;
                        dataTable.Rows.Add(employeeCode, employee.Name, employee.BirthDate);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
            }
        }

        public async Task<(List<EmployeeDto> data, int total)> GetEmployees(int page, int pageSize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    Page = page,
                    PageSize = pageSize
                };

                using (var multi = await connection.QueryMultipleAsync("GetEmployeesPagingData", parameters, commandType: CommandType.StoredProcedure))
                {
                    var data = (await multi.ReadAsync<EmployeeDto>()).ToList();
                    var total = await multi.ReadFirstOrDefaultAsync<int>();
                    return (data, total);
                }
            }
        }
    }
}
