#!/usr/bin/env bash
# Regenerate docs/diagrams/*.svg from Mermaid code blocks in docs/**/*.md.
# Usage: scripts/gen-diagrams.sh [file ...]
#   No args  — processes all docs/*.md
#   With args — processes only the listed files
set -euo pipefail

DIAGRAMS_DIR="docs/diagrams"

if ! command -v mmdc &>/dev/null; then
  echo "[docs] mmdc not found — install with: npm install -g @mermaid-js/mermaid-cli" >&2
  exit 1
fi

mkdir -p "$DIAGRAMS_DIR"

files=("${@:-docs/*.md}")
total=0
failed=0

for mdfile in "${files[@]}"; do
  [[ -f "$mdfile" ]] || continue
  basename=$(basename "$mdfile" .md)

  i=0
  in_block=0
  block=""

  while IFS= read -r line || [[ -n "$line" ]]; do
    if [[ "$line" == '```mermaid' ]]; then
      in_block=1
      block=""
    elif [[ "$line" == '```' && $in_block -eq 1 ]]; then
      in_block=0
      tmpfile=$(mktemp /tmp/mermaid_XXXXXX.mmd)
      printf '%s\n' "$block" > "$tmpfile"
      outfile="${DIAGRAMS_DIR}/${basename}_${i}.svg"
      if mmdc -i "$tmpfile" -o "$outfile" -b transparent 2>/dev/null; then
        echo "[docs] Generated $outfile"
        total=$((total + 1))
      else
        echo "[docs] WARN: failed to render diagram $i in $mdfile" >&2
        failed=$((failed + 1))
      fi
      rm -f "$tmpfile"
      i=$((i + 1))
    elif [[ $in_block -eq 1 ]]; then
      block+=$'\n'"$line"
    fi
  done < "$mdfile"
done

echo "[docs] $total diagram(s) generated, $failed failed."
[[ $failed -eq 0 ]]
