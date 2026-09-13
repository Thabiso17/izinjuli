#!/usr/bin/env bash
#
# Brings the API up with a league in it, ready for the browser tests to drive.
#
# Two starts, not one, and the reason is in AuthTestDataSeeder: it scopes divadmin@test.com and
# teamadmin@test.com to the first division and team it finds, and on a fresh database there are
# none yet, so it logs a warning and leaves them unassigned. Seeding the league then restarting
# is what a developer does locally, and it is what gives those two accounts something to
# administer.
#
# Environment expected:
#   DATABASE_URL              where to find PostgreSQL
#   ASPNETCORE_URLS           what the API listens on (default http://localhost:5207)
set -euo pipefail

API_URL="${ASPNETCORE_URLS:-http://localhost:5207}"
LOG="${API_LOG:-api.log}"
PID_FILE="${API_PID_FILE:-api.pid}"

wait_for_api() {
  for _ in $(seq 1 60); do
    if curl -sf "$API_URL/api/health" > /dev/null; then
      return 0
    fi
    sleep 5
  done
  echo "The API did not come up at $API_URL within five minutes." >&2
  tail -n 100 "$LOG" >&2 || true
  return 1
}

start_api() {
  # setsid detaches the API from this script's process group, so it keeps running after the
  # step that started it has finished and the browser tests can still reach it.
  setsid nohup dotnet run --project iDiski.Api --configuration Release --no-build \
    >> "$LOG" 2>&1 < /dev/null &
  echo $! > "$PID_FILE"
  wait_for_api
}

stop_api() {
  if [ -f "$PID_FILE" ]; then
    kill "$(cat "$PID_FILE")" 2>/dev/null || true
    # dotnet run spawns the app as a child, so clear anything still holding the port.
    pkill -f 'iDiski.Api' 2>/dev/null || true
    sleep 5
    rm -f "$PID_FILE"
  fi
}

echo "Starting the API for the first time (migrations and admin accounts)..."
start_api

echo "Seeding divisions, teams and players..."
curl -sf "$API_URL/api/seed" || {
  echo "Seeding failed." >&2
  tail -n 100 "$LOG" >&2
  exit 1
}
echo

echo "Restarting so the division and team admins get their assignments..."
stop_api
start_api

echo "The API is ready at $API_URL."
