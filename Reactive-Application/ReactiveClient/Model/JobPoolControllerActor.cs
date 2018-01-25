using Akka.Actor;
using Akka.Routing;
using API;
using API.Messages;
using ReactiveClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveClient
{
    public class JobPoolControllerActor : ReceiveActor
    {
        private class ProcessUnfinishedJobs { }
        Dictionary<int, ProcessJobMessage> _jobsToProcessed = new Dictionary<int, ProcessJobMessage>();
       
        /// <summary>
        /// scheduler instance for processing unstashed objects
        /// </summary>
        private ICancelable _jobScheduler;

        /// <summary>
        /// api actor instance
        /// </summary>
        private IActorRef _api;      

        /// <summary>
        /// timeout of task in minutes
        /// </summary>
        int _taskTimeout = 5;

        /// <summary>
        /// schedule intial delay for processing unfinished tasks
        /// </summary>
        int _initialDelayInMinutes = 2;

        /// <summary>
        /// schedule interval for processing unfinished tasks
        /// </summary>
        int _intervalInMinutes = 1;

        /// <summary>
        /// Job manager view model instance
        /// </summary>
        JobManagerViewModel jobManagerViewModel;


        public JobPoolControllerActor(IActorRef api, JobManagerViewModel mainVm)
        {
            _api = api;
            this.jobManagerViewModel = mainVm;

            ExtractSchedularSettings();

            Receive<ScheduleJobMessage>(msg => HandleScheduleAllJobs());
            Receive<ProcessUnfinishedJobs>(msg => HandleProcessUnFinishedJobs());

            Receive<UnableToAcceptJobMessage>(job =>
            {
                if (!_jobsToProcessed.ContainsKey(job.ID))
                {
                    TaskItem taskItem = this.jobManagerViewModel.Tasks.Where(x => x.TaskID == job.ID).FirstOrDefault();
                    if (taskItem.Status == JobStatus.NotStarted.ToString())
                    {
                        _jobsToProcessed.Add(job.ID, new ProcessJobMessage(job.Description, job.ID, Self));
                    }
                }
            });

            Receive<JobStartedMessage>(job =>
            {
                TaskItem taskItem = this.jobManagerViewModel.Tasks.Where(x => x.TaskID == job.ID).FirstOrDefault();
                taskItem.Node = Sender.Path.ToString().Split('@')[1].Split('/')[0];
                taskItem.StartTime = job.ProcessedTime;
                taskItem.Status = JobStatus.Started.ToString();
                taskItem.NoOfAttempts++;
            });

            Receive<JobCompletedMessage>(job =>
            {
                TaskItem taskItem = this.jobManagerViewModel.Tasks.Where(x => x.TaskID == job.ID).FirstOrDefault();
                taskItem.EndTime = job.ProcessedTime;
                taskItem.Duration = TimeSpan.FromMilliseconds(job.Duration).ToString(@"hh\:mm\:ss");
                taskItem.Status = JobStatus.Completed.ToString();
            });

            Receive<JobFailedMessage>(job =>
            {
                TaskItem taskItem = this.jobManagerViewModel.Tasks.Where(x => x.TaskID == job.ID).FirstOrDefault();
                taskItem.Status = job.Status.ToString();
            });
        }        

        private void HandleScheduleAllJobs()
        {
            _jobsToProcessed.Clear();
            foreach (var taskItem in this.jobManagerViewModel.Tasks)
            {
                var job = new ProcessJobMessage(taskItem.Description, taskItem.TaskID, Self);
                _jobsToProcessed.Add(taskItem.TaskID, job);                
            }

            // schedule jobs
            Context.System.Scheduler.Advanced.ScheduleRepeatedly(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), () =>
            {
                if (_jobsToProcessed.Count > 0)
                {
                    if (_api.Ask<Routees>(new GetRoutees()).Result.Members.Any())
                    {
                        var currentJobMsg = _jobsToProcessed.Values.ElementAt(0);
                        _api.Tell(currentJobMsg);
                        _jobsToProcessed.Remove(currentJobMsg.ID);
                    }
                }
            });

            // create scheduler for uprocessed jobs
            _jobScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                             TimeSpan.FromMinutes(_initialDelayInMinutes), //initial delay in minutes
                             TimeSpan.FromMinutes(_intervalInMinutes),// interval
                             Self,
                             new ProcessUnfinishedJobs(),
                             Self);
        }

      
        private void HandleProcessUnFinishedJobs()
        {
            if (_jobsToProcessed.Count() == 0)
            {
                var count = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Completed.ToString()).Count();
                if (count == this.jobManagerViewModel.Tasks.Count())
                {
                    _jobScheduler.Cancel();
                    this.jobManagerViewModel.StopTimer();
                    this.jobManagerViewModel.IsCompleted = true;
                }
                else
                {
                    List<TaskItem> failedtasks = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Cancelled.ToString()
                         || x.Status == JobStatus.Timeout.ToString() || x.Status == JobStatus.NotStarted.ToString()
                         || (x.Status == JobStatus.Started.ToString() && DateTime.Now.Subtract(x.StartTime).TotalMinutes > _taskTimeout)
                         ).ToList();

                    if (failedtasks.Count() > 0)
                    {
                        foreach (var task in failedtasks)
                        {
                            if (!_jobsToProcessed.ContainsKey(task.TaskID))
                                _jobsToProcessed.Add(task.TaskID, new ProcessJobMessage(task.Description, task.TaskID, Self));
                        }
                    }
                    else
                    {
                        List<TaskItem> unFinishedtasks = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Started.ToString()
                                && DateTime.Now.Subtract(x.StartTime).TotalMinutes < _taskTimeout).ToList();
                        if (unFinishedtasks.Count() == 0)
                        {
                            _jobScheduler.Cancel();
                            this.jobManagerViewModel.StopTimer();
                            this.jobManagerViewModel.IsCompleted = true;
                        }
                    }
                }
            }
        }

        private void ExtractSchedularSettings()
        {            
            int returnValue = GetSettingValue(ConfigurationManager.AppSettings["IntervalInMinutes"]);
            if (returnValue > 0)
            {
                _intervalInMinutes = returnValue;
            }

            returnValue = GetSettingValue(ConfigurationManager.AppSettings["TimeOutInMinutes"]);
            if (returnValue > 0)
            {
                _taskTimeout  = returnValue;
            }

            returnValue = GetSettingValue(ConfigurationManager.AppSettings["InitialDelayInMinutes"]);
            if (returnValue > 0)
            {
                _initialDelayInMinutes = returnValue;
            }
        }

        private int GetSettingValue(string intervalStr)
        {
            int returnValue = 0;
            if (!string.IsNullOrEmpty(intervalStr))
            {
                int outValue = 0;
                if (int.TryParse(intervalStr, out outValue))
                {
                    returnValue = outValue;
                }
            }
            return returnValue;
        }

        #region Lifecycle Event Hooks
        protected override void PreStart()
        {          
            base.PreStart();
        }

        protected override void PostStop()
        {
            if(null != _jobScheduler)
                _jobScheduler.Cancel();
        }

        #endregion
    }
}
