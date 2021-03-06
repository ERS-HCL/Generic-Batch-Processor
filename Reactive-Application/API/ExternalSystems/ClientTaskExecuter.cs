﻿using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using API.Messages;

namespace API.ExternalSystems
{
    public class ClientTaskExecuter : ITaskExecuter
    {
        public async Task<AcknowledgementMessage> ExecuteTask(JobStartedMessage taskMessage)
        {
            string outputPath = ConfigurationManager.AppSettings["ClientOutputFolderPath"];
            string exePath = ConfigurationManager.AppSettings["ClientExecutablePath"];
            string title = string.Format($"Clipping Executer : Task {taskMessage.ID}");

            outputPath = ConvertToURIPath(outputPath);
            exePath = ConvertToURIPath(exePath);

            string compartmentExtractor = outputPath + @"/" + "CompartmentExtractor.txt";
            string childExtractor = outputPath + @"/" + taskMessage.Description;

            return await Task.Delay(100)
               .ContinueWith<AcknowledgementMessage>(task =>
               {
                   // execute task here
                   long taskTime = 0;
                   int exitCode = 0;
                   AcknowledgementReceipt receipt = AcknowledgementReceipt.SUCCESS;

                   if (string.IsNullOrEmpty(childExtractor) || string.IsNullOrEmpty(compartmentExtractor)
                   || string.IsNullOrEmpty(outputPath) || string.IsNullOrEmpty(exePath))
                   {
                       receipt = AcknowledgementReceipt.INVALID_TASK;
                   }
                   else
                   {
                       Task clipperTask = Task.Factory.StartNew(() =>
                       {
                           ExecuteCompartmentClipper(compartmentExtractor, childExtractor,
                               outputPath, exePath, title, ref taskTime, ref exitCode);
                       });

                       // wait for task to complete
                       clipperTask.Wait();

                       Console.WriteLine(string.Format("Clpping process for task {0} exited with exit code {1}:",
                           taskMessage.ID.ToString(), exitCode.ToString()));

                       switch (clipperTask.Status)
                       {
                           case TaskStatus.Faulted:
                               receipt = AcknowledgementReceipt.FAILED;
                               break;
                           case TaskStatus.Canceled:
                               receipt = AcknowledgementReceipt.CANCELED;
                               break;
                           case TaskStatus.RanToCompletion:
                               {
                                   if (exitCode == 0 || exitCode == -529697949)
                                       receipt = AcknowledgementReceipt.SUCCESS;
                                   else if(exitCode == -1073741510 || exitCode == 254 || taskTime == 0)
                                       receipt = AcknowledgementReceipt.CANCELED;
                                   else
                                       receipt = AcknowledgementReceipt.FAILED;
                                   break;
                               }                              
                       }
                   }

                   // send the acknowledgement
                   return new AcknowledgementMessage(taskMessage.ID, taskMessage.Description, taskTime, receipt);
               });
        }

        private string ConvertToURIPath(string filePath)
        {
            var uriOutPath = new Uri(filePath);
            string uriStr = uriOutPath.ToString();
            if (uriStr.StartsWith("file:///"))
                uriStr = uriStr.Replace("file:///", string.Empty);
            else if (uriStr.StartsWith("file://"))
                uriStr = uriStr.Replace("file:", string.Empty);

            uriStr = uriStr.Replace("%20", " ");
            return uriStr;
        }

        #region Executer Program
        private void ExecuteCompartmentClipper(string compExtractorPath, string childExtractorPath,
            string outputPath, string exePath, string title, ref long taskTime, ref int exitCode)
        {
            Stopwatch watch = new Stopwatch();
            taskTime = 0;
            watch.Start();

            using (Process Executer = new Process())
            {
                Executer.StartInfo.FileName = exePath;
                Executer.StartInfo.Arguments = "\"" + compExtractorPath + "\" " + "\"" + childExtractorPath + "\" " + "\"" + outputPath + "\" " + "\" " + title + "\" ";
                Executer.StartInfo.UseShellExecute = true;
                Executer.StartInfo.Verb = "runas";
                Executer.StartInfo.RedirectStandardOutput = false;
                Executer.OutputDataReceived += Executer_OutputDataReceived;
                try
                {
                    bool isStarted = Executer.Start();
                    Executer.WaitForExit();
                    exitCode = Executer.ExitCode;
                }
                catch (Win32Exception)
                {
                    exitCode = Executer.ExitCode;
                    throw;
                }
            }

            taskTime = watch.ElapsedMilliseconds;
            watch.Stop();
        }

        private static void Executer_OutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
        {
            String output = dataReceivedEventArgs.Data;
            Console.WriteLine(output);
        }

        #endregion
    }
}
