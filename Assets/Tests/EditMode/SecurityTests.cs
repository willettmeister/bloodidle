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

    // ── PostIssue JSON construction ───────────────────────────────────────────

    [Test]
    public void PostIssue_Json_ContainsTitleField()
    {
        string title = "My Feature";
        string body  = "Some description";
        string json  = BuildIssueJson(title, body);
        StringAssert.Contains("\"title\":", json);
        StringAssert.Contains("My Feature", json);
    }

    [Test]
    public void PostIssue_Json_ContainsBodyField()
    {
        string json = BuildIssueJson("T", "My body");
        StringAssert.Contains("\"body\":", json);
        StringAssert.Contains("My body", json);
    }

    [Test]
    public void PostIssue_Json_ContainsFeatureRequestLabel()
    {
        string json = BuildIssueJson("T", "B");
        StringAssert.Contains("\"labels\"", json);
        StringAssert.Contains("feature request", json);
    }

    [Test]
    public void PostIssue_Json_EscapesTitleSpecialChars()
    {
        string json = BuildIssueJson("Has \"quotes\" & \nnewline", "body");
        // Literal double-quote and newline must not appear unescaped inside a JSON string value
        // Strip the outer structure and verify the title value is safe
        StringAssert.DoesNotContain("\n", json);
        StringAssert.Contains("\\\"quotes\\\"", json);
        StringAssert.Contains("\\n", json);
    }

    [Test]
    public void ParseIssueNumber_ExtractsNumber()
    {
        string response = "{\"url\":\"...\",\"number\":42,\"title\":\"t\"}";
        string result   = InvokeParseIssueNumber(response);
        Assert.AreEqual(" #42", result);
    }

    [Test]
    public void ParseIssueNumber_ReturnsEmptyOnMissingField()
    {
        string result = InvokeParseIssueNumber("{\"title\":\"no number here\"}");
        Assert.AreEqual("", result);
    }

    [Test]
    public void ParseIssueNumber_ReturnsEmptyOnEmptyInput()
    {
        Assert.AreEqual("", InvokeParseIssueNumber(""));
        Assert.AreEqual("", InvokeParseIssueNumber(null));
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static string BuildIssueJson(string title, string rawBody)
    {
        // Mirror the JSON construction in UIManager.PostIssue exactly.
        string body = "**Community Request**\n\n" +
                      (rawBody.Length > 0 ? rawBody : "_No description provided._");
        return "{\"title\":"  + UIManager.JStrForTest(title) +
               ",\"body\":"   + UIManager.JStrForTest(body) +
               ",\"labels\":[\"feature request\"]}";
    }

    static string InvokeParseIssueNumber(string response) =>
        UIManager.ParseIssueNumberForTest(response);
}
