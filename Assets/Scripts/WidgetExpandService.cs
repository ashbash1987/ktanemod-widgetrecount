using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public class WidgetExpandService : MonoBehaviour
{
    private static int SettingsVersion = 2;

    public class WidgetExpandModSettings
    {
        public int fileVersion;
        public bool allowWidgetCountChange = true;
        public int minimumWidgetCount = 7;
        public int maximumWidgetCount = 7;
        public bool allowSerialNumberChange = true;
        public int minimumCustomIndicators = 1;
        public bool allowCustomIndicators = true;
    }

    private WidgetExpandModSettings _modSettings = new WidgetExpandModSettings();

    private Type _widgetGeneratorType = null;
    private FieldInfo _widgetCountField = null;

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;

    private List<string> _knownIndicators = null;
    private List<string> _customIndicators = null;
    private bool _refreshWidgetCount = false;

    private Type _serialNumberType = null;
    private FieldInfo _serialStringField = null;
    private FieldInfo _serialNumberArrayField = null;

    private void DebugLog(object text, params object[] formatting)
    {
        Debug.LogFormat("[WidgetExpander] " + text, formatting);
    }

    private void ReadSettings()
    {
        string ModSettingsDirectory = Path.Combine(Application.persistentDataPath, "Modsettings");
        string ModSettings = Path.Combine(ModSettingsDirectory, "WidgetExpand-settings.txt");
        if (File.Exists(ModSettings))
        {
            string settings = File.ReadAllText(ModSettings);
            _modSettings = JsonConvert.DeserializeObject<WidgetExpandModSettings>(settings);
            DebugLog("Widget Expansion: {0}\nMinimum: {1}\nMaximum: {2}", _modSettings.allowWidgetCountChange ? "Enabled" : "Disabled", _modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount);
            DebugLog("Serial Number Change: {0}", _modSettings.allowSerialNumberChange ? "Enabled" : "Disabled");
            DebugLog("Custom Indicators: {0}\nMinimum: {1}", _modSettings.allowCustomIndicators ? "Enabled" : "Disabled", _modSettings.minimumCustomIndicators);

            if (_modSettings.fileVersion != SettingsVersion)
            {
                DebugLog("Settings version updated");
                _modSettings.fileVersion = SettingsVersion;
                settings = JsonConvert.SerializeObject(_modSettings, Formatting.Indented);
                File.WriteAllText(ModSettings, settings);
                DebugLog("New settings = {0}", settings);
            }
        }
        else
        {
            _modSettings = new WidgetExpandModSettings();
            try
            {
                if (!Directory.Exists(ModSettingsDirectory))
                {
                    Directory.CreateDirectory(ModSettingsDirectory);
                }

                _modSettings.fileVersion = SettingsVersion;
                string settings = JsonConvert.SerializeObject(_modSettings, Formatting.Indented);
                File.WriteAllText(ModSettings, settings);
                DebugLog("New settings = {0}", settings);
            }
            catch (Exception ex)
            {
                DebugLog("Failed to Create settings file due to Exception:\n{0}", ex.ToString());
            }
        }
    }

    private void InitCustomIndicators()
    {
        string _letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        _customIndicators = new List<string>();

        foreach (char x in _letters)
        {
            foreach (char y in _letters)
            {
                foreach (char z in _letters)
                {
                    _customIndicators.Add(x.ToString() + y + z);
                }
            }
        }

        foreach (string x in _knownIndicators)
        {
            _customIndicators.Remove(x);
        }
        _customIndicators.Remove("NLL");
    }

    private void Start()
    {
        _widgetGeneratorType = ReflectionHelper.FindType("WidgetGenerator");
        _widgetCountField = _widgetGeneratorType.GetField("NumberToGenerate", BindingFlags.Instance | BindingFlags.Public);

        _indicatorWidgetType = ReflectionHelper.FindType("IndicatorWidget");
        _indicatorLabelsField = _indicatorWidgetType.GetField("Labels", BindingFlags.Public | BindingFlags.Static);
        _knownIndicators = (List<string>) _indicatorLabelsField.GetValue(null);
        InitCustomIndicators();

        _serialNumberType = ReflectionHelper.FindType("SerialNumber");
        _serialStringField = _serialNumberType.GetField("serialString", BindingFlags.NonPublic | BindingFlags.Instance);
        _serialNumberArrayField = _serialNumberType.GetField("possibleCharArray", BindingFlags.NonPublic | BindingFlags.Instance);

        KMGameInfo gameInfoComponent = GetComponent<KMGameInfo>();
        gameInfoComponent.OnStateChange += OnStateChange;
        gameInfoComponent.OnLightsChange += OnLightsChange;
    }

    private void LateUpdate()
    {
        try
        {
            if (_refreshWidgetCount)
            {
                UpdateWidgetCount();
            }
        }
        catch (Exception ex)
        {
            DebugLog("An Exception occured: \n{0}", ex.ToString());
            _refreshWidgetCount = false;
        }
    }

    private KMGameInfo.State _state = KMGameInfo.State.Unlock;

    private void OnStateChange(KMGameInfo.State state)
    {
        DebugLog("Game State changed from {0} to {1}", _state.ToString(), state.ToString());

        switch (state)
        {
            case KMGameInfo.State.Setup:
            case KMGameInfo.State.PostGame:
            case KMGameInfo.State.Quitting:
            case KMGameInfo.State.Unlock:
                _refreshWidgetCount = false;
                break;

            case KMGameInfo.State.Transitioning:
                if (_state == KMGameInfo.State.Transitioning || _state == KMGameInfo.State.Gameplay)
                {
                    break;
                }
                _refreshWidgetCount = true;

                ReadSettings();
                _refreshWidgetCount &= _modSettings.allowWidgetCountChange;
                SetCustomIndicators(_modSettings.allowCustomIndicators);
                StartCoroutine(ReplaceSerialNumber(_modSettings.allowSerialNumberChange));
                break; 
        }
        _state = state;
    }

    private void DebugPrintList(List<string> list, int count)
    {
        string listStr = "";
        for (int i = 0; i < count; i++)
        {
            if (listStr == "")
            {
                listStr = list[i];
            }
            else
            {
                listStr += ", " + list[i];
            }

            if (i % 16 != 15)
            {
                continue;
            }
            DebugLog(listStr);
            listStr = "";
        }

        if (listStr != "")
        {
            DebugLog(listStr);
        }
    }

    private void ShuffleCustomIndicators()
    {
        List<string> list = _customIndicators;
        int n = _customIndicators.Count;
        while (n-- > 0)
        {
            int k = UnityEngine.Random.Range(0, n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void SetCustomIndicators(bool allowed)
    {
        if (!allowed)
        {
            _indicatorLabelsField.SetValue(null, _knownIndicators);
            return;
        }
        int count = Math.Max(_modSettings.maximumWidgetCount - 12, _modSettings.minimumCustomIndicators);
        count = Math.Min(count, _customIndicators.Count);

        List<string> labels = new List<string>(_knownIndicators);

        labels.Add("NLL");

        ShuffleCustomIndicators();
        for (int i = 0; i < count; i++)
        {
            labels.Add(_customIndicators[i]);
        }

        DebugLog("In Addition to the standard 11 Indicators as well as NLL");
        DebugLog("The following {0} may spawn on the upcoming bomb(s)", count);
        DebugPrintList(_customIndicators, count);

        _indicatorLabelsField.SetValue(null, labels);
    }

    private void UpdateWidgetCount()
    {
        UnityEngine.Object widgetGenerator = FindObjectOfType(_widgetGeneratorType);
        if (widgetGenerator == null)
        {
            return;
        }

        DebugLog("Widget generator found.");

        //Because the rule manager resets the random seed to 1 before bomb generation
        UnityEngine.Random.InitState((int)Time.time);

        int widgetCount = UnityEngine.Random.Range(Mathf.Min(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount), Mathf.Max(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount) + 1);
        _widgetCountField.SetValue(widgetGenerator, _modSettings.allowWidgetCountChange ? widgetCount : 5);
        if (_modSettings.allowWidgetCountChange)
        {
            DebugLog("Widget count set to {0}.", widgetCount);
        }

        _refreshWidgetCount = false;
    }

    private bool _lights = false;
    private void OnLightsChange(bool on)
    {
        _lights = on;
    }

    IEnumerator ReplaceSerialNumber(bool allowed)
    {
        DebugLog("ReplaceSerialNumber() Started...");
        List<UnityEngine.Object> snList = new List<UnityEngine.Object>();
        _lights = false;
        
        if (!allowed)
        {
            DebugLog("Serial number replacement not allowed. Aborting.");
            yield break;
        }

        DebugLog("Finding Serial Number...");
        object serialNumber = null;
        do
        {
            yield return new WaitUntil(() =>
            {
                serialNumber = FindObjectOfType(_serialNumberType);
                return serialNumber != null || _state == KMGameInfo.State.Setup || _state == KMGameInfo.State.PostGame;
            });
        } while (serialNumber == null && _state == KMGameInfo.State.Transitioning);

        if (serialNumber == null)
        {
            DebugLog("serialNumber == null. Aborting.");
            yield break;
        }

        DebugLog("Replacing allowable serial number characters...");
        while (!_lights)
        {
            foreach (UnityEngine.Object sn in FindObjectsOfType(_serialNumberType))
            {
                if (snList.Contains(sn))
                {
                    continue;
                }
                snList.Add(sn);

                string serialString = (string) _serialStringField.GetValue(sn);
                if (!string.IsNullOrEmpty(serialString))
                {
                    DebugLog("Serial number already generated. serialNumber = {0}.", serialString);
                    continue;
                }

                DebugLog("serialNumber = null.");
                _serialNumberArrayField.SetValue(sn, new char[]
                    {
                        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                        'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
                    }
                );
            }
            yield return null;
        }
        DebugLog("Done replacing serial number.");
    }
}
