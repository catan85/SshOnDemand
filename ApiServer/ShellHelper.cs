using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiServer
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;

    public static class ShellHelper
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd;
            if (AppSettings.OS == "Linux")
              escapedArgs = cmd.Replace("\"", "\\\"");


            var process = new Process();

            if (AppSettings.OS == "Linux")
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
            }
            else
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {escapedArgs}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
            }

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) => {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(1000) &&
                    outputWaitHandle.WaitOne(1000) &&
                    errorWaitHandle.WaitOne(1000))
                {
                    // Process completed. Check process.ExitCode here.
                }
                else
                {
                    // Timed out.
                }
            }
            return output.ToString();
        }


    }
}
