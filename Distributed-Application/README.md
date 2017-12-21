## Fault tolerant and resilient distributed application for batch processing using [Akka.NET](http://getakka.net/ "Akka.NET - .NET distributed actor framework"). 
The goal of this sample is to show you how to use Akka.Cluster to form resilient systems that can scale out across multiple processes or machines without complicated user-defined code or expensive tools.

## Workflow

![Image of Workflow](/Distributed-Application/Workflow.PNG)

## Sample Overview

In this sample we're actually going to run three different pieces of software concurrently:
* **`[Lighthouse]`** - An instance of the **[Lighthouse](https://github.com/petabridge/lighthouse "Lighthouse - Service Discovery for Akka.NET")** service, so you'll want to clone and build that repository in you intend to run this sample;
* **`[Client]`** - A dedicated console application built using Akka.Cluster. This is a single instance application whose job is to assign the jobs to API and get the updates from API. This application plays a Job Manager role. 
* **`[API]`** - A dedicated console application built using Akka.Cluster. This is where all of the scalable processing work is done in this sample, and multiple instances of these application can be run in parallel in order to cooperatively execute a job assigned by Job Manager (Client application).  


## Distributed Workflow

![Image of Distributed System Workflow](/Distributed-Application/Distribute_Flow_Diagram.PNG)


#### `[Lighthouse]` Role
[Lighthouse is a piece of free open-source software for Akka.Cluster service discovery](https://github.com/petabridge/lighthouse "Lighthouse - Service Discovery for Akka.NET"), developed by Petabridge.

It has two jobs:

1. Act as the dedicated seed node for all `[Client]` and `[API]` roles when they attempt to join the Akka.Cluster and
2. Broadcast the availability of new nodes to all `[Client]` and `[API]` instances so they can leverage the newly available nodes for work.

There can be multiple `[Lighthouse]` roles running in parallel, but all of their addresses need to be written into the `akka.cluster` HOCON configuration section of each `[Client]` and `[API]` node in order to use Lighthouse's capabilities effectively.

#### `[Client]` Role
The `[Client]` role corresponds to everything inside the `[Client project]` that uses a lightweight `ActorSystem` to communicate with all `[API]` roles. It's meant to act as the [Job Manager] part of the Application.

This application creates the job pool, schedules them and assign to any available API. This application keeps the status of each task and displays the summary after completion of all jobs. This application also managed the failed tasks and decide the strategy whether they will need to restart or not.

There can be only one instance of the `[Client]` role.

#### `[API]` Role
The `[API]` role corresponds to everything inside the `[API project]` that uses a lightweight `ActorSystem` to communicate with `[Client]` roles. It's meant to act as the `[Executer]` part of the Application.

This application processed the job assigned by Job Manager `[Client]`. Once the job received, it will call the external application to execute the job. Once the job completes, this application sends the acknowledgement to Client.

There can be multiple instances of the `[API]` role.


## Running the Sample

1. Clone this repository to your local computer - we highly recommend installing [GitHub for Windows](https://windows.github.com/ "GitHub for Windows") if you don't already have a Git client installed.
2. Clone [Lighthouse](https://github.com/petabridge/lighthouse) to your local computer.
3. Open the `Lighthouse.sln` and change this line in App.config  `lighthouse {
  actorsystem: "batchProcessor" #change from "lighthouse" to "batchProcessor"
}`
and update the actorsystem address in 
cluster '{
	#will inject this node as a self-seed node at run-time
	seed-nodes = ["akka.tcp://batchProcessor@127.0.0.1:4053"]
	roles = [lighthouse]
}'

4. Press `F6` to start Lighthouse.
4. Open `BatchProcessor.sln` in Visual Studio 2015 or later.
5. Open ClientTaskExceuter.cs file and modify ExecuteTask() method for your application.
6. Provide the job details in JobPool.txt file
7. Press `F6` to build the sample - this solution has [Nuget package restore](http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages) enabled, so any third party dependencies will automatically be downloaded and added as references.
8. Press `F5` to run the sample.

And then give it a try!

## Contributing

Please feel free to contribute.

## Questions about Samples?

If you have any questions about this sample, please [Create a GitHub issue for us](https://github.com/ERS-HCL/Generic-Batch-Processor/issues)!

## Code License
MIT