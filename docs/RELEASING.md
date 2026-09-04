# Releasing

## Before you tag

| Check | Where |
| :--- | :--- |
| Version matches in both places | `Directory.Build.props` and `src/Community.PowerToys.Run.Plugin.Claude/plugin.json`. A test fails if they drift |
| Changelog has a section for it | `CHANGELOG.md` |
| The demo shows the current build | `assets/demo.gif` |
| Tests pass | `dotnet test NewChatForClaude.sln -c Release /p:Platform=x64` |

## Cutting a release

```powershell
git tag v1.0.0
git push origin main --tags
```

The `Release` workflow runs on the tag. It checks the tag against `plugin.json`, runs the
tests, builds x64 and ARM64, and attaches four files to a GitHub release. Publishing the
release by hand from the GitHub interface also creates the tag, and the workflow then
attaches the archives to the release that already exists.

| File | Contents |
| :--- | :--- |
| `NewChatForClaude-<version>-x64.zip` | The plugin folder for x64 |
| `NewChatForClaude-<version>-x64.zip.sha256` | Checksum for that archive |
| `NewChatForClaude-<version>-arm64.zip` | The plugin folder for ARM64 |
| `NewChatForClaude-<version>-arm64.zip.sha256` | Checksum for that archive |

Each archive contains a single `NewChatForClaude` folder, which is what users extract into
`%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`.

## Building the archives yourself

```powershell
.\scripts\pack.ps1
```

The archives land in `artifacts`, which is not tracked. Pass `-Destination` to write them
somewhere else.

`pack.ps1` deletes every `bin` and `obj` folder before it builds, so an archive can never
carry a file the current build did not produce.

## Recording the demo

`assets/demo.gif` shows the whole gesture, which takes about eight seconds.

1. Open PowerToys Run with <kbd>Alt</kbd>+<kbd>Space</kbd>.
2. Type `cl:` followed by a short question.
3. Press <kbd>Enter</kbd>. Claude opens with the prompt written in the composer.
4. Press <kbd>Enter</kbd> again to send it, which is the part worth showing.

Aim for 720 pixels wide and under 5 MB. GitHub serves the file straight from the
repository, so anything heavier makes the page slow to load. Keep the filename so the
README needs no edit.
