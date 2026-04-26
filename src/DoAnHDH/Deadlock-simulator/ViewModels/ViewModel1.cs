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
                    .Where(p => p.Allocation != null && p.Allocation.ContainsKey(r.ResourceId))
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
            return ConfirmDeadlockMultiInstance(); // Kiểm tra thêm với thuật toán Banker nếu có tài nguyên đa thực thể
        }

        public bool ConfirmDeadlockMultiInstance()
{
    // 1. Khởi tạo Work = Available hiện tại
    var work = new Dictionary<int, int>(CalculateAvailable());
    
    // 2. Khởi tạo Finish
    // Nếu Allocation = 0, coi như đã xong (true), ngược lại là false
    var finish = ListProcess.ToDictionary(
    p => p.ProcessName,
    p => false
);

    bool progress;
    do
    {
        progress = false;
        foreach (var p in ListProcess.Where(proc => !finish[proc.ProcessName]))
        {
            // Kiểm tra: Yêu cầu hiện tại (Request) <= Work
            // Lưu ý: Request ở đây là p.WaitingResourceId
            bool canBeSatisfied = true;
            
            if (p.WaitingResourceId.HasValue)
            {
               foreach (var resId in p.Max.Keys)
                {
                    int need = p.Max[resId] - p.Allocation.GetValueOrDefault(resId, 0);
                    if (need > work.GetValueOrDefault(resId, 0))
                    {
                        canBeSatisfied = false;
                        break;
                    }
                }
            }

            if (canBeSatisfied)
            {
                // Giả định tiến trình này nhận được tài nguyên, chạy xong và trả lại toàn bộ
                foreach (var alloc in p.Allocation)
                {
                    work[alloc.Key] = work.GetValueOrDefault(alloc.Key, 0) + alloc.Value;
                }
                finish[p.ProcessName] = true;
                progress = true;
            }
        }
    } while (progress);

    // 3. Nếu tồn tại bất kỳ tiến trình nào Finish == false -> DEADLOCK THẬT
    return finish.Values.Any(f => f == false);
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
// Xây dựng đồ thị Resource Allocation Graph từ dữ liệu hiện tại
     private Dictionary<string, List<string>> BuildGraph()
{
    var map = new Dictionary<string, List<string>>();
    var resources = ListResource.Where(r => !r.IsShareable).ToList();
    var processes = ListProcess.ToList();

    foreach (var p in processes) map[p.ProcessName] = new List<string>();
    foreach (var r in resources) map[r.ResourceName] = new List<string>();

    // 1. Cạnh Allocation: Resource -> Process
    foreach (var res in resources)
    {
        // Duyệt qua tất cả những người đang giữ thực thể của Resource này
       if (res.CurrentHolders != null)
{
    foreach (var holderName in res.CurrentHolders)
            if (map.ContainsKey(res.ResourceName))
                map[res.ResourceName].Add(holderName);
        }
    }

    // 2. Cạnh Request: Process -> Resource
    foreach (var p in processes)
    {
        if (p.WaitingResourceId != null)
        {
            var res = resources.FirstOrDefault(r => r.ResourceId == p.WaitingResourceId);
            if (res != null) map[p.ProcessName].Add(res.ResourceName);
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
    var res = ListResource.FirstOrDefault(r => r.ResourceId == resourceId);
    if (res == null) return;

    // 1. Tìm tiến trình đang giữ dựa trên CurrentHolder của Resource
    var holder = ListProcess.FirstOrDefault(p => p.ProcessName == res.CurrentHolder);
    
    if (holder != null)
    {
        // Giải phóng phía Process
        holder.HoldingResourceId = null;
        if (holder.Allocation.ContainsKey(resourceId))
            holder.Allocation[resourceId] = 0;
            
        _db.UpdateProcess(holder); // Cập nhật xuống Database
    }

    // 2. GIẢI PHÓNG PHÍA RESOURCE
    res.CurrentHolder = null; 

    // 3. SAU KHI GIẢI PHÓNG, CẤP NGAY CHO TIẾN TRÌNH ĐANG ĐỢI (Nếu có)
    if (res.WaitingQueue != null && res.WaitingQueue.Count > 0)
    {
        var nextProcess = res.WaitingQueue.Dequeue();
        
        res.CurrentHolder = nextProcess.ProcessName;
        nextProcess.HoldingResourceId = resourceId;
        nextProcess.WaitingResourceId = null; // Phá vỡ cạnh đợi trên đồ thị
        
        _db.UpdateProcess(nextProcess);
        MessageBox.Show($"Đã thu hồi từ {holder?.ProcessName} và cấp cho {nextProcess.ProcessName}");
    }
    
    LoadAllData(); 
        }

        //hàm kiểm tra trạng thái an toàn của hệ thống (Safe State)
       public bool IsSafeState()
        {
            // 1. Tạo bản Clone của Available (Work)
            var work = new Dictionary<int, int>(CalculateAvailable());
            
            // 2. Tạo bản Clone của Finish
                    var finish = ListProcess.ToDictionary(p => p.ProcessName, p => false);

            bool progress;
            do {
                    progress = false;
                foreach (var p in ListProcess)
                {            if (finish[p.ProcessName]) continue;

                    // Kiểm tra: Need <= Work
                    bool canFinish = true;
                    foreach (var resId in p.Max.Keys)
                    {
                        int need = p.Max[resId] - p.Allocation.GetValueOrDefault(resId, 0);
                        if (need > work.GetValueOrDefault(resId, 0))
                        {
                            canFinish = false;
                            break;
                        }
                            }

                    if (canFinish)
                    {
                        // Giả định tiến trình xong -> Trả lại toàn bộ Allocation vào Work
                        foreach (var alloc in p.Allocation)
                        {
                            work[alloc.Key] = work.GetValueOrDefault(alloc.Key, 0) + alloc.Value;
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
    var work = CalculateAvailable();
    var finish = ListProcess.ToDictionary(p => p.ProcessName, _ => false);
    var sequence = new List<string>();

    bool found;
    do
    {
        found = false;
        foreach (var p in ListProcess.Where(p => !finish[p.ProcessName]))
        {
            bool canFinish = ListResource.All(r =>
            {
                int resId = r.ResourceId;
                int need = Math.Max(0,
                    p.Max.GetValueOrDefault(resId, 0) -
                    p.Allocation.GetValueOrDefault(resId, 0));

                return need <= work.GetValueOrDefault(resId, 0);
            });

            if (!canFinish) continue;

            // giải phóng tài nguyên
            foreach (var alloc in p.Allocation)
            {
                work[alloc.Key] = work.GetValueOrDefault(alloc.Key, 0) + alloc.Value;
            }

            finish[p.ProcessName] = true;
            sequence.Add(p.ProcessName);
            found = true;
        }

    } while (found);
    return (finish.Values.All(f => f), sequence);
}


        // Kiểm tra yêu cầu tài nguyên theo thuật toán Banker có được chấp nhận không
    public bool RequestResource(Process p, int resourceId, int amount)
        {
            var available = CalculateAvailable();
            var res = ListResource.FirstOrDefault(r => r.ResourceId == resourceId);
            if (res == null) return false;

            // --- LOGIC NGĂN CHẶN (PREVENTION) ---
            // Kiểm tra thứ tự phân cấp (Circular Wait Prevention)
            if (!res.IsShareable && !PreventCircularWait(p, res))
            {
                MessageBox.Show("Vi phạm thứ tự tài nguyên!");
                return false;
            }

            // --- LOGIC TRÁNH Deadlock (AVOIDANCE) ---
            // 1. Check request <= Need
            int need = p.Max.GetValueOrDefault(resourceId, 0) - p.Allocation.GetValueOrDefault(resourceId, 0);
            if (amount > need) return false;

            // 2. Check request <= Available
            if (amount > available.GetValueOrDefault(resourceId, 0))
            {
                p.WaitingResourceId = resourceId;
                if (!res.WaitingQueue.Contains(p)) res.WaitingQueue.Enqueue(p);
                
                // Kiểm tra xem việc đợi này có gây Deadlock thật không (Detection)
                if (ConfirmDeadlockMultiInstance()) 
                {
                    MessageBox.Show("Phát hiện Deadlock thật sự!");
                }
                return false;
            }

            // 3. Thử cấp phát (Banker Test)
            p.Allocation[resourceId] = p.Allocation.GetValueOrDefault(resourceId, 0) + amount;

            if (IsSafeState())
            {
                // cấp chính thức
                if (res.CurrentHolders == null) res.CurrentHolders = new List<string>();
                for (int i = 0; i < amount; i++) res.CurrentHolders.Add(p.ProcessName);
                
                p.WaitingResourceId = null;
                _db.UpdateProcess(p);
                return true;
            }
            else
            {
                //roollback
                p.Allocation[resourceId] -= amount;
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

        // thử hàm mới xem được Không
public void AnalyzeMinimumRecovery()
{
    // 1. Lấy tài nguyên hiện có
    var available = CalculateAvailable();
    
    // 2. Lọc ra các tiến trình đang đợi (Waiting) và chưa hoàn thành
    var stuckProcesses = ListProcess.Where(p => p.WaitingResourceId != null).ToList();

    if (!stuckProcesses.Any()) return;
// chọn tiến trình có số lượng tài nguyên còn thiếu ít nhất để giải phóng nhanh nhất
    var bestCandidate = stuckProcesses
        .Select(p => new {
            Process = p,
            // Tính số lượng còn thiếu của tài nguyên nó đang đợi
            MissingAmount = CalculateMissing(p, available)
        })
        .OrderBy(x => x.MissingAmount)
        .FirstOrDefault();

    if (bestCandidate != null && bestCandidate.MissingAmount > 0)
    {
        var res = ListResource.FirstOrDefault(r => r.ResourceId == bestCandidate.Process.WaitingResourceId);
        
        MessageBox.Show($"[Phân tích] Để giải phóng Deadlock nhanh nhất:\n" +
                        $"- Ưu tiên : {bestCandidate.Process.ProcessName}\n" +
                        $"- Tài nguyên đang đợi: {res?.ResourceName}\n" +
                        $"- Số lượng cần thêm: {bestCandidate.MissingAmount} đơn vị.");
    }
}
// Hàm tính toán số lượng tài nguyên còn thiếu để tiến trình có thể tiếp tục
    private int CalculateMissing(Process p, Dictionary<int, int> available)
    {
        if (p.WaitingResourceId == null) return 0;
        
        int resId = p.WaitingResourceId.Value;
        // Need thực tế = Max[resId] - Allocation[resId]
        int need = p.Max.ContainsKey(resId) ? (p.Max[resId] - p.Allocation.GetValueOrDefault(resId, 0)) : 1;
        int currentAvailable = available.GetValueOrDefault(resId, 0);
        
        return need > currentAvailable ? (need - currentAvailable) : 0;
    }


// Thử tới đây


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
