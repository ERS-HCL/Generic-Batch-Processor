using System;
using Akka.Actor;

namespace API.Messages
{
    public abstract class JobMessage //: IConsistentHashable
    {
        public string Description { get; private set; }
        public int ID { get; private set; }
        public int TimeOut { get; private set; }
      //  public object ConsistentHashKey { get { return ID; } }

        protected JobMessage(string taskFileName, int taskId, int timeout = 0)
        {
            Description = taskFileName;
            ID = taskId;
            TimeOut = timeout;
        }
    }

    public class CanAcceptJobMessage : JobMessage
    {
        public CanAcceptJobMessage(string taskFileName, int taskId, int timeout)
            : base(taskFileName, taskId, timeout) { }
    }

    public class AbleToAcceptJobMessage : JobMessage
    {
        public AbleToAcceptJobMessage(string taskFileName, int taskId, int timeout)
            : base(taskFileName, taskId, timeout) { }
    }

    public class UnableToAcceptJobMessage : JobMessage
    {
        public UnableToAcceptJobMessage(string taskFileName, int taskId, int timeout)
            : base(taskFileName, taskId, timeout) { }
    }

    public class JobStartedMessage : JobMessage
    {
        public DateTime ProcessedTime { get; private set; }
        public JobStartedMessage(string taskFileName, int taskId, int timeout)
            : base(taskFileName, taskId, timeout)
        {
            ProcessedTime = DateTime.Now;
        }
    }

    public class JobFailedMessage : JobMessage
    {
        public JobStatus Status { get; private set; }
        public JobFailedMessage(string taskFileName, int taskId,int timeout, JobStatus reason)
            : base(taskFileName, taskId, timeout)
        {
            Status = reason;
        }
    }

    public class JobCompletedMessage : JobMessage
    {
        public long Duration { get; private set; }
        public DateTime ProcessedTime { get; private set; }
        public JobCompletedMessage(string taskFileName, int taskId, long duration)
            : base(taskFileName, taskId)
        {
            Duration = duration;
            ProcessedTime = DateTime.Now;
        }
    }


    public class ProcessJobMessage : JobMessage
    {
        public IActorRef Client { get; set; }
        public ProcessJobMessage(string taskFileName, int taskId, int timeout, IActorRef client=null)
            : base(taskFileName, taskId, timeout) {
            Client = client;
        }
    }
    
    //public class ProcessStashedJobsMessage : JobMessage
    //{
    //    public ProcessStashedJobsMessage(string taskFileName, int taskId, int timeout)
    //        : base(taskFileName, taskId, timeout) { }
    //}

    //public class WorkerKilledMessage : JobMessage
    //{
    //     public WorkerKilledMessage(string taskFileName, int taskId, int timeout)
    //        : base(taskFileName, taskId, timeout) { }
    //}

    public class ProcessFileMessage
    {
        public string FileName { get; private set; }

        public ProcessFileMessage(string fileName)
        {
            FileName = fileName;
        }
    }

    public class JobValidationSucceedMessage { };
    public class JobValidationFailedMessage { };
}
