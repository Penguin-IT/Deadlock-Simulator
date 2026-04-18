using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Deadlock_simulator.Models;

namespace Deadlock_simulator.Services
{
    public class DatabaseService
    {
        // Hàm này dùng chung để lấy chuỗi kết nối
        private string GetConnectionString()
        {
            // Dấu "." đại diện cho máy cục bộ
            return @"Server=.;Database=QLTIENTRINH;Trusted_Connection=True;TrustServerCertificate=True;";
        }
        public List<Resource> GetAllResources()
        {
            using (var db = new SqlConnection(GetConnectionString()))
            {
                return db.Query<Resource>("SELECT * FROM Resources").ToList();
            }
        }
        public List<Process> GetAllProcesses()
        {
            using (var db = new SqlConnection(GetConnectionString()))
            {
            
                return db.Query<Process>("SELECT * FROM Processes").ToList();
            }
        }

        public void UpdateProcess(Process process)
        {
           
            throw new NotImplementedException();
        }
    }
}
