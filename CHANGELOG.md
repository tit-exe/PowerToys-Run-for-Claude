# Changelog

This project follows [semantic versioning](https://semver.org/spec/v2.0.0.html).

## 1.0.0

First release.

| Area | Detail |
| :--- | :--- |
| Query | A prompt opens a new Claude conversation, in the desktop app through `claude://` or in the browser through `claude.ai` |
| Activation | `cl:`, so that ordinary words starting with `cl` still reach your programs and files |
| Settings | One option: whether a result opens the desktop app, the browser, or both |
| Context menu | Open in the other destination with Ctrl+Enter, copy the link with Ctrl+C |
| Languages | 33, following the Windows display language |
| Links | Prompts are percent encoded and shortened to fit both the composer limit and the Windows command line ceiling, cut on whole characters |
| Icon | A typographic asterisk from Google's Material Symbols, in a light and a dark variant that follow the PowerToys theme |
| Warning | An activation command ending in a letter hides other plugins from ordinary words, so a query where that happens shows one result naming the command to use instead |
| Build | Reproducible: the same commit yields the same bytes on any machine |
| Architectures | x64 and ARM64 |
