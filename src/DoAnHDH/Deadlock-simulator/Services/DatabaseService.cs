using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using Deadlock_simulator.Models;

namespace Deadlock_simulator.Services
{
    // Class phụ để hứng dữ liệu từ cục JSON
    public class SimulationData
    {
        public List<Resource> ListResource { get; set; }
        public List<Process> ListProcess { get; set; }
    }

    public class DatabaseService
    {
        private SimulationData LoadDataFromJson()
        {
            // 1. Dùng đường dẫn tuyệt đối để trị dứt điểm trò "trốn tìm" của Windows
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string jsonFilePath = Path.Combine(basePath, "data/data_antoan.json");

            if (!File.Exists(jsonFilePath))
            {
                // Nếu thực sự không thấy file, nó sẽ gào lên cho bạn biết chính xác đường dẫn nó đang tìm
                MessageBox.Show($"KHÔNG TÌM THẤY FILE!\nỨng dụng đang tìm tại:\n{jsonFilePath}\n\nVui lòng copy file data_antoan.json thả trực tiếp vào thư mục trên!",
                                "Báo cáo từ DatabaseService", MessageBoxButton.OK, MessageBoxImage.Error);
                return new SimulationData { ListResource = new List<Resource>(), ListProcess = new List<Process>() };
            }

            try
            {
                // 2. Ép JSON bỏ qua việc phân biệt chữ Hoa/Thường để nạp dữ liệu dễ dàng hơn
                string jsonString = File.ReadAllText(jsonFilePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<SimulationData>(jsonString, options) ?? new SimulationData();
            }
            catch (Exception ex)
            {
                // Nếu cấu trúc JSON bị sai (dư dấu phẩy, sai kiểu dữ liệu), nó sẽ chỉ rõ lỗi ở dòng nào
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
            // Hàm lưu Database tạm thời bỏ trống như cũ
        }
    }
}