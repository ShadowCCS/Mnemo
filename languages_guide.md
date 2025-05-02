# Adding a New Language to MnemoProject

This guide explains how to add a new language to MnemoProject without needing to modify any code.

## Simple Steps

1. Copy one of the existing language files (`Strings.*.resx`) from the `Languages` folder and rename it using the proper culture code:
   - Example: To add French, copy `Strings.resx` to `Strings.fr.resx`

2. Edit the file and translate all the text values (right side of the XML entries)

3. Add a special entry for the native language name:
   - Add a string with the key `__LanguageNativeName`
   - Set its value to the language name in its native form 
   - Example for French: `Français`

## Example

Here's what your entry should look like in the XML:

```xml
<data name="__LanguageNativeName" xml:space="preserve">
  <value>Français</value>
</data>
```

## Existing Language Files

The application currently includes the following language files:
- `Strings.resx` (English)
- `Strings.de.resx` (German)
- `Strings.es.resx` (Spanish)

## After Adding Files

Once you've added your language file to the `Languages` folder:

1. Start the application
2. Go to Settings > Appearance
3. Your new language should appear in the dropdown menu with its native name

No code changes are required! 