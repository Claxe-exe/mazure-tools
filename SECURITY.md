# Security policy

## Reporting a vulnerability

Please **do not** open a public issue for security problems. Use GitHub's *Report a vulnerability* button (Security tab → Advisories) so it can be fixed before it is disclosed.

Include the Mazure Tools version, your Windows version, and steps to reproduce.

## What Mazure Tools does and does not do

- It runs as the current user and never elevates silently; elevation goes through Windows' UAC prompt.
- It sends no telemetry and makes no network requests on its own; the public-IP lookup happens only when you press *Look up*.
- The process guard is a name-based safety net against accidental clicks. It is **not** a security boundary and does not protect against malware that reuses a protected process name.
- Release binaries are currently **not code-signed**. Verify downloads against `SHA256SUMS.txt` from the release page.
