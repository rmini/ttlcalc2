using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace TtlCalcSim.Testing;

/// <summary>
/// Thrown when customasm cannot be run or rejects the source.
/// </summary>
public class AssemblerException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Runs customasm to produce program words and symbols. Results are cached for the lifetime
/// of the process, keyed by file path and modification time, or by source text.
/// </summary>
internal static partial class Assembler
{
    private const string ExecutableEnvVar = "CUSTOMASM";

    private static readonly ConcurrentDictionary<string, Lazy<AsmProgram>> Cache = new();

    private static string Executable => Environment.GetEnvironmentVariable(ExecutableEnvVar) ?? "customasm";

    public static AsmProgram AssembleFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Assembly source not found: {fullPath}", fullPath);

        var key = $"file:{fullPath}:{File.GetLastWriteTimeUtc(fullPath).Ticks}:{File.GetLastWriteTimeUtc(RepoPaths.Rules).Ticks}";
        return Cache.GetOrAdd(key, _ => new Lazy<AsmProgram>(() => Run(fullPath, Path.GetFileName(fullPath)))).Value;
    }

    public static AsmProgram AssembleSource(string source)
    {
        var key = $"source:{File.GetLastWriteTimeUtc(RepoPaths.Rules).Ticks}:{source}";
        return Cache.GetOrAdd(key, _ => new Lazy<AsmProgram>(() =>
        {
            var dir = CreateTempDir();
            try
            {
                // customasm rejects absolute include paths, so include a copy of the rules
                var rules = Path.GetFileName(RepoPaths.Rules);
                File.Copy(RepoPaths.Rules, Path.Combine(dir, rules));
                var path = Path.Combine(dir, "source.asm");
                File.WriteAllText(path, $"#include \"{rules}\"\n#bank program\n{source}\n");
                return Run(path, "<source>");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        })).Value;
    }

    private static AsmProgram Run(string sourcePath, string name)
    {
        var dir = CreateTempDir();
        try
        {
            var binPath = Path.Combine(dir, "out.bin");
            var symPath = Path.Combine(dir, "out.sym");

            var psi = new ProcessStartInfo(Executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(sourcePath)!,
            };
            foreach (var arg in new[] { sourcePath, "-q", "-f", "binary", "-o", binPath, "--", "-f", "symbols", "-o", symPath })
                psi.ArgumentList.Add(arg);

            Process process;
            try
            {
                process = Process.Start(psi) ?? throw new AssemblerException($"Failed to start {Executable}");
            }
            catch (Win32Exception e)
            {
                throw new AssemblerException(
                    $"Could not run '{Executable}'. Install customasm v0.14.0 or later on PATH, " +
                    $"or set the {ExecutableEnvVar} environment variable to its location.", e);
            }

            using (process)
            {
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEnd();
                stdoutTask.Wait();
                process.WaitForExit();

                if (process.ExitCode != 0 || !File.Exists(binPath))
                {
                    var output = AnsiEscape().Replace(stderr + stdoutTask.Result, "").Trim();
                    throw new AssemblerException($"customasm failed to assemble {name}:\n{output}");
                }
            }

            return new AsmProgram(name, ReadWords(binPath), ReadSymbols(symPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static ushort[] ReadWords(string binPath)
    {
        var bytes = File.ReadAllBytes(binPath);
        var words = new ushort[bytes.Length / 2];
        for (var i = 0; i < words.Length; i++)
            words[i] = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i * 2));
        return words;
    }

    private static Dictionary<string, int> ReadSymbols(string symPath)
    {
        var symbols = new Dictionary<string, int>();
        if (!File.Exists(symPath))
            return symbols;

        foreach (var line in File.ReadLines(symPath))
        {
            var match = SymbolLine().Match(line);
            if (match.Success)
                symbols[match.Groups[1].Value] = int.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
        }

        return symbols;
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ttlcalcsim-asm", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiEscape();

    [GeneratedRegex(@"^\s*(\S+)\s*=\s*0x([0-9a-fA-F]+)\s*$")]
    private static partial Regex SymbolLine();
}
