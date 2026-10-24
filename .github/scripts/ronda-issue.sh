#!/usr/bin/env bash
# L'issue della ronda notturna (D9 della M15), solo quando la ronda fallisce: il titolo con la data
# e il numero di errori, nel corpo le parti rosse del rapporto e il link al run. Se un'issue `ronda`
# è ancora aperta, un commento invece di una nuova; si chiude a mano quando l'errore è corretto.
#
# Uso: ronda-issue.sh <rapporto> <seme della notte> <commit> <url del run>
# Con DRY_RUN=1 scrive titolo e corpo invece di chiamare gh (per provarlo in locale).
set -euo pipefail

report="$1"
seed="$2"
commit="$3"
run_url="$4"

months=(gen feb mar apr mag giu lug ago set ott nov dic)
date_text="$(date -u +%-d) ${months[$(( $(date -u +%-m) - 1 ))]} $(date -u +%Y)"

# ogni parte rossa dice quanti errori ha: "<summary>12 errori, i primi 10</summary>" o "<summary>1 errore</summary>"
errors=0
if [ -f "$report" ]; then
  errors=$(awk 'match($0, /<summary>[0-9]+ error[ei]/) { s += substr($0, RSTART + 9) + 0 } END { print s + 0 }' "$report")
fi

case "$errors" in
  0) title="Ronda del $date_text: non è arrivata alla fine" ;;
  1) title="Ronda del $date_text: 1 errore" ;;
  *) title="Ronda del $date_text: $errors errori" ;;
esac

body="$(mktemp)"
{
  echo "**$title**"
  echo
  echo "- Il run: $run_url"
  echo "- Seme della notte: \`$seed\`"
  echo "- Commit: \`${commit:0:7}\`"
  echo "- Per rifarla in locale: \`-nightly-seed $seed\`"
  echo
  if [ -f "$report" ]; then
    # solo le parti rosse: una parte va da un titolo ### al successivo
    awk '/^### / { if (red) printf "%s", part; part = ""; red = 0 }
         { part = part $0 "\n"; if ($0 ~ /^❌/) red = 1 }
         END { if (red) printf "%s", part }' "$report"
  else
    echo "Nessun rapporto: la ronda si è fermata prima di scriverlo. Il perché è nel log del run."
  fi
} > "$body"

if [ "${DRY_RUN:-0}" = 1 ]; then
  echo "titolo: $title"
  cat "$body"
  exit 0
fi

gh label create ronda --color B60205 --description "Errori trovati dalla ronda notturna" --force
open="$(gh issue list --label ronda --state open --limit 1 --json number --jq '.[0].number // empty')"
if [ -n "$open" ]; then
  gh issue comment "$open" --body-file "$body"
else
  gh issue create --title "$title" --label ronda --body-file "$body"
fi
