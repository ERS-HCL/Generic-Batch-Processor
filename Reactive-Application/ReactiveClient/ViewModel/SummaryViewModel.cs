using API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;

namespace ReactiveClient
{
    public class SummaryViewModel : BaseViewModel
    {
        JobManagerViewModel jobManagerVM;
        MainViewModel mainVM;

       // DispatcherTimer _dispatcherTimer = new DispatcherTimer();
       // bool isTimerStart = false;
        public string Header { get; set; }

        CollectionViewSource _taskStausView;
        public CollectionViewSource TaskStatusView
        {
            get { return _taskStausView; }
            private set
            {
                _taskStausView = value;
                RaisePropertyChanged("TaskStatusView");
            }
        }

        CollectionViewSource _loadBalanceView;
        public CollectionViewSource LoadBalanceView
        {
            get { return _loadBalanceView; }
            private set
            {
                _loadBalanceView = value;
                RaisePropertyChanged("LoadBalanceView");
            }
        }

        ObservableCollection<TaskStatusView> _taskStatusViewCollection = new ObservableCollection<TaskStatusView>();
        ObservableCollection<TaskStatusView> TaskStatusViewCollection
        {
            get { return _taskStatusViewCollection; }
            set
            {
                _taskStatusViewCollection = value;
                RaisePropertyChanged("TaskStatusViewCollection");
            }
        }

        ObservableCollection<LoadBalanceView> _loadBalanceViewCollection = new ObservableCollection<LoadBalanceView>();
        ObservableCollection<LoadBalanceView> LoadBalanceViewCollection
        {
            get { return _loadBalanceViewCollection; }
            set
            {
                _loadBalanceViewCollection = value;
                RaisePropertyChanged("LoadBalanceViewCollection");
            }
        }

        #region constructor
        public SummaryViewModel(MainViewModel mainVm, JobManagerViewModel jobMgrVM)
        {
            this.Header = "Summary";
            this.mainVM = mainVm;
            this.jobManagerVM = jobMgrVM;

            _taskStausView = new CollectionViewSource();
            _taskStausView.Source = _taskStatusViewCollection;
            _loadBalanceView = new CollectionViewSource();
            _loadBalanceView.Source = _loadBalanceViewCollection;

          //  _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
          //  _dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
        }
        #endregion

        //private void DispatcherTimer_Tick(object sender, EventArgs e)
        //{
        //    AddLoadBalanceViewData();
        //    AddTaskViewData();
        //}

        public void AddTaskDataforView()
        {
            //if (!isTimerStart)
            //{
            //    _dispatcherTimer.Start();
            //    isTimerStart = true;
            //}
            if (!this.jobManagerVM.IsCompleted)
                return;
            if (_loadBalanceViewCollection.Count() > 0 && _taskStatusViewCollection.Count() > 0)
            {
              //  _dispatcherTimer.Stop();
                return;
            }

            AddLoadBalanceViewData();
            AddTaskViewData();
        }
        private void AddLoadBalanceViewData()
        {
            _loadBalanceViewCollection.Clear();
            List<string> nodeList = new List<string>();
            foreach (var task in this.jobManagerVM.Tasks)
            {
                if (nodeList.Contains(task.Node))
                {
                    LoadBalanceView nodeData = _loadBalanceViewCollection.Where(x => x.Node == task.Node).FirstOrDefault();
                    Task nodeTask = new Task()
                    {
                        ID = task.TaskID,
                        Description = task.Description
                    };
                    nodeData.TaskCollection.Add(nodeTask);
                }
                else
                {
                    LoadBalanceView nodeData = new LoadBalanceView();
                    nodeData.Node = task.Node;
                    nodeData.TotalJobs = this.jobManagerVM.Tasks.Count();
                    Task nodeTask = new Task()
                    {
                        ID = task.TaskID,
                        Description = task.Description
                    };
                    nodeData.TaskCollection.Add(nodeTask);
                    _loadBalanceViewCollection.Add(nodeData);
                    nodeList.Add(task.Node);
                }
            }

            nodeList.Clear();
        }

        private void AddTaskViewData()
        {
            _taskStatusViewCollection.Clear();            
            var statusValues = Enum.GetValues(typeof(JobStatus));
            foreach (JobStatus status in statusValues)
            {
                if (status == JobStatus.NotStarted || status == JobStatus.Started || status == JobStatus.Timeout)
                    continue;

                TaskStatusView taskData = new TaskStatusView();
                taskData.TaskStatus = status.ToString();
                taskData.TotalJobs = this.jobManagerVM.Tasks.Count();
                var taskList = this.jobManagerVM.Tasks.Where(x => x.Status == status.ToString()).ToList();
                foreach (var task in taskList)
                {                    
                    Task statusTask = new Task()
                    {
                        ID = task.TaskID,
                        Description = task.Description
                    };
                    taskData.TaskCollection.Add(statusTask);
                }
                _taskStatusViewCollection.Add(taskData);
            }
        }        
    }    
}
