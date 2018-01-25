using API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ReactiveClient
{
    public class TaskItem : BaseViewModel
    {
        int _taskId;
        string _description;
        string _node;
        string _status;
        DateTime _startTime;
        DateTime _endTime;
        string _duration;
        int _noOfAttempt;
        int _timeOut;

        public int TaskID
        {
            get { return _taskId; }
            set
            {
                _taskId = value;
                this.RaisePropertyChanged("TaskID");
            }
        }
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                this.RaisePropertyChanged("Description");
            }
        }
        public string Node
        {
            get { return _node; }
            set
            {
                _node = value;
                this.RaisePropertyChanged("Node");
            }
        }
        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                this.RaisePropertyChanged("Status");
                this.RaisePropertyChanged("StatusToBrush");
            }
        }
        public DateTime StartTime
        {
            get { return _startTime; }
            set
            {
                _startTime = value;
                this.RaisePropertyChanged("StartTime");
            }
        }
        public DateTime EndTime
        {
            get { return _endTime; }
            set
            {
                _endTime = value;
                this.RaisePropertyChanged("EndTime");
            }
        }
        public string Duration
        {
            get { return _duration; }
            set
            {
                _duration = value;
                this.RaisePropertyChanged("Duration");
            }
        }

        public int NoOfAttempts
        {
            get { return _noOfAttempt; }
            set
            {
                _noOfAttempt = value;
                this.RaisePropertyChanged("NoOfAttempts");
            }

        }

        public int TimeOut
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }
        public TaskItem(int taskId, string taskDescription, int timeOut)
        {
            TaskID = taskId;
            Description = taskDescription;
            TimeOut = timeOut;
            Status = JobStatus.NotStarted.ToString();
        }

        public Brush StatusToBrush
        {
            get
            {
                switch (Status)
                {
                    case "NotStarted":
                        return Brushes.LightBlue;
                    case "Started":
                        return Brushes.LightSalmon;
                    case "Completed":
                        return Brushes.LightGreen;
                    default:
                        break;
                }

                return Brushes.Transparent;
            }
        }
    }
}
