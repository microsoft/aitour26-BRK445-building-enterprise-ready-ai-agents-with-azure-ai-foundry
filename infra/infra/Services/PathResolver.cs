namespace Infra.AgentDeployment;

internal static class PathResolver
{
 public static string ResolveSourceFilePath(string file)
 {
 if (string.IsNullOrWhiteSpace(file)) return file ?? string.Empty;
 var normalized = file.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
 if (Path.IsPathRooted(normalized)) return normalized;
 var cwdCandidate = Path.GetFullPath(normalized, Directory.GetCurrentDirectory());
 if (File.Exists(cwdCandidate)) return cwdCandidate;
 var baseDirCandidate = Path.GetFullPath(normalized, AppContext.BaseDirectory);
 if (File.Exists(baseDirCandidate)) return baseDirCandidate;
 var probe = new DirectoryInfo(AppContext.BaseDirectory);
 for (int i =0; i <6 && probe != null; i++)
 {
 var candidate = Path.Combine(probe.FullName, normalized);
 if (File.Exists(candidate)) return candidate;
 probe = probe.Parent;
 }
 var parts = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
 if (parts.Length >1)
 {
 var withoutFirst = string.Join(Path.DirectorySeparatorChar, parts.Skip(1));
 probe = new DirectoryInfo(AppContext.BaseDirectory);
 for (int i =0; i <6 && probe != null; i++)
 {
 var candidate = Path.Combine(probe.FullName, withoutFirst);
 if (File.Exists(candidate)) return candidate;
 probe = probe.Parent;
 }
 }
 return baseDirCandidate;
 }
}
