# LintChecker

Editor-only static class. Scans `Assets/Scripts/` for common code quality issues and reports them to the Unity Console.

Assembly: Editor (`Assets/Editor/`)  
File: `Assets/Editor/LintChecker.cs`  
Menu: **IdleClicker → Run Lint Check** (priority 10)

---

## Usage

Click **IdleClicker → Run Lint Check** in the Unity menu bar. Results appear in the Console window. Exit codes:

- `[Lint] ✓ All checks passed.` — no issues found
- `[Lint] Done — N error(s), M warning(s).` — issues listed above this line

---

## Rules

### Errors (block shipping)

| Rule | Pattern | Rationale |
|------|---------|-----------|
| Hardcoded PAT | Line contains `ghp_` or `github_pat_` | Prevents committing GitHub access tokens |
| Missing .gitignore | `.gitignore` doesn't exist at project root | |
| Secrets not ignored | `.gitignore` doesn't contain `bloodidle_secrets.txt` | Prevents the secrets file from being committed |

### Warnings (code smell)

| Rule | Pattern | Rationale |
|------|---------|-----------|
| Debug.Log | `\bDebug\.Log\b` | Logging left in production builds |
| Unresolved markers | `// TODO`, `// FIXME`, `// HACK`, `// XXX` | Indicates incomplete work |
| Broad catch | `catch(Exception e) {` or `catch(Exception) {` | Silently swallows errors |

Lines beginning with `//` or `*` (comments) are skipped for the Debug.Log, marker, and catch checks.

---

## Extending

Add new checks inside `CheckDirectory()` — iterate `lines[i]` with `Regex.IsMatch()` or `string.Contains()`, call `Debug.LogWarning()` for warnings and `Debug.LogError()` for errors, and increment the respective counter. For project-level checks (file existence, config validation) follow the `CheckGitIgnore()` pattern.
