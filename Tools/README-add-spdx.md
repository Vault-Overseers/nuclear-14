# Adding SPDX license headers

This repository switched from MIT to AGPL at commit `87c70a89a67d0521a56388e6b1c3f2cb947943e4`. To update all source files with the appropriate `SPDX-License-Identifier` header you can run the helper script included here.

## Usage

1. Ensure you have Python 3 available.
2. From the repository root run:

   ```bash
   python3 Tools/add_spdx_headers.py
   ```

The script will check each tracked file. If the file existed at the transition commit it receives an MIT header, otherwise it receives an AGPL header. Files already containing an SPDX header are skipped. Shebang lines are preserved.

After running, review the changes with `git status` and `git diff`. When satisfied, commit the modifications.
