using Akka.Actor;
using Akka.Routing;
using API;
using API.Messages;
using ReactiveClient.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// schedule intial delay for processing unfinished tasks
        /// </summary>
        int _initialDelayInMinutes = 2;

        /// <summary>
        /// schedule interval for processing unfinished tasks
        /// </summary>
        int _intervalInSeconds = 60;

        /// <summary>
        /// max no. of attempts for the task
        /// </summary>
        int _maxNoOfAttempts = 3;

        /// <summary>
        /// Job manager view model instance
        /// </summary>
        JobManagerViewModel jobManagerViewModel;


        public JobPoolControllerActor(IActorRef api, JobManagerViewModel mainVm)
        {
            _api = api;
            this.jobManagerViewModel = mainVm;

            this._initialDelayInMinutes = Utility.GetSettingValue("InitialDelayInMinutes");
            this._intervalInSeconds =  Utility.GetSettingValue("IntervalInSeconds");
            this._maxNoOfAttempts = Utility.GetSettingValue("MaxNoOfAttempts");
            
            Receive<ScheduleJobMessage>(msg => HandleScheduleAllJobs());
            Receive<ProcessUnfinishedJobs>(msg => HandleProcessUnFinishedJobs());

            Receive<UnableToAcceptJobMessage>(job =>
            {
                if (!_jobsToProcessed.ContainsKey(job.ID))
                {
                    TaskItem taskItem = this.jobManagerViewModel.Tasks.Where(x => x.TaskID == job.ID).FirstOrDefault();
                    if (taskItem.Status == JobStatus.NotStarted.ToString())
                    {
                        _jobsToProcessed.Add(job.ID, new ProcessJobMessage(job.Description, job.ID, taskItem.TimeOut, Self));
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
                if (_jobsToProcessed.ContainsKey(job.ID))
                    _jobsToProcessed.Remove(taskItem.TaskID);
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
                var job = new ProcessJobMessage(taskItem.Description, taskItem.TaskID, taskItem.TimeOut, Self);
                _jobsToProcessed.Add(taskItem.TaskID, job);                
            }

            // schedule jobs
            Context.System.Scheduler.Advanced.ScheduleRepeatedly(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10), () =>
            {
                if (_jobsToProcessed.Count > 0)
                {
                    if (_api.Ask<Routees>(new GetRoutees()).Result.Members.Any())
                    {
                        var currentJobMsg = _jobsToProcessed.Values.ElementAt(0);
                        _api.Tell(currentJobMsg);
                      //  _jobsToProcessed.Remove(currentJobMsg.ID);
                    }
                }
            });

            // create scheduler for uprocessed jobs
            _jobScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                             TimeSpan.FromMinutes(_initialDelayInMinutes), //initial delay in minutes
                             TimeSpan.FromSeconds(_intervalInSeconds),// interval
                             Self,
                             new ProcessUnfinishedJobs(),
                             Self);
        }


        //private void HandleProcessUnFinishedJobs()
        //{
        // //   if (_jobsToProcessed.Count() == 0)
        //    {
        //        var count = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Completed.ToString()).Count();
        //        if (count == this.jobManagerViewModel.Tasks.Count())
        //        {
        //            _jobScheduler.Cancel();
        //            this.jobManagerViewModel.StopTimer();
        //            this.jobManagerViewModel.IsCompleted = true;
        //        }
        //        else
        //        {
        //            if (_jobsToProcessed.Count() == 0)
        //            {
        //                // not started tasks
        //                List<TaskItem> unProcessedTasks = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.NotStarted.ToString()).ToList();
        //                foreach (var task in unProcessedTasks)
        //                {
        //                    if (!_jobsToProcessed.ContainsKey(task.TaskID))
        //                        _jobsToProcessed.Add(task.TaskID, new ProcessJobMessage(task.Description, task.TaskID, Self));
        //                }
        //            }

        //            // timeout taks
        //            List<TaskItem> timeoutTasks = this.jobManagerViewModel.Tasks.Where(x => (x.Status == JobStatus.Started.ToString()
        //                                         && DateTime.Now.Subtract(x.StartTime).TotalMinutes > x.TimeOut)).ToList();

        //            if (timeoutTasks.Count() > 0)
        //            {
        //                foreach (var task in timeoutTasks)
        //                {
        //                    if(task.NoOfAttempts >= _maxNoOfAttempts)
        //                    {
        //                        task.Status = JobStatus.Timeout.ToString();
        //                        continue;
        //                    }

        //                    if (!_jobsToProcessed.ContainsKey(task.TaskID))
        //                        _jobsToProcessed.Add(task.TaskID, new ProcessJobMessage(task.Description, task.TaskID, Self));
        //                }
        //            }
        //            else
        //            {
        //                List<TaskItem> unFinishedtasks = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Started.ToString()
        //                        && DateTime.Now.Subtract(x.StartTime).TotalMinutes < x.TimeOut).ToList();
        //                if (unFinishedtasks.Count() == 0)
        //                {
        //                    _jobScheduler.Cancel();
        //                    this.jobManagerViewModel.StopTimer();
        //                    this.jobManagerViewModel.IsCompleted = true;
        //                }
        //            }
        //        }
        //    }
        //}

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
                    List<TaskItem> failedTasks = this.jobManagerViewModel.Tasks.Where( x => x.Status == JobStatus.Timeout.ToString()
                     ||(x.Status == JobStatus.Started.ToString() && DateTime.Now.Subtract(x.StartTime).TotalMinutes > x.TimeOut)).ToList();

                    if (failedTasks.Count() > 0)
                    {
                        foreach (var task in failedTasks)
                        {
                            if (task.NoOfAttempts >= _maxNoOfAttempts)
                            {
                                task.Status = JobStatus.Cancelled.ToString();
                                continue;
                            }

                            if (!_jobsToProcessed.ContainsKey(task.TaskID))
                            {
                                if (task.Status == JobStatus.Timeout.ToString() || 
                                   (task.Status == JobStatus.Started.ToString() && task.NoOfAttempts ==1 )) // somtimes even if the task completed successfuly by worker, the acknowledge message is not delivered to api. This condition will handle this problem.
                                {
                                    task.TimeOut += (int)Math.Ceiling(task.TimeOut * 0.5);
                                    _jobsToProcessed.Add(task.TaskID, new ProcessJobMessage(task.Description, task.TaskID, task.TimeOut, Self));
                                }
                            }
                        }
                    }                    
                    else
                    {
                        List<TaskItem> unFinishedtasks = this.jobManagerViewModel.Tasks.Where(x => x.Status == JobStatus.Started.ToString()
                                && DateTime.Now.Subtract(x.StartTime).TotalMinutes < x.TimeOut).ToList();
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
