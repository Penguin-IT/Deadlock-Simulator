using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Deadlock_simulator.Models;
using Deadlock_simulator.Services;

namespace Deadlock_simulator.ViewModels
{
    public class ViewModel1 : BaseViewModel
    {
        private DatabaseService _db;

        private ObservableCollection<Resource> listResource;
        public ObservableCollection<Resource> ListResource
        {
            get => listResource;
            set
            {
                listResource = value;
                OnPropertyChanged(nameof(ListResource));
            }
        }

        private ObservableCollection<Process> listProcess;
        public ObservableCollection<Process> ListProcess
        {
            get => listProcess;
            set
            {
                listProcess = value;
                OnPropertyChanged(nameof(ListProcess));
            }
        }

        //khởi tạo dữ liệu
        public ViewModel1()
        {
            _db = new DatabaseService();

            ListResource = new ObservableCollection<Resource>();
            ListProcess = new ObservableCollection<Process>();


        }
        // Tính toán tài nguyên có sẵn dựa trên tổng số lượng và số lượng đã cấp phát
        private Dictionary<int, int> CalculateAvailable()
        {
            var available = new Dictionary<int, int>();

            foreach (var r in ListResource)
            {
                int allocated = ListProcess
                    .Where(p => p.Allocation.ContainsKey(r.ResourceId))
                    .Sum(p => p.Allocation[r.ResourceId]);

                available[r.ResourceId] = r.Total - allocated;
            }

            return available;
        }
        // Kiểm tra nếu tài nguyên có thể dùng chung thì không cần xét đến nó trong việc phát hiện deadlock
        private bool IsShareableResource(Resource resource)
        {
            return resource != null && resource.IsShareable;
        }
         // Phát hiện deadlock
        public bool IsDeadlock()
        {
            var graph = BuildGraph();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> stack = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                if (HasCycle(node, graph, visited, stack)) return true;
            }
            return false;
        }
        //Ngăn chặn deadlock

        //Hạn chế tối đa việc cấp phát chưa cần thiết

        private bool StrictCurbRequest(Process p, Resource r)
        {

            if (r == null) return false;
            // 1. Ngăn circular wait (quan trọng nhất)
            if (!PreventCircularWait(p, r))
                return false;

            // 2. Giới hạn waiting động theo resource
            int currentWaiting = ListProcess.Count(proc => proc.WaitingResourceId == r.ResourceId);
            int MAX_WAITING = r.Total * 2; if (currentWaiting >= MAX_WAITING)
                return false;

            // 3. Resource shareable → ưu tiên
            if (r.IsShareable)
                return true;

            return true;
        }

        //Tạo đồ thị cấp phát và yêu cầu từ dữ liệu Process và Resource
        private Dictionary<string, List<string>> BuildGraph()
        {
            var map = new Dictionary<string, List<string>>();
            var processes = ListProcess.ToList();
            var resources = ListResource.ToList();

            foreach (var p in processes)
            {
                if (!map.ContainsKey(p.ProcessName))
                    map[p.ProcessName] = new List<string>();

                // 1. Allocation: Resource -> Process
                if (p.HoldingResourceId != null)
                {
                    var res = resources.FirstOrDefault(r => r.ResourceId == p.HoldingResourceId);

                    if (res != null && !res.IsShareable)
                    {
                        if (!map.ContainsKey(res.ResourceName))
                            map[res.ResourceName] = new List<string>();

                        map[res.ResourceName].Add(p.ProcessName);
                    }
                }

                // 2. Request: Process -> Resource
                if (p.WaitingResourceId != null)
                {
                    var res = resources.FirstOrDefault(r => r.ResourceId == p.WaitingResourceId);

                    if (res != null)
                    {
                        map[p.ProcessName].Add(res.ResourceName);
                    }
                }
            }

            return map;
        }

