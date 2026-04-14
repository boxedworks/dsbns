
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
  public class LocalizationController
  {
    static Settings.SettingsSaveData SettingsModule { get { return Settings.s_SaveData.Settings; } }

    static LocalizationController _Singleton;

    const string LanguageFilesPath = @"Localization\";
    Language _currentLanguage
    {
      get
      {
        var langStr = SettingsModule.Language;
        if (Enum.TryParse(langStr, out Language lang))
        {
          return lang;
        }
        else
        {
          Debug.LogWarning($"Invalid language in settings: {langStr}. Defaulting to English.");
          return Language.English;
        }
      }
    }

    Dictionary<string, string> _localizationDictionary;
    public LocalizationController()
    {
      _Singleton = this;

      // Load saved language on load
      var savedLanguage = _currentLanguage;
      LoadLanguage(savedLanguage);
    }

    // Load another language
    public enum Language
    {
      English,
    }
    public void LoadLanguage(Language language)
    {
      var languageData = LoadLanguageFile(language);
      if (languageData == null) return;

      _localizationDictionary = new();
      foreach (var line in languageData)
      {
        var parts = line.Split(',');
        if (parts.Length < 2) continue;
        var key = parts[0].Trim();
        var value = parts[1].Trim();
        _localizationDictionary[key] = value;
      }

      // Reload menus to update text
      //Menu.Init();

      // Save new language to settings
      SettingsModule.Language = language.ToString();
      Settings.SettingsSaveData.Save();
    }

    // Get localized string by key
    public static string GetString(string key)
    {
      var localizationDict = _Singleton._localizationDictionary;
      if (localizationDict == null || !localizationDict.ContainsKey(key))
      {
        Debug.LogWarning($"Localization key not found: {key}");
        return key; // Return key as fallback
      }

      return localizationDict[key];
    }

    // Load CSV file and return
    static string[] LoadLanguageFile(Language language)
    {
      var filePath = $"{LanguageFilesPath}{language}";
      var textAsset = Resources.Load<TextAsset>(filePath);
      if (textAsset == null)
      {
        Debug.LogError($"Localization file not found for language: {language}");
        return null;
      }

      var lines = textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      return lines;
    }

  }
}