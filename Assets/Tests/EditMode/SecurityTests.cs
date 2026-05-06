using NUnit.Framework;
using System.IO;
using UnityEngine;

public class SecurityTests
{
    // ── JStr JSON-escaping ────────────────────────────────────────────────────

    [Test]
    public void JStr_EscapesDoubleQuotes()
    {
        string result = UIManager.JStrForTest("say \"hello\"");
        StringAssert.Contains("\\\"", result);
        StringAssert.DoesNotContain("\"hello\"", result.Substring(1, result.Length - 2));
    }

    [Test]
    public void JStr_EscapesBackslash()
    {
        string result = UIManager.JStrForTest("C:\\Users");
        StringAssert.Contains("\\\\", result);
    }

    [Test]
    public void JStr_EscapesNewline()
    {
        string result = UIManager.JStrForTest("line1\nline2");
        StringAssert.Contains("\\n", result);
        Assert.IsFalse(result.Contains("\n"), "Literal newline must not appear in JSON string");
    }

    [Test]
    public void JStr_EscapesCarriageReturn()
    {
        string result = UIManager.JStrForTest("a\rb");
        StringAssert.Contains("\\r", result);
    }

    [Test]
    public void JStr_EscapesTab()
    {
        string result = UIManager.JStrForTest("a\tb");
        StringAssert.Contains("\\t", result);
    }

    [Test]
    public void JStr_WrapsInDoubleQuotes()
    {
        string result = UIManager.JStrForTest("hello");
        Assert.IsTrue(result.StartsWith("\"") && result.EndsWith("\""),
            "JStr output must be wrapped in double quotes");
    }

    [Test]
    public void JStr_HandlesEmptyString()
    {
        string result = UIManager.JStrForTest("");
        Assert.AreEqual("\"\"", result);
    }

    // ── No hardcoded secrets in source ────────────────────────────────────────

    [Test]
    public void NoHardcodedGitHubPAT_InScripts()
    {
        string[] files = Directory.GetFiles("Assets/Scripts", "*.cs",
                                            SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string src = File.ReadAllText(file);
            Assert.IsFalse(src.Contains("ghp_"),
                $"Possible hardcoded GitHub PAT (ghp_) found in {file}");
            Assert.IsFalse(src.Contains("github_pat_"),
                $"Possible hardcoded GitHub PAT (github_pat_) found in {file}");
        }
    }

    [Test]
    public void NoHardcodedGitHubPAT_InEditorScripts()
    {
        string[] files = Directory.GetFiles("Assets/Editor", "*.cs",
                                            SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string src = File.ReadAllText(file);
            Assert.IsFalse(src.Contains("ghp_"),
                $"Possible hardcoded GitHub PAT found in editor script {file}");
        }
    }

    // ── .gitignore coverage ───────────────────────────────────────────────────

    [Test]
    public void GitIgnore_CoversSecretsFile()
    {
        Assert.IsTrue(File.Exists(".gitignore"), ".gitignore must exist");
        string content = File.ReadAllText(".gitignore");
        StringAssert.Contains("bloodidle_secrets.txt", content,
            "bloodidle_secrets.txt must be listed in .gitignore");
    }

    [Test]
    public void SecretsFile_DoesNotExistInRepo()
    {
        // The real file must be gitignored and never committed.
        // If it exists locally that is fine, but it must not be tracked.
        // We verify the .gitignore entry covers it (tested above).
        // Additionally confirm the sample file (which IS tracked) contains
        // only a placeholder, not a real token.
        string sample = "Assets/Resources/bloodidle_secrets.txt.sample";
        if (!File.Exists(sample)) return;
        string content = File.ReadAllText(sample);
        Assert.IsFalse(content.Contains("ghp_"),
            "Sample secrets file must not contain a real PAT");
        Assert.IsFalse(content.Contains("github_pat_"),
            "Sample secrets file must not contain a real PAT");
    }
}
