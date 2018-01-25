using Akka.Actor;
using Akka.Routing;
using System;
using System.Linq;
using API.Messages;
using API.Actors;
using API.Exceptions;

namespace API
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching task-processing jobs
    /// </summary>
    public class API : ReceiveActor
    {
        #region private members

        /// <summary>
        /// Coordinator instance actor
        /// </summary>
        private IActorRef _coordinator;

        /// <summary>
        /// no. of routees replies
        /// </summary>
        private int _pendingJobReplies;

        /// <summary>
        /// current job to be processed
        /// </summary>
        private ProcessJobMessage _currentJob;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="API"/>  class
        /// </summary>
        public API()
        {
            Ready();
        }

        #region Switchable behaviour for Coordinator
        private void Ready()
        {
            ColorConsole.WriteLineGreen("api's current state is Ready.");

            Receive<ProcessJobMessage>(job =>
            {
                _currentJob = job;
                ColorConsole.WriteLineWhite($"Received Task {job.ID}.{job.Description} for processing.");
                // ask the coordinator for job
                _coordinator.Tell(new CanAcceptJobMessage(job.Description, job.ID, job.TimeOut));

                // move to next state              
                BecomeAsking();
            });

            Receive<JobCompletedMessage>(job =>
            {
                // send response to client
                _currentJob.Client.Tell(job);

                ColorConsole.WriteLineGreen($"Task {job.ID} is completed by api.");
            });

            Receive<JobFailedMessage>(job =>
            {
                // send response to client
                _currentJob.Client.Tell(job);

                ColorConsole.WriteLineRed($"Task {job.ID} is failed.");
            });
          
        }

        private void BecomeAsking()
        {
            // block, but ask the router for the number of routees. Avoids magic numbers.
            _pendingJobReplies = _coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();

            // move to next state
            Become(Asking);

            // send ourselves a ReceiveTimeout message if no message within 3 seonds
            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void Asking()
        {
            // received UnableToAcceptJobMessage from coordinator
            Receive<UnableToAcceptJobMessage>(job =>
            {
                // each routee is giving response to cordinator
                _pendingJobReplies--;
                if (_pendingJobReplies == 0)
                {
                    // send response to client
                    _currentJob.Client.Tell(job);

                    // move to next state
                    BecomeReady();
                }
            });

            // received AbleToAcceptJobMessage from coordinator
            Receive<AbleToAcceptJobMessage>(job =>
            {
                ColorConsole.WriteLineYellow($"Starting Job for Task ID: {job.ID}. {job.Description}");
                // ask coordinator for processing message

                var jobMessage = new JobStartedMessage(job.Description, job.ID, job.TimeOut);
                Sender.Tell(jobMessage, Self);

                // tell the client that job has been started
                _currentJob.Client.Tell(jobMessage);

                BecomeReady();
            });

            // means at least one actor failed to respond
            Receive<ReceiveTimeout>(timeout =>
            {
                // send response to client
                ColorConsole.WriteLineYellow($"Receive timeout from { Sender.Path.Name } for Task ID. {_currentJob.ID}. {_currentJob.Description}");

                BecomeAsking();
            });
        }

        private void BecomeReady()
        {
            // cancel ReceiveTimeout
            Context.SetReceiveTimeout(null);

            // move to next state
            Become(Ready);
        }

        #endregion


        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                exception =>
                {
                    if (exception is CoordinatorStoppedException)
                    {
                        ColorConsole.WriteLineRed("CoordinatorStoppedException caught for Coordinator {0}, Restarting the task...", Self.Path.Name);
                        return Directive.Restart;
                    }
                    else
                    {
                        ColorConsole.WriteLineRed("Unknown exception caugh for api, restarting the task...");
                        return Directive.Restart;
                    }
                });
        }

        #region Lifecycle Event Hooks
        protected override void PreStart()
        {
            // create a broadcast router who will ask all of them if they're available for work
            _coordinator = Context.ActorOf(Props.Create(() => new CoordinatorActor())
                .WithRouter(FromConfig.Instance), "coordinator");

            base.PreStart();
        }

        protected override void PostStop()
        {
            ColorConsole.WriteLineRed("api's PostStop called.");
        }

        protected override void PreRestart(Exception reason, object message)
        {
            ColorConsole.WriteLineWhite("api's PreRestart called because: {0} ", reason.Message);

            //kill off the old coordinator so we can recreate it from scratch
            ColorConsole.WriteLineWhite("kill off the old coordinator so we can recreate it from scratch");
            _coordinator.Tell(PoisonPill.Instance);

            base.PreRestart(reason, message);
        }

        #endregion
    }

}