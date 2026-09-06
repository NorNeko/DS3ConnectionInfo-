using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("DS3ConnectionInfo")]
[assembly: AssemblyDescription("Single-file launcher for DS3ConnectionInfo")]
[assembly: AssemblyCompany("DS3ConnectionInfo community fork")]
[assembly: AssemblyProduct("DS3ConnectionInfo")]
[assembly: AssemblyVersion("4.5.0.1")]
[assembly: AssemblyFileVersion("4.5.0.1")]

namespace DS3ConnectionInfo.SingleFile
{
    internal static class Program
    {
        private const string ReleaseName = "v4.5.0-cn.1";
        private const string InnerExecutable = "DS3ConnectionInfo.App.exe";
        private const string MutexName = "Local\\DS3ConnectionInfo.SingleFile.v4.5.0-cn.1";

        private static readonly KeyValuePair<string, string>[] Payload =
        {
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.App", InnerExecutable),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.Steam", "steam_api64.dll"),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.KernelTrace", @"amd64\KernelTraceControl.dll"),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.Msdia", @"amd64\msdia140.dll"),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.Msvcp", @"amd64\msvcp140.dll"),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.Vcruntime1", @"amd64\vcruntime140_1.dll"),
            new KeyValuePair<string, string>("DS3ConnectionInfo.Payload.Vcruntime", @"amd64\vcruntime140.dll")
        };

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                string extractionRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DS3ConnectionInfo",
                    ReleaseName);

                using (var mutex = new Mutex(false, MutexName))
                {
                    if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
                        throw new TimeoutException("Timed out while waiting for another launcher instance.");

                    try
                    {
                        ExtractPayload(extractionRoot);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }

                if (args.Length == 1 && string.Equals(args[0], "--extract-only", StringComparison.Ordinal))
                    return 0;

                var startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(extractionRoot, InnerExecutable),
                    Arguments = JoinArguments(args),
                    WorkingDirectory = extractionRoot,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                return 0;
            }
            catch (Exception exception)
            {
                if (args.Length == 1 && string.Equals(args[0], "--extract-only", StringComparison.Ordinal))
                {
                    File.WriteAllText(
                        Path.Combine(Environment.CurrentDirectory, "single-file-smoke-error.txt"),
                        exception.ToString());
                    return 1;
                }

                MessageBox.Show(
                    "无法启动 DS3ConnectionInfo。\n\nFailed to start DS3ConnectionInfo.\n\n" + exception.Message,
                    "DS3ConnectionInfo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void ExtractPayload(string extractionRoot)
        {
            Directory.CreateDirectory(extractionRoot);
            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (KeyValuePair<string, string> item in Payload)
            {
                string destination = Path.Combine(extractionRoot, item.Value);
                string directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                using (Stream resource = assembly.GetManifestResourceStream(item.Key))
                {
                    if (resource == null)
                        throw new InvalidOperationException("Missing embedded resource: " + item.Key);

                    if (File.Exists(destination) && FilesMatch(resource, destination))
                        continue;

                    resource.Position = 0;
                    string temporary = destination + ".new";
                    using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                        resource.CopyTo(output);

                    if (File.Exists(destination))
                        File.Delete(destination);
                    File.Move(temporary, destination);
                }
            }
        }

        private static bool FilesMatch(Stream resource, string destination)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] resourceHash = sha256.ComputeHash(resource);
                using (var existing = new FileStream(destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] existingHash = sha256.ComputeHash(existing);
                    if (resourceHash.Length != existingHash.Length)
                        return false;
                    for (int index = 0; index < resourceHash.Length; index++)
                    {
                        if (resourceHash[index] != existingHash[index])
                            return false;
                    }
                    return true;
                }
            }
        }

        private static string JoinArguments(string[] args)
        {
            var result = new StringBuilder();
            foreach (string argument in args)
            {
                if (result.Length > 0)
                    result.Append(' ');
                result.Append(QuoteArgument(argument));
            }
            return result.ToString();
        }

        private static string QuoteArgument(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
                return argument;

            var result = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char character in argument)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }

                if (character == '"')
                    result.Append('\\', backslashes * 2 + 1);
                else
                    result.Append('\\', backslashes);

                backslashes = 0;
                result.Append(character);
            }

            result.Append('\\', backslashes * 2);
            result.Append('"');
            return result.ToString();
        }
    }
}
