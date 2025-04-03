using Employee.Application.Interfaces.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Employee.Application.Interfaces
{
    public interface IEmployee
    {
        Task ImportEmployeesExcel(List<EmployeeDto> employees);
        Task<(List<EmployeeDto> data, int total)> GetEmployees(int page, int pageSize);
    }
}
