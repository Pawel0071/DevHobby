#!/usr/bin/env bash
set -euo pipefail

# Runs the DocumentRepository CLI scenarios for all entities unless an entity name filter is provided.
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLI_DIR="$ROOT_DIR/RPG.CLI"

pushd "$CLI_DIR" >/dev/null

dotnet run -- document-tests "$@"

popd >/dev/null
