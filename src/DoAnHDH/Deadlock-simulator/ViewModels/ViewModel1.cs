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
using System.Diagnostics;

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

    bool hasCycle = false;
    HashSet<string> visited = new();
    HashSet<string> stack = new();

    foreach (var node in graph.Keys)
    {
        if (HasCycle(node, graph, visited, stack))
        {
            hasCycle = true;
            break;
        }
    }

    if (!hasCycle) return false;

    // Nếu có cycle → phải confirm bằng Banker
    return ConfirmDeadlockMultiInstance();
}

        public bool ConfirmDeadlockMultiInstance()
{
    // 1. Khởi tạo Work = Available hiện tại
    var work = new Dictionary<int, int>(CalculateAvailable());
    
    // 2. Khởi tạo Finish
    // Nếu Allocation = 0, coi như đã xong (true), ngược lại là false
    var finish = ListProcess.ToDictionary(
        p => p.ProcessName,
        p => p.Allocation.Values.Sum() == 0
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

          // 1. Tìm tiến trình đang giữ
          var holderName = res.CurrentHolders?.FirstOrDefault();
          var holder = holderName != null ? ListProcess.FirstOrDefault(p => p.ProcessName == holderName) : null;
          
          if (holder != null)
          {
              // Giải phóng phía Process
              UpdateAllocation(holder, res, -1);
              _db.UpdateProcess(holder); // Cập nhật xuống Database
          }

          // 3. SAU KHI GIẢI PHÓNG, CẤP NGAY CHO TIẾN TRÌNH ĐANG ĐỢI (Nếu có)
          if (res.WaitingQueue != null && res.WaitingQueue.Count > 0)
          {
              var nextProcess = res.WaitingQueue.Peek();
              UpdateAllocation(nextProcess, res, 1);
              
              // Đảm bảo phá vỡ trạng thái chờ
              if (res.WaitingQueue.Contains(nextProcess)) {
                  res.WaitingQueue = new Queue<Process>(res.WaitingQueue.Where(x => x.ProcessId != nextProcess.ProcessId));
                  nextProcess.WaitingResourceId = null;
              }

              _db.UpdateProcess(nextProcess);
              Console.WriteLine($"Đã thu hồi 1 đơn vị từ {holder?.ProcessName} và cấp cho {nextProcess.ProcessName}");
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

// Yêu cầu tài nguyên cho tiến trình, trả về true nếu được cấp phát, false nếu bị từ chối
   public bool RequestResource(Process p, int resourceId, int amount, bool isRetry = false)
{
    var res = ListResource.FirstOrDefault(r => r.ResourceId == resourceId);
    if (res == null) return false;

    // --- PREVENTION ---
    if (!res.IsShareable && !PreventCircularWait(p, res))
    {
        if (isRetry) return false; //tránh loop vô hạn

        var violatingResources = p.Allocation
            .Where(a => a.Value > 0)
            .Select(a => ListResource.FirstOrDefault(r => r.ResourceId == a.Key))
            .Where(r => r != null && r.HierarchyOrder > res.HierarchyOrder)
            .ToList();

        foreach (var v in violatingResources)
            ReleaseResourceCompletely(p, v);

        return RequestResource(p, resourceId, amount, true); // retry 1 lần
    }

    // kiểm tra nếu yêu cầu vượt quá Need thì từ chối
    int need = p.Max.GetValueOrDefault(resourceId, 0)
              - p.Allocation.GetValueOrDefault(resourceId, 0);
    if (amount > need) return false;

    // kiểm tra nếu yêu cầu vượt quá Available thì từ chối và cho vào hàng đợi chờ
    var available = CalculateAvailable();
    if (amount > available.GetValueOrDefault(resourceId, 0))
    {
        p.WaitingResourceId = resourceId;
        if (!res.WaitingQueue.Contains(p)) res.WaitingQueue.Enqueue(p);
        return false;
    }

    // kiểm tra lại trạng thái an toàn sau khi giả định cấp phát
    UpdateAllocation(p, res, amount);

    if (IsSafeState())
    {
        CommitAllocation(p, res, amount);
        p.WaitingResourceId = null; //xoa trạng thái chờ nếu có
        return true;
    }
    else
    {
        UpdateAllocation(p, res, -amount);
        p.WaitingResourceId = null; 
        return false;
    }
}

   private void ReleaseResourceCompletely(Process p, Resource r)
    {
        int currentAlloc = p.Allocation.GetValueOrDefault(r.ResourceId, 0);
        if (currentAlloc <= 0) return;

        UpdateAllocation(p, r, -currentAlloc);

        if (p.WaitingResourceId == r.ResourceId)
            p.WaitingResourceId = null;

        _db.UpdateProcess(p);

    }
private void UpdateAllocation(Process p, Resource r, int amount)
{
    if (p == null || r == null || amount == 0) return;
// Khởi tạo nếu null để tránh lỗi khi truy cập
    p.Allocation ??= new Dictionary<int, int>();
    r.CurrentHolders ??= new List<string>();
    r.WaitingQueue ??= new Queue<Process>();

    int currentAlloc = p.Allocation.GetValueOrDefault(r.ResourceId, 0);
    int newAlloc = currentAlloc + amount;
// Kiểm tra nếu Allocation âm sẽ gây gây lỗi
    if (newAlloc < 0)
    {
        return;
    }
    // bên Process cập nhật Allocation
    if (newAlloc > 0)
        p.Allocation[r.ResourceId] = newAlloc;
    else
        p.Allocation.Remove(r.ResourceId);

    // bên Resource cập nhật CurrentHolders
    if (amount > 0)
    {
        for (int i = 0; i < amount; i++)
            r.CurrentHolders.Add(p.ProcessName);

        // kiểm tra hoàn thành toàn bộ
        bool finished = p.Max.All(kv =>
        {
            int need = kv.Value - p.Allocation.GetValueOrDefault(kv.Key, 0);
            return need <= 0;
        });
// Nếu hoàn thành, loại bỏ khỏi hàng đợi chờ và reset WaitingResourceId
        if (finished)
        {
            r.WaitingQueue = new Queue<Process>(
                r.WaitingQueue.Where(x => x.ProcessId != p.ProcessId)
            );
            p.WaitingResourceId = null;
        }
    }
    else
    {
        int toRemove = Math.Min(currentAlloc, Math.Abs(amount));
        for (int i = 0; i < toRemove; i++)
        {
            int index = r.CurrentHolders.IndexOf(p.ProcessName);
            if (index >= 0)
                r.CurrentHolders.RemoveAt(index);
        }
    }
}

        // Ngăn chặn đợi vòng tròn bằng cách yêu cầu các tiến trình phải yêu cầu tài nguyên theo thứ tự phân cấp (hierarchical ordering)
        private bool PreventCircularWait(Process p, Resource requested)
        {
            if (p.Allocation == null || !p.Allocation.Any(a => a.Value > 0)) return true;

            var maxHeldOrder = p.Allocation.Where(a => a.Value > 0)
                                           .Select(a => ListResource.FirstOrDefault(r => r.ResourceId == a.Key)?.HierarchyOrder ?? -1)
                                       ests    .Max();

            return requested.HierarchyOrder > maxHeldOrder;
        }

   // Gán thứ tự phân cấp cho tài nguyên dựa trên độ khan hiếm và mức độ tranh chấp
        public void AssignOrderByScarcity()
        {
            if (ListResource == null || !ListResource.Any()) return;
            if (ListResource.All(r => r.HierarchyOrder != 0)) return;

            const int STEP = 10;

            try
            {
                // 1. Tiền tính toán số lượng tiến trình đang đợi cho từng tài nguyên để tối ưu hiệu năng sắp xếp
                var waitingCounts = ListProcess
                    .Where(p => p.WaitingResourceId != null)
                    .GroupBy(p => p.WaitingResourceId.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 2. Sắp xếp tài nguyên theo chiến lược Ngăn chặn Deadlock (Prevention)
                // Chiến lược: Tài nguyên dồi dào, dễ mượn, ít tranh chấp -> ID thấp (lấy trước)
                //             Tài nguyên khan hiếm, đang bị nghẽn -> ID cao (lấy sau)
                var sortedResources = ListResource
                    .OrderByDescending(r => r.Total)                                // Ưu tiên số lượng nhiều trước
                    .ThenBy(r => waitingCounts.GetValueOrDefault(r.ResourceId, 0))   // Ưu tiên tài nguyên ít bị tranh chấp
                    .ThenByDescending(r => r.IsShareable)                           // Tài nguyên dùng chung được ưu tiên ID thấp
                    .ThenBy(r => r.ResourceId)                                      // Đảm bảo thứ tự duy nhất
                    .ToList();

                // 3. Gán thứ tự mới
                int currentOrder = STEP;
                foreach (var r in sortedResources)
                {
                    r.HierarchyOrder = currentOrder;
                    currentOrder += STEP;
                }

           OnPropertyChanged(nameof(ListResource));

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Lỗi khi gán thứ tự phân cấp: " + ex.Message);
            }
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
                var res = ListResource.FirstOrDefault(r => r.ResourceId == resId);
                if (res != null)
                {
                    UpdateAllocation(victim, res, -victim.Allocation[resId]);
                }
            }

            _db.UpdateProcess(victim);
            Debug.WriteLine($"Đã thu hồi tài nguyên từ {victim.ProcessName}");

  
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
        Debug.WriteLine($"[Phân tích] Để giải phóng Deadlock nhanh nhất:\n" +
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
