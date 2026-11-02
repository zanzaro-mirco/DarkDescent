#!/usr/bin/env bash
# I test in locale, con Unity chiuso (batchmode, mai -nographics). Lancia solo quello che serve:
#
#   Tools/run_tests.sh edit                 EditMode senza la ronda notturna (~1 min)
#   Tools/run_tests.sh play                 PlayMode senza ronda notturna né foto (~7 min)
#   Tools/run_tests.sh play Classe1;Classe2 solo quelle classi, per nome (come -testFilter)
#   Tools/run_tests.sh edit Classe1;Classe2
#
# Le categorie Nightly (la ronda, solo di notte dal cron) e Photos (il giro di foto, che riscrive
# Recordings/Photos) restano fuori: senza questi filtri un EditMode intero dura 6 minuti invece di 1,
# e una PlayMode intera ne perde 4 a fotografare. I risultati vanno in Temp/TestRuns/.
set -euo pipefail

UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.24f1/Editor/Unity.exe}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
MODE="${1:-edit}"
FILTER="${2:-}"

case "$MODE" in
  edit) PLATFORM=EditMode; CATEGORY='!Nightly' ;;
  play) PLATFORM=PlayMode; CATEGORY='!Nightly;!Photos' ;;
  *) echo "uso: $0 edit|play [Classe;Classe]" >&2; exit 2 ;;
esac

OUT="$PROJECT/Temp/TestRuns"
mkdir -p "$OUT"
ARGS=(-batchmode -projectPath "$PROJECT" -runTests -testPlatform "$PLATFORM" -testCategory "$CATEGORY"
      -testResults "$OUT/$MODE.xml" -logFile "$OUT/$MODE.log")
if [ -n "$FILTER" ]; then
  # i nomi corti diventano completi: DarkDescent.Tests.<Classe>
  FULL=""
  IFS=';' read -ra NAMES <<< "$FILTER"
  for name in "${NAMES[@]}"; do FULL+="DarkDescent.Tests.$name;"; done
  ARGS+=(-testFilter "${FULL%;}")
fi

"$UNITY" "${ARGS[@]}" || true
python - "$OUT/$MODE.xml" <<'PY'
import re, sys
text = open(sys.argv[1], encoding='utf-8').read()
run = re.search(r'<test-run [^>]*>', text).group(0)
get = lambda k: re.search(k + r'="(\d+)"', run).group(1)
print(f"{get('total')} test: {get('passed')} verdi, {get('failed')} falliti, {get('skipped')} saltati")
for case in re.finditer(r'<test-case [^>]*?fullname="([^"]+)"[^>]*?result="Failed".*?</test-case>', text, re.S):
    message = re.search(r'<message><!\[CDATA\[(.*?)\]\]></message>', case.group(0), re.S)
    print('  FALLITO', case.group(1)); print('   ', (message.group(1) if message else '?')[:300].replace('\n', ' '))
PY
