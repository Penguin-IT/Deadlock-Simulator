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
       public Dictionary<int, int> CalculateAvailable()
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

    
    return ConfirmDeadlockMultiInstance();
}

        public bool ConfirmDeadlockMultiInstance()
{

    var work = new Dictionary<int, int>(CalculateAvailable());
    

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
              
                foreach (var alloc in p.Allocation)
                {
                    work[alloc.Key] = work.GetValueOrDefault(alloc.Key, 0) + alloc.Value;
                }
                finish[p.ProcessName] = true;
                progress = true;
            }
        }
    } while (progress);

  
    return finish.Values.Any(f => f == false);
}
 

 

        private bool StrictCurbRequest(Process p, Resource r)
        {

            if (r == null) return false;
      
            if (!PreventCircularWait(p, r))
                return false;

      
            int currentWaiting = ListProcess.Count(proc => proc.WaitingResourceId == r.ResourceId);
            int MAX_WAITING = r.Total * 2; if (currentWaiting >= MAX_WAITING)
                return false;

      
            if (r.IsShareable)
                return true;

            return true;
        }

     private Dictionary<string, List<string>> BuildGraph()
{
    var map = new Dictionary<string, List<string>>();
    var resources = ListResource.Where(r => !r.IsShareable).ToList();
    var processes = ListProcess.ToList();

    foreach (var p in processes) map[p.ProcessName] = new List<string>();
    foreach (var r in resources) map[r.ResourceName] = new List<string>();


    foreach (var res in resources)
    {
  
       if (res.CurrentHolders != null)
{
    foreach (var holderName in res.CurrentHolders)
            if (map.ContainsKey(res.ResourceName))
                map[res.ResourceName].Add(holderName);
        }
    }


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
            var stack = new List<string>(); 

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


    var holder = ListProcess.FirstOrDefault(p => p.ProcessName == res.CurrentHolder);
    
    if (holder != null)
    {
    
        holder.HoldingResourceId = null;
        if (holder.Allocation.ContainsKey(resourceId))
            holder.Allocation[resourceId] = 0;
            
        _db.UpdateProcess(holder); 
    }


    res.CurrentHolder = null; 

   
    if (res.WaitingQueue != null && res.WaitingQueue.Count > 0)
    {
        var nextProcess = res.WaitingQueue.Dequeue();
        
        res.CurrentHolder = nextProcess.ProcessName;
        nextProcess.HoldingResourceId = resourceId;
        nextProcess.WaitingResourceId = null; 
        
        _db.UpdateProcess(nextProcess);
        MessageBox.Show($"Đã thu hồi từ {holder?.ProcessName} và cấp cho {nextProcess.ProcessName}");
    }
    
    LoadAllData(); 
        }

        //hàm kiểm tra trạng thái an toàn của hệ thống (Safe State)
       public bool IsSafeState()
        {
         
            var work = new Dictionary<int, int>(CalculateAvailable());
            
          
                    var finish = ListProcess.ToDictionary(p => p.ProcessName, p => false);

            bool progress;
            do {
                    progress = false;
                foreach (var p in ListProcess)
                {            if (finish[p.ProcessName]) continue;

             
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
           
            if (!res.IsShareable && !PreventCircularWait(p, res))
            {
                MessageBox.Show($"[NGĂN CHẶN]Vi phạm thứ tự tài nguyên! {p.ProcessName} không   hể xin {res.ResourceName}.", "Thông báo");
                return false;
            }
           
            // --- LOGIC TRÁNH Deadlock (AVOIDANCE) ---
          
            int need = p.Max.GetValueOrDefault(resourceId, 0) - p.Allocation.GetValueOrDefault(resourceId, 0);
            if (amount > need)
            {
                MessageBox.Show($"[TỪ CHỐI] Số lượng xin ({amount}) vượt quá nhu cầu còn lại ({need}) của tiến trình!", "Lỗi Banker");
                return false;
            }

            if (amount > available.GetValueOrDefault(resourceId, 0))
            {
                p.WaitingResourceId = resourceId;
                if (!res.WaitingQueue.Contains(p)) res.WaitingQueue.Enqueue(p);
                MessageBox.Show($"[ĐỢI] Hệ thống không đủ {res.ResourceName}. Tiến trình đã vào hàng chờ.", "Thông báo");
             
                if (ConfirmDeadlockMultiInstance()) 
                {
                    MessageBox.Show("CẢNH BÁO: Việc chờ đợi này sẽ gây ra Deadlock!", "Cảnh báo");
                }
                return false;
            }

           
            UpdateAllocation(p, res, amount);

            if (IsSafeState())
            {
                if (res.CurrentHolders == null) res.CurrentHolders = new List<string>();
                for (int i = 0; i < amount; i++) res.CurrentHolders.Add(p.ProcessName);
                
                p.WaitingResourceId = null;
                var currentHeld = ListResource.FirstOrDefault(x => x.ResourceId == p.HoldingResourceId);
                if (currentHeld == null || res.HierarchyOrder > currentHeld.HierarchyOrder)
                {
                    p.HoldingResourceId = res.ResourceId;
                }
                _db.UpdateProcess(p);
                return true;
            }
            else
            {
              
                p.Allocation[resourceId] -= amount;
                MessageBox.Show("[TỪ CHỐI] Cấp phát này sẽ đưa hệ thống vào trạng thái không an toàn!", "Thông báo Banker");
                return false;
            }
        }


private void UpdateAllocation(Process p, Resource r, int amount)
{
    if (p == null || r == null || amount == 0) return;

    p.Allocation ??= new Dictionary<int, int>();
    r.CurrentHolders ??= new List<string>();
    r.WaitingQueue ??= new Queue<Process>();

    int currentAlloc = p.Allocation.GetValueOrDefault(r.ResourceId, 0);
    int newAlloc = currentAlloc + amount;

    if (newAlloc < 0)
        throw new InvalidOperationException("Allocation âm!");

  
    if (newAlloc > 0)
        p.Allocation[r.ResourceId] = newAlloc;
    else
        p.Allocation.Remove(r.ResourceId);

  
    if (amount > 0)
    {
        for (int i = 0; i < amount; i++)
            r.CurrentHolders.Add(p.ProcessName);

    
        bool finished = p.Max.All(kv =>
        {
            int need = kv.Value - p.Allocation.GetValueOrDefault(kv.Key, 0);
            return need <= 0;
        });

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

       
        private bool PreventCircularWait(Process p, Resource requested)
        {
            var heldResources = ListResource.Where(r => p.Allocation.GetValueOrDefault(r.ResourceId, 0) > 0).ToList();
            if (!heldResources.Any())
            {
                return true;
            }
            int maxHeldOrder = heldResources.Max(r => r.HierarchyOrder);
            return requested.HierarchyOrder > maxHeldOrder;
        }
        public Process SelectVictim()
        {
            return ListProcess
                .Where(p => p.Allocation.Values.Sum() > 0)
                .OrderBy(p => p.Allocation.Values.Sum())
                .FirstOrDefault();
        }
      
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
            MessageBox.Show($"Đã thu hồi tài nguyên từ {victim.ProcessName}");

  
        }

 
public void AnalyzeMinimumRecovery()
{

    var available = CalculateAvailable();
    
   
    var stuckProcesses = ListProcess.Where(p => p.WaitingResourceId != null).ToList();

    if (!stuckProcesses.Any()) return;

    var bestCandidate = stuckProcesses
        .Select(p => new {
            Process = p,
           
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

        public void LoadAllData()
        {
                var service = new DatabaseService();
            var reData = service.GetAllResources();
                ListResource = new ObservableCollection<Resource>(reData);
            var proData = service.GetAllProcesses();
            ListProcess = new ObservableCollection<Process>(proData);
        
            foreach (var p in ListProcess)
            {
                if (p.Max == null)
                    p.Max = new Dictionary<int, int>();

                if (p.Allocation == null)
                    p.Allocation = new Dictionary<int, int>();
                if (p.Allocation.Count > 0)
                {
                  
                    var highestResId = p.Allocation.Keys
                        .Select(id => ListResource.FirstOrDefault(r => r.ResourceId == id))
                        .Where(r => r != null)
                        .OrderByDescending(r => r.HierarchyOrder)
                        .Select(r => r.ResourceId)
                        .FirstOrDefault();

                    p.HoldingResourceId = highestResId != 0 ? highestResId : (int?)null;
                }
            }
        }
        

    }
}
