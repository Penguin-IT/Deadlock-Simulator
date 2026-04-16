
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Deadlock_simulator.Models;
using Deadlock_simulator.Services;

namespace Deadlock_simulator.ViewModels
{
    public class ViewModel1 : BaseViewModel
    {
        private  DatabaseService _db;

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

        //khoi tao dulieu
        public ViewModel1()
        {
            _db=new DatabaseService();

            ListResource = new ObservableCollection<Resource>();
            ListProcess = new ObservableCollection<Process>();


        }


        // Phát hiện deadlock
        //kiem traa vòng lạp
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
            return false; // Quan trọng: phải có dòng này!
        }
        private Dictionary<string, List<string>> BuildGraph()
        {
            var graph = new Dictionary<string, List<string>>();
            var processes = ListProcess.ToList();
            var resources = ListResource.ToList();

            foreach (var p in processes)
            {
                if (!graph.ContainsKey(p.ProcessName)) graph[p.ProcessName] = new List<string>();

                // Cạnh cấp phát: Resource -> Process 
               if(p.HoldingResourceId !=null)
                {
                    var res = resources.FirstOrDefault(t => t.ResourceId == p.HoldingResourceId);
                    if(res!=null)
                        graph[p.ProcessName].Add(res.ResourceName);
                }

                // Cạnh yêu cầu: Process -> Resource (Ai đang đợi?)
                if (p.WaitingResourceId != null)
                {
                    var res = resources.FirstOrDefault(r => r.ResourceId == p.WaitingResourceId);
                    if (res != null)
                    {
                        graph[p.ProcessName].Add(res.ResourceName);
                    }
                }
            }
            return graph;
        }
        //Thuật toán loại trừ tương hỗ 






        // Thuật toán Đợi vòng















        public void LoadAllData()
        {
            var service = new DatabaseService();

            var reData = service.GetAllResources();
            ListResource = new ObservableCollection<Resource>(reData);

            var proData = service.GetAllProcesses();
            ListProcess = new ObservableCollection<Process>(proData);
        }
    }
}
