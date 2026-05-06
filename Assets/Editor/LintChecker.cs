#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public static class LintChecker
{
    [MenuItem("IdleClicker/Run Lint Check", priority = 10)]
    public static void Run()
    {
        int warnings = 0, errors = 0;

        CheckDirectory("Assets/Scripts", ref warnings, ref errors);
        CheckGitIgnore(ref errors);

        if (errors == 0 && warnings == 0)
            Debug.Log("[Lint] ✓ All checks passed.");
        else
            Debug.Log($"[Lint] Done — {errors} error(s), {warnings} warning(s). See above for details.");
    }

    static void CheckDirectory(string dir, ref int warnings, ref int errors)
    {
        foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            string name  = Path.GetRelativePath(".", file);
            string[] lines = File.ReadAllLines(file);

            for (int i = 0; i < lines.Length; i++)
            {
                string line    = lines[i];
                string trimmed = line.TrimStart();
                int    lineNum = i + 1;

                // Skip commented-out lines
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;

                // Debug.Log in production scripts
                if (Regex.IsMatch(line, @"\bDebug\.Log\b"))
                {
                    Debug.LogWarning($"[Lint] Debug.Log: {name}:{lineNum}  →  {trimmed}");
                    warnings++;
                }

                // Hardcoded GitHub tokens
                if (line.Contains("ghp_") || line.Contains("github_pat_"))
                {
                    Debug.LogError($"[Lint] Hardcoded PAT: {name}:{lineNum}");
                    errors++;
                }

                // TODO / FIXME markers
                if (Regex.IsMatch(line, @"//\s*(TODO|FIXME|HACK|XXX)\b", RegexOptions.IgnoreCase))
                {
                    Debug.LogWarning($"[Lint] Unresolved marker: {name}:{lineNum}  →  {trimmed}");
                    warnings++;
                }

                // Naked catch blocks (swallows errors silently)
                if (Regex.IsMatch(line, @"\bcatch\s*\(\s*(Exception\s+\w+|Exception\s*)\s*\)\s*\{?\s*$"))
                {
                    Debug.LogWarning($"[Lint] Broad catch: {name}:{lineNum}");
                    warnings++;
                }
            }
        }
    }

    static void CheckGitIgnore(ref int errors)
    {
        if (!File.Exists(".gitignore"))
        {
            Debug.LogError("[Lint] .gitignore not found at project root.");
            errors++;
            return;
        }

        string content = File.ReadAllText(".gitignore");
        if (!content.Contains("bloodidle_secrets.txt"))
        {
            Debug.LogError("[Lint] bloodidle_secrets.txt is NOT listed in .gitignore — secret would be committed!");
            errors++;
        }
    }
}
#endif
