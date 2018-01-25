using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveClient
{
    public class Task
    {
        public int ID { get; set; }
        public string Description { get; set; }
    }
    public abstract class TaskView : BaseViewModel
    {
        private string tooltipHeader;
        //private string tasks;
        //private double percentage;
        //private int taskCount;

        public string ToolTipHeader
        {
            get { return tooltipHeader; }
            set
            {
                this.tooltipHeader = value;
                RaisePropertyChanged("ToolTipHeader");
            }
        }
        public string TaskIds
        {
            get
            {
                StringBuilder taskIds = new StringBuilder();
                foreach(Task task in TaskCollection)
                {
                    taskIds.Append(task.ID.ToString() + ",");
                }
                return taskIds.ToString().TrimEnd(',');
            }          
        }

        public double Percentage
        {
            get
            {
                double result = (double)TaskCount / (double)TotalJobs;
                double percentageValue = System.Math.Round(result, 2);
                return percentageValue;
            }
        }

        public int TaskCount
        {
            get { return TaskCollection.Count(); }
        }

        public int TotalJobs { get; set; }
        public Collection<Task> TaskCollection { get; set; }
        protected TaskView()
        {
            TaskCollection = new Collection<Task>();
        }
    }
    public class LoadBalanceView : TaskView
    {        
        private string node;

        public string Node
        {
            get { return node; }
            set
            {
                node = value;
                RaisePropertyChanged("Node");
                this.ToolTipHeader = value;
            }
        }
        
        public LoadBalanceView():base()
        {
        }
    }

    public class TaskStatusView : TaskView
    {
        private string taskStatus;

        public string TaskStatus
        {
            get { return taskStatus; }
            set
            {
                this.taskStatus = value;
                RaisePropertyChanged("TaskStatus");
                this.ToolTipHeader = value;
            }
        }

        public TaskStatusView():base()
        {            
        }
    }

}
