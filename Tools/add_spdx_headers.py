#!/usr/bin/env python3
"""Add SPDX license headers to all source files.

Usage: python3 Tools/add_spdx_headers.py

Determines for each file whether it existed in commit `87c70a89a67d0521a56388e6b1c3f2cb947943e4`.
If a file existed in that commit, it predates the license switch and is
assumed to be MIT licensed. Otherwise it is assumed to be AGPL-3.0-or-later.

The appropriate header is inserted at the top of each source file if not
already present. Shebang lines are preserved.
"""

import subprocess
import sys
from pathlib import Path

# Commit hash where the AGPL transition happened
AGPL_COMMIT = "87c70a89a67d0521a56388e6b1c3f2cb947943e4"

# File extensions that should get C-style '//' comments
C_STYLE = {'.cs', '.js', '.ts', '.c', '.h', '.cpp', '.hpp', '.csx', '.java'}
# File extensions that should get hash '#' comments
HASH_STYLE = {'.py', '.ps1', '.sh', '.bash', '.rb'}

HEADER_MIT = "SPDX-License-Identifier: MIT"
HEADER_AGPL = "SPDX-License-Identifier: AGPL-3.0-or-later"

REPO_ROOT = Path(__file__).resolve().parents[1]


def file_exists_in_commit(commit: str, path: Path) -> bool:
    """Return True if the given path exists in the specified commit."""
    try:
        subprocess.run(
            ["git", "cat-file", "-e", f"{commit}:{path.as_posix()}"],
            cwd=REPO_ROOT,
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        return True
    except subprocess.CalledProcessError:
        return False


def comment_prefix(ext: str) -> str | None:
    if ext in C_STYLE:
        return "//"
    if ext in HASH_STYLE:
        return "#"
    return None


def add_header(path: Path, header: str, prefix: str) -> bool:
    """Insert the header into the file if not already present.

    Returns True if the file was modified.
    """
    try:
        data = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        print(f"Skipping non-text file: {path}")
        return False

    lines = data.splitlines()

    if lines and lines[0].startswith(prefix) and "SPDX-License-Identifier" in lines[0]:
        return False  # already has header

    insert_at = 0
    if lines and lines[0].startswith("#!"):
        insert_at = 1

    header_line = f"{prefix} {header}"
    lines.insert(insert_at, header_line)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return True


def main():
    files = subprocess.check_output(["git", "ls-files"], cwd=REPO_ROOT)
    modified = 0
    for rel in files.decode().splitlines():
        path = REPO_ROOT / rel
        if not path.is_file():
            continue
        ext = path.suffix.lower()
        prefix = comment_prefix(ext)
        if not prefix:
            continue

        header = HEADER_MIT if file_exists_in_commit(AGPL_COMMIT, path) else HEADER_AGPL
        if add_header(path, header, prefix):
            print(f"Updated {rel}")
            modified += 1

    print(f"Done. {modified} files modified.")


if __name__ == "__main__":
    sys.exit(main())
