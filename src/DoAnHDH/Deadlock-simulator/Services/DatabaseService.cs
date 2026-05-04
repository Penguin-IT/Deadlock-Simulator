using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Deadlock_simulator.Models;

namespace Deadlock_simulator.Services
{
    
    public class SimulationData
    {
        public List<Resource> ListResource { get; set; }
        public List<Process> ListProcess { get; set; }
    }

    public class DatabaseService
    {
        private SimulationData LoadDataFromJson()
        {
           
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string jsonFilePath = Path.Combine(basePath, "data/data_antoan.json");

            if (!File.Exists(jsonFilePath))
            {
                
                MessageBox.Show($"KHÔNG TÌM THẤY FILE!\nỨng dụng đang tìm tại:\n{jsonFilePath}\n\nVui lòng copy file data_antoan.json thả trực tiếp vào thư mục trên!",
                                "Báo cáo từ DatabaseService", MessageBoxButton.OK, MessageBoxImage.Error);
                return new SimulationData { ListResource = new List<Resource>(), ListProcess = new List<Process>() };
            }

            try
            {
               
                string jsonString = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<SimulationData>(jsonString, options) ?? new SimulationData();
            }
            catch (Exception ex)
            {
                
                MessageBox.Show($"TÌM THẤY FILE NHƯNG LỖI CẤU TRÚC JSON:\n{ex.Message}",
                                "Báo cáo từ DatabaseService", MessageBoxButton.OK, MessageBoxImage.Error);
                return new SimulationData { ListResource = new List<Resource>(), ListProcess = new List<Process>() };
            }
        }

        public List<Resource> GetAllResources()
        {
            return LoadDataFromJson().ListResource ?? new List<Resource>();
        }

        public List<Process> GetAllProcesses()
        {
            return LoadDataFromJson().ListProcess ?? new List<Process>();
        }

        public void UpdateProcess(Process process)
        {
            
        }
    }
}