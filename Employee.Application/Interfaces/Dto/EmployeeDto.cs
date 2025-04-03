using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Employee.Application.Interfaces.Dto
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; }
        public string Name { get; set; }
        public DateTime BirthDate { get; set; }
        public int Age { get; set; }
    }
}
