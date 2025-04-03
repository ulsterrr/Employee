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
                        dataTable.Rows.Add(employee.EmployeeCode, employee.Name, employee.BirthDate);
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
