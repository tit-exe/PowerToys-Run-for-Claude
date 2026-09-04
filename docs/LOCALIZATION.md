# Adding a language

Every string the plugin shows lives in one file: `src/Community.PowerToys.Run.Plugin.Claude/Strings.json`.
Adding a language means adding one block to it. There is no project file to edit and no
new file to create.

## Steps

1. Open `Strings.json`. It maps a culture code to that language's strings.
2. Copy the `en` block, paste it at the end, and rename the key to your culture code.
3. Translate the values, keeping `Claude` and `Ctrl+` exactly as they are.
4. Build. The file is embedded in the assembly, so nothing else changes.
5. Run the tests. They check your block against the English one.

```json
{
  "en": {
    "PluginName": "New Chat for Claude",
    "TitleNewChat": "New chat"
  },
  "fr": {
    "PluginName": "Nouvelle conversation pour Claude",
    "TitleNewChat": "Nouvelle conversation"
  }
}
```

## Culture codes

Use the neutral code, not a country specific one.

| Use | Not | Why |
| :--- | :--- | :--- |
| `de` | `de-DE` | `de` also serves de-AT and de-CH |
| `fr` | `fr-FR` | `fr` also serves fr-CA and fr-BE |
| `zh-Hans` | `zh-CN` | A script code serves every region using that script |

A country code is only right when the wording genuinely differs, which is why `pt` and
`pt-BR` are separate blocks here.

Lookup walks up from the interface language until it finds a match, so `fr-CA` resolves
to `fr`, `zh-CN` resolves to `zh-Hans`, and anything unknown resolves to English.

## Why one file rather than satellite assemblies

The usual .NET approach ships one small assembly per language, in one folder per
language. For a plugin with eighteen strings that would mean thirty two extra folders and
thirty two extra binaries next to the one that matters, for about twenty kilobytes of
text. Embedding the text keeps the plugin a single assembly: one file to install, one
file to read, and nowhere for anything unexpected to sit.

## Rules the tests enforce

| Rule | Reason |
| :--- | :--- |
| Same keys as English | A missing key falls back silently, so half a translation would look finished |
| No empty values | Same reason |
| Same `{0}` placeholders | A lost placeholder drops the browser name from the sentence |
| `Claude` kept verbatim | It is a product name, so it is never translated, only surrounded |
| `Ctrl+` kept verbatim | These are the labels printed on the keys |
| The culture code is one Windows knows | A typo would silently never match |
| The language reaches a real result | Proves the text is embedded, not just present in the file |

## Checking your work

```powershell
dotnet test NewChatForClaude.sln -c Release /p:Platform=x64
```

To see your language in the launcher, change the Windows display language, sign out and
back in, then restart PowerToys. The plugin follows the same setting PowerToys does.
