using Akka.Actor;
using System;
using API.Exceptions;
using API.ExternalSystems;
using API.Messages;

namespace API.Actors
{
    internal class WorkerActor: ReceiveActor
    {
        #region private members
      
        /// <summary>
        /// executer instance
        /// </summary>
        private readonly ITaskExecuter _taskExecuter;
       
        /// <summary>
        /// instance of BeginJobMessage
        /// </summary>
        private JobStartedMessage _myJob;

        /// <summary>
        /// scheduler instance for checking timer
        /// </summary>
        private ICancelable _taskTimer;

        private class TaskTimer { };
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerActor"/>  class
        /// </summary>
        /// <param name="executer">Executer object</param>
        public WorkerActor(ITaskExecuter executer)
        {            
            _taskExecuter = executer;

            Receive<JobStartedMessage>(job => HandleJobExecute(job));
            Receive<AcknowledgementMessage>(message => HandleAcknowldgement(message));
            Receive<TaskTimer>(x => CheckTaskTimer());
        }

        private void CheckTaskTimer()
        {
            if (null != _myJob)
            {
                if (DateTime.Now.Subtract(_myJob.ProcessedTime).TotalMinutes > _myJob.TimeOut)
                {
                    // throw new JobTimeOutException();
                    Context.Parent.Tell(new JobFailedMessage(_myJob.Description, _myJob.ID, _myJob.TimeOut, JobStatus.Timeout));
                }
            }
        }

        #region Handle Receive Messages
        /// <summary>
        /// Perform the exceution of job, Handles "BeginJobMessage" message
        /// </summary>
        /// <param name="job"></param>
        private void HandleJobExecute(JobStartedMessage job)
        {            
            _myJob = new JobStartedMessage(job.Description,job.ID, job.TimeOut);
            ColorConsole.WriteLineCyan($"Task {job.ID}. { job.Description} is started with timeout {job.TimeOut.ToString()} minute.");
            // use pipeto to handle async call from caller ( taskexecuter )
            _taskExecuter.ExecuteTask(job).PipeTo(Self, Sender);

            _taskTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                         TimeSpan.FromMinutes(1), //initial delay in minutes
                         TimeSpan.FromSeconds(10),// interval
                         Self,
                         new TaskTimer(),
                         Self);
        }

        /// <summary>
        /// Perform acknowledgement message from the sender.
        /// </summary>
        /// <param name="message"></param>
        private void HandleAcknowldgement(AcknowledgementMessage message)
        {
            if (null != _taskTimer)
                _taskTimer.Cancel();

            if (message.Receipt == AcknowledgementReceipt.CANCELED)
            {
                 throw new JobCanceledException();
            }
            else if (message.Receipt == AcknowledgementReceipt.INVALID_TASK)
            {
                Context.Parent.Tell(new JobFailedMessage(message.Description, message.ID,0, JobStatus.Failed));
                ColorConsole.WriteLineRed($"Task {message.ID}. {message.Description} is invalid.");
            }
            else if (message.Receipt == AcknowledgementReceipt.FAILED)
            {
                Context.Parent.Tell(new JobFailedMessage(message.Description, message.ID,0,JobStatus.Failed));
                ColorConsole.WriteLineRed($"Task {message.ID}. { message.Description} is failed due to unhandled exeption.");
            }
            else if (message.Receipt == AcknowledgementReceipt.TIMEOUT)
            {
                Context.Parent.Tell(new JobFailedMessage(message.Description, message.ID, 0,JobStatus.Timeout));
                ColorConsole.WriteLineRed("Task ID: {0} is cancelled due to time out error.", message.ID);
            }
            else
            {
                if (message.CompletionTime == 0)
                {
                    //  ColorConsole.WriteLineCyan($"Task ID: {message.ID} failed to execute by external application.");
                    //  Context.Parent.Tell(new JobFailedMessage(message.Description, message.ID, JobStatus.Cancelled));  
                    throw new JobCanceledException();
                }
                else
                {
                    ColorConsole.WriteLineCyan("Task ID: {0} completed successfully by worker.", message.ID);
                    Context.Parent.Tell(new JobCompletedMessage(message.Description, message.ID, message.CompletionTime));
                }
            }
        }
        #endregion

        #region Lifecycle hooks

        protected override void PreStart()
        {
            //_taskTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
            //              TimeSpan.FromMinutes(1), //initial delay in minutes
            //              TimeSpan.FromSeconds(10),// interval
            //              Self,
            //              new TaskTimer(),
            //              Self);
        }

        protected override void PostStop()
        {
            if (null != _taskTimer)
                _taskTimer.Cancel();

            ColorConsole.WriteLineRed("WorkerActor for Coordinator {0} stopped.", Context.Parent.Path.Name);           
        }

        protected override void PreRestart(Exception reason, object message)
        {
            ColorConsole.WriteLineWhite("WorkerActor for Coordinator {0} called PreReStart because: {1}", Context.Parent.Path.Name, reason.Message);          
            Self.Tell(_myJob);
            base.PreRestart(reason, message);
        }

        protected override void PostRestart(Exception reason)
        {
            ColorConsole.WriteLineWhite("WorkerActor for Coordinator {0} called PostRestart because: {1}", Context.Parent.Path.Name, reason.Message);
            base.PostRestart(reason);
        }
        #endregion
    }
}
