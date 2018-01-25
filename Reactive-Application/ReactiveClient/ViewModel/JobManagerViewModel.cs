using Akka.Actor;
using Akka.Routing;
using Microsoft.Practices.Prism.Commands;
using ReactiveClient.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ReactiveClient
{
    public class JobManagerViewModel : BaseViewModel
    {
        ObservableCollection<TaskItem> lstTasks = new ObservableCollection<TaskItem>();
        CollectionViewSource _taskView;
        IActorRef _jobPoolManagerActor;
        /// <summary>
        /// stop watch to calculate time for processing all jobs 
        /// </summary>
        Stopwatch _stopWatch = new Stopwatch();

        DispatcherTimer _dispatcherTimer = new DispatcherTimer();

        MainViewModel theVM;
        public CollectionViewSource TaskView
        {
            get { return _taskView; }
            private set
            {
                _taskView = value;
                RaisePropertyChanged("TaskView");
            }
        }

        public ObservableCollection<TaskItem> Tasks
        {
            get { return lstTasks; }
            set
            {
                lstTasks = value;
                RaisePropertyChanged("Tasks");
            }
        }

        private string _taskFileName = Path.Combine(Environment.CurrentDirectory, "JobPool.txt");
        public string TaskFileName
        {
            get { return _taskFileName; }
            set
            {
                this._taskFileName = value;
                this.RaisePropertyChanged("TaskFileName");
            }
        }

        /// <summary>
        /// The browse dialog
        /// </summary>
        private Microsoft.Win32.OpenFileDialog _browseDialog = new Microsoft.Win32.OpenFileDialog();

        private bool _enableGetTaskButton;
        public bool EnableGetTaskButton
        {
            get { return _enableGetTaskButton; }
            set
            {
                _enableGetTaskButton = value;
                this.RaisePropertyChanged("EnableGetTaskButton");
            }
        }

        private bool _enableProcessButton;
        public bool EnableProcessTaskButton
        {
            get { return _enableProcessButton; }
            set
            {
                _enableProcessButton = value;
                this.RaisePropertyChanged("EnableProcessTaskButton");
            }
        }

        private ICommand _getTaskCommand;
        public ICommand GetTasksCommand
        {
            get
            {
                if (this._getTaskCommand == null)
                {
                    this._getTaskCommand = new DelegateCommand(ExecuteGetTaskCommand);
                }

                return this._getTaskCommand;
            }
        }

        private ICommand _processTasksCommand;
        public ICommand ProcessTasksCommand
        {
            get
            {
                if (this._processTasksCommand == null)
                {
                    this._processTasksCommand = new DelegateCommand(ExecuteProcessTasksCommand);
                }

                return this._processTasksCommand;
            }
        }

        private ICommand _browseJobPoolData;
        public ICommand BrowseJobPoolDataFile
        {
            get
            {
                if (this._browseJobPoolData == null)
                    this._browseJobPoolData = new DelegateCommand(ExecuteBrowsePoolData);

                return this._browseJobPoolData;
            }
        }

        string _totalDuration;
        public string TotalDuration
        {
            get { return _totalDuration; }
            set
            {
                _totalDuration = value;
                this.RaisePropertyChanged("TotalDuration");
            }
        }

        public string Header { get; set; }

        public bool IsCompleted { get; set; }
        public JobManagerViewModel(MainViewModel viewModel)
        {
            this.theVM = viewModel;            
            this.Header = "Job Manager";
            this.IsCompleted = false;
            _taskView = new CollectionViewSource();
            _taskView.Source = lstTasks;
            EnableGetTaskButton = true;
            EnableProcessTaskButton = false;
            _dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            CreateActorSystem();
            
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            long totalProcessedTime = _stopWatch.ElapsedMilliseconds;
            TimeSpan taskDuration = TimeSpan.FromMilliseconds(totalProcessedTime);
            TotalDuration = taskDuration.ToString(@"hh\:mm\:ss");
           // DateTime.Now.ToLongTimeString();
           // CommandManager.InvalidateRequerySuggested();
        }

        private void CreateActorSystem()
        {
            var actorSystem = ActorSystem.Create("batchProcessor");
            var api = actorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "api");

            _jobPoolManagerActor = actorSystem.ActorOf(
                Props.Create<JobPoolControllerActor>(api, this), "jobpoolmanager");
        }
        
        private void ExecuteBrowsePoolData()
        {
            _browseDialog.Reset();
            _browseDialog.DefaultExt = ".txt";
            _browseDialog.Filter = "txt file |*.txt";
            _browseDialog.CheckPathExists = false;
            Nullable<bool> result = _browseDialog.ShowDialog();
            if (true == result)
            {
                this.TaskFileName = _browseDialog.FileName;
                EnableGetTaskButton = true;
            }
        }

        private void ExecuteGetTaskCommand()
        {
            string fileName = this.TaskFileName;
            bool fileFound = File.Exists(fileName);
            if (!fileFound)
            {
                MessageBox.Show("Task file not found.", "Batch Processor Client");
                return;
            }
            lstTasks.Clear();

            if(!CheckSchedulerSettingPresent())
            {
                MessageBox.Show("Please check scheduler settings in config file.");
                return;
            }

            _stopWatch.Reset();
            StopTimer();

            int timeOut = Utility.GetSettingValue("TimeOutInMinutes");
            var fileLines = File.ReadAllLines(fileName);
            int jobId = 0;
            foreach (var line in fileLines)
            {
                string taskStr = line.Trim();
                if (!string.IsNullOrEmpty(taskStr))
                {
                    jobId++;
                    lstTasks.Add(new TaskItem(jobId, taskStr, timeOut));
                }
            }
            EnableProcessTaskButton = true;
        }
                
        private void ExecuteProcessTasksCommand()
        {
            if (this._jobPoolManagerActor != null)
            {                
                this._jobPoolManagerActor.Tell(new ScheduleJobMessage());
                EnableGetTaskButton = false;
                EnableProcessTaskButton = false;
                _stopWatch.Start();
                _dispatcherTimer.Start();
            }
        }

        public void StopTimer()
        {
            _stopWatch.Stop();
            _dispatcherTimer.Stop();
        }

        private bool CheckSchedulerSettingPresent()
        {
            IList<string> settings = new List<string>() { "TimeOutInMinutes", "MaxNoOfAttempts", "InitialDelayInMinutes", "IntervalInSeconds" };
            var count = settings.Where(x => Utility.GetSettingValue(x.ToString()) == 0).Count();
            return count == 0;
        }
    }
}
