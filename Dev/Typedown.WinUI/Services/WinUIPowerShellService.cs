using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Typedown.Core.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIPowerShellService : IPowerShellService
    {
        public IEnumerable<string> Invoke(string script, string command, params string[] parameters)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("PowerShell command cannot be empty.", nameof(command));
            }

            var scriptPath = Path.Combine(Path.GetTempPath(), $"Typedown-{Guid.NewGuid():N}.ps1");
            try
            {
                File.WriteAllText(scriptPath, BuildScript(script, command, parameters), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ResolvePowerShellExecutable(),
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }) ?? throw new InvalidOperationException("Failed to start PowerShell.");

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                var output = outputTask.GetAwaiter().GetResult();
                var error = errorTask.GetAwaiter().GetResult();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"PowerShell exited with code {process.ExitCode}: {error}");
                }

                return output
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.TrimEnd('\r'))
                    .ToArray();
            }
            finally
            {
                TryDelete(scriptPath);
            }
        }

        private static string BuildScript(string script, string command, IReadOnlyList<string> parameters)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(script))
            {
                builder.AppendLine(script);
            }

            builder.Append(command);
            foreach (var parameter in parameters)
            {
                builder.Append(' ');
                builder.Append(ToPowerShellLiteral(parameter));
            }

            builder.AppendLine();
            return builder.ToString();
        }

        private static string ToPowerShellLiteral(string? value)
        {
            return $"'{(value ?? string.Empty).Replace("'", "''")}'";
        }

        private static string ResolvePowerShellExecutable()
        {
            var systemPowerShell = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell",
                "v1.0",
                "powershell.exe");

            return File.Exists(systemPowerShell) ? systemPowerShell : "powershell.exe";
        }

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
