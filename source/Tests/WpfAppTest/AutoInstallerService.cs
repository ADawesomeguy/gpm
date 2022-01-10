using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace WpfAppTest;

public class AutoInstallerService
{
    public static async Task<bool> CheckForUpdate()
    {




        await Task.Delay(1);
    }

    public static async Task<bool> EnsureGpmInstalled()
    {
        var installed = await InstallGpmAsync();
        if (!installed)
        {
            return await UpdateGpmAsync();
        }
        return true;
    }


    private static async Task<bool> UpdateGpmAsync() => await RunDotnetAsync("update", "gpm");

    private static async Task<bool> InstallGpmAsync() => await RunDotnetAsync("install", "gpm");

    private static async Task<bool> RunDotnetAsync(string verb, string toolName)
    {
        Process? p;
        TaskCompletionSource<bool>? _eventHandled;
        _eventHandled = new TaskCompletionSource<bool>();

        using (p = new Process())
        {
            try
            {
                p.StartInfo.FileName = "dotnet";
                p.StartInfo.Arguments = $"tool {verb} {toolName} -g";

                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                p.EnableRaisingEvents = true;

                p.OutputDataReceived += (s, e) =>
                {
                    Log.Information(e.Data);
                };
                p.ErrorDataReceived += (s, e) =>
                {
                    Log.Error(e.Data);
                };

                //p.Exited += new EventHandler(myProcess_Exited);
                p.Exited += (s, e) =>
                {
                    var exitCode = p.ExitCode;
                    Log.Information(
                        $"Exit time    : {p.ExitTime}\n" +
                        $"Exit code    : {exitCode}\n" +
                        $"Elapsed time : {Math.Round((p.ExitTime - p.StartTime).TotalMilliseconds)}");
                    _eventHandled.TrySetResult(true);
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred trying to install gpm:\n{ex.Message}");
                return false;
            }

            // Wait for Exited event, but not more than 30 seconds.
            await await Task.WhenAny(_eventHandled.Task, Task.Delay(30000));
            return p.ExitCode == 0;
        }
    }
}
