using Microsoft.Build.Framework;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Bit.Tooling.CodeGenerator.Task
{
    public class BitSourceGenerator : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            Stopwatch? sw = null;

            try
            {
                LogMessage($"Code generation started for project: {ProjectPath}");

                sw = Stopwatch.StartNew();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    using (Process bitCodeGeneratorImplProcess = new Process())
                    {
                        bitCodeGeneratorImplProcess.StartInfo.UseShellExecute = false;
                        bitCodeGeneratorImplProcess.StartInfo.RedirectStandardOutput = bitCodeGeneratorImplProcess.StartInfo.RedirectStandardError = true;
                        bitCodeGeneratorImplProcess.StartInfo.FileName = Path.Combine(Path.GetDirectoryName(typeof(BitSourceGenerator).Assembly.Location), "..", @"implementation\Bit.Tooling.CodeGenerator.exe"); // Not supported on Mac/Linux at the moment.
                        bitCodeGeneratorImplProcess.StartInfo.Arguments = $"-projectPath \"{ProjectPath}\"";
                        bitCodeGeneratorImplProcess.StartInfo.CreateNoWindow = true;
                        bitCodeGeneratorImplProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(bitCodeGeneratorImplProcess.StartInfo.FileName);
                        bitCodeGeneratorImplProcess.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                                LogMessage(e.Data);
                            else
                                outputWaitHandle.Set();
                        };
                        bitCodeGeneratorImplProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                                LogError(e.Data, new Exception(e.Data));
                            else
                                errorWaitHandle.Set();
                        };
                        bitCodeGeneratorImplProcess.Start();
                        bitCodeGeneratorImplProcess.BeginOutputReadLine();
                        bitCodeGeneratorImplProcess.BeginErrorReadLine();
                        bitCodeGeneratorImplProcess.WaitForExit();
                        outputWaitHandle.WaitOne();
                        errorWaitHandle.WaitOne();
                    }
                }

                LogMessage($"Code Generation completed in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception exp)
            {
                LogError(exp.Message, exp);
            }
            finally
            {
                sw?.Stop();
            }

            return true;
        }

        void LogMessage(string text)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName} <<<<< \n";

            Log.LogMessage(MessageImportance.High, text);
        }

        void LogError(string text, Exception ex)
        {
            text = $">>>>> {text} {DateTimeOffset.Now} {typeof(BitSourceGenerator).Assembly.FullName}<<<<< \n {ex} \n";

            Log.LogError(text);
        }

        [Required]
        public virtual string ProjectPath { get; set; } = default!;
    }
}