        // Dùng DFS để kiem tra trong đồ thị có chu trình không
        private bool HasCycle(string node, Dictionary<string, List<string>> Graph, HashSet<string> visited, HashSet<string> stack)
        {
            if (stack.Contains(node)) return true;
            if (visited.Contains(node)) return false;

            visited.Add(node);
            stack.Add(node);

            if (Graph.ContainsKey(node))
            {
                foreach (var neighbor in Graph[node])
                {
                    if (HasCycle(neighbor, Graph, visited, stack))
                        return true;
                }
            }

            stack.Remove(node);
            return false;
        }

        // Phát hiện deadlock
        public bool IsDeadlock()
        {
            var graph = BuildGraph();
            HashSet<string> visited = new HashSet<string>();
            HashSet<string> stack = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                if (HasCycle(node, graph, visited, stack)) return true;
            }
            return false;
        }

        //In ra chi tiết chu trình gây ra deadlock

        public string GetDeadlockDetails()
        {
            var graph = BuildGraph();
            var visited = new HashSet<string>();
            var stack = new List<string>(); // Dùng List để lưu thứ tự các nút

            foreach (var node in graph.Keys)
            {
                if (Find_CycleDetails(node, graph, visited, stack, out var cycle))
                {
                    return "Chu trình gây Deadlock: " + string.Join(" -> ", cycle);
                }
            }
            return "Không tìm thấy chu trình.";
        }

        private bool Find_CycleDetails(string node, Dictionary<string, List<string>> graph,
            HashSet<string> visited, List<string> stack, out List<string> cycle)
        {
            cycle = null;
            if (stack.Contains(node))
            {
                // Lấy đoạn bị lặp để tạo thành chu trình
                int index = stack.IndexOf(node);
                cycle = stack.Skip(index).ToList();
                cycle.Add(node);
                return true;
            }

            if (visited.Contains(node)) return false;

            visited.Add(node);
            stack.Add(node);

            if (graph.ContainsKey(node))
            {
                foreach (var neighbor in graph[node])
                {
                    if (Find_CycleDetails(neighbor, graph, visited, stack, out cycle)) return true;
                }
            }

            stack.RemoveAt(stack.Count - 1);
            return false;
        }

        // Thu hồi tài nguyên mà tiến trình đang giữ bằng cách ưu tiên thu hồi tài nguyên không thể dùng chung trước, sau đó mới đến tài nguyên có thể dùng chung
        public void RecoverByPreemption(int resourceId)
        {
            // Tìm tiến trình đang giữ tài nguyên này và bắt nó trả lại(set Holding = null)
            var holder = ListProcess.FirstOrDefault(p => p.HoldingResourceId == resourceId);
            if (holder != null)
            {
                holder.HoldingResourceId = null;
                // Cập nhật vào Database
                _db.UpdateProcess(holder);
                MessageBox.Show($"Đã thu hồi tài nguyên {resourceId} từ {holder.ProcessName}");
            }
            LoadAllData(); // Load lại giao diện
        }

        //hàm kiểm tra trạng thái an toàn của hệ thống (Safe State)
        public bool IsSafeState()
        {
            var work = new Dictionary<int, int>(CalculateAvailable());

            var finish = ListProcess.ToDictionary(p => p.ProcessName, p => false);

            bool progress;

            do
            {
                progress = false;

                foreach (var p in ListProcess)
                {
                    if (finish[p.ProcessName]) continue;
                    bool canRun = true;

                    foreach (var resId in p.Max.Keys)
                    {
                        int allocation = p.Allocation.GetValueOrDefault(resId, 0);
                        int need = p.Max[resId] - allocation;

                        if (need > work.GetValueOrDefault(resId, 0))
                        {
                            canRun = false;
                            break;
                        }
                    }

                    if (canRun)
                    {
                        // trả tài nguyên lại
                        foreach (var resId in p.Allocation.Keys)
                        {
                            work[resId] += p.Allocation[resId];
                        }

                        finish[p.ProcessName] = true;
                        progress = true;
                    }
                }

            } while (progress);

            return finish.Values.All(f => f);
        }
        //Trả về chuỗi an toàn nếu có, nếu không có thì trả về chuỗi rỗng
        public (bool isSafe, List<string> sequence) GetSafeSequence()
        {
            var work = new Dictionary<int, int>(CalculateAvailable());
            var finish = ListProcess.ToDictionary(p => p.ProcessName, p => false);
            var sequence = new List<string>();

            bool progress;

            do
            {
                progress = false;

                foreach (var p in ListProcess)
                {
                    if (finish[p.ProcessName]) continue;

                    bool canRun = true;

                    foreach (var resId in p.Max.Keys)
                    {
                        int need = p.Max[resId] - p.Allocation.GetValueOrDefault(resId, 0);

                        if (need > work.GetValueOrDefault(resId, 0))
                        {
                            canRun = false;
                            break;
                        }
                    }

                    if (canRun)
                    {
                        foreach (var resId in p.Allocation.Keys)
                        {
                            work[resId] += p.Allocation[resId];
                        }

                        finish[p.ProcessName] = true;
                        sequence.Add(p.ProcessName);
                        progress = true;
                    }
                }

            } while (progress);

            bool isSafe = finish.Values.All(f => f);
            return (isSafe, sequence);
        }

        // Kiểm tra yêu cầu tài nguyên theo thuật toán Banker có được chấp nhận không
        public bool RequestResources(Process p, Dictionary<int, int> request)
        {
            var available = CalculateAvailable();

            // 1. Check request <= Need
            foreach (var resId in request.Keys)
            {
                int need = p.Max[resId] - p.Allocation.GetValueOrDefault(resId, 0);

                if (request[resId] > need)
                    return false;
            }

            // 2. Check request <= Available
            foreach (var resId in request.Keys)
            {
                if (request[resId] > available.GetValueOrDefault(resId, 0))
                    return false;
            }

            // 3. Thử cấp phát tạm thời
            foreach (var resId in request.Keys)
            {
                available[resId] -= request[resId];
                p.Allocation[resId] = p.Allocation.GetValueOrDefault(resId, 0) + request[resId];
            }

            // 4. Check SAFE
            if (IsSafeState())
            {
                return true; // OK
            }
            else
            {
                // rollback
                foreach (var resId in request.Keys)
                {
                    available[resId] += request[resId];
                    p.Allocation[resId] -= request[resId];
                }
                return false;
            }
        }
        // Ngăn chặn đợi vòng tròn bằng cách yêu cầu các tiến trình phải yêu cầu tài nguyên theo thứ tự phân cấp (hierarchical ordering)
        private bool PreventCircularWait(Process p, Resource requested)
        {
            if (p.HoldingResourceId == null) return true;

            var held = ListResource.FirstOrDefault(r => r.ResourceId == p.HoldingResourceId);
            if (held == null) return true;
            return requested.HierarchyOrder > held.HierarchyOrder;
        }
        public Process SelectVictim()
        {
            return ListProcess
                .Where(p => p.Allocation.Values.Sum() > 0)
                .OrderBy(p => p.Allocation.Values.Sum())
                .FirstOrDefault();
        }
        // Thu hồi tài nguyên bằng cách chọn một tiến trình làm nạn nhân và thu hồi tất cả tài nguyên mà nó đang giữ
        public void RecoverDeadlock()
        {
            var victim = SelectVictim();
            if (victim == null) return;

            foreach (var resId in victim.Allocation.Keys.ToList())
            {
                victim.Allocation[resId] = 0;
            }

            _db.UpdateProcess(victim);
            MessageBox.Show($"Đã thu hồi tài nguyên từ {victim.ProcessName}");

            LoadAllData();
        }
        // Tải lại dữ liệu từ database
        public void LoadAllData()
        {
            var service = new DatabaseService();

            var reData = service.GetAllResources();
            ListResource = new ObservableCollection<Resource>(reData);

            var proData = service.GetAllProcesses();
            ListProcess = new ObservableCollection<Process>(proData);
            //khởi tạo mặc i=định cho Max và Allocation nếu null để tránh lỗi khi truy cập
            foreach (var p in ListProcess)
            {
                if (p.Max == null)
                    p.Max = new Dictionary<int, int>();

                if (p.Allocation == null)
                    p.Allocation = new Dictionary<int, int>();
            }
        }




    }
}
