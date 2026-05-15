#!/usr/bin/env bash
# Install git hooks from scripts/hooks/ into .git/hooks/.
# Run once after cloning: bash scripts/install-hooks.sh
set -euo pipefail

REPO_ROOT="$(git -C "$(dirname "$0")" rev-parse --show-toplevel)"
HOOKS_SRC="$REPO_ROOT/scripts/hooks"
HOOKS_DST="$REPO_ROOT/.git/hooks"

for src in "$HOOKS_SRC"/*; do
  name=$(basename "$src")
  dst="$HOOKS_DST/$name"
  cp "$src" "$dst"
  chmod +x "$dst"
  echo "Installed hook: $name"
done

echo "Done. Hooks active in .git/hooks/."
