using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public class WidgetExpandService : MonoBehaviour
{
    private static int SettingsVersion = 1;
    public class WidgetExpandModSettings
    {
        public int fileVersion;
        public int minimumWidgetCount = 7;
        public int maximumWidgetCount = 7;
        public bool enableCustomIndicators = true;
    }

    private WidgetExpandModSettings _modSettings = new WidgetExpandModSettings();

    private Type _widgetGeneratorType = null;
    private FieldInfo _widgetCountField = null;

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;

    private List<string> _knownIndicators = null;
    private List<string> _customIndicators = null;
    private bool _refreshWidgetCount = true;

    private void Start()
    {
        _widgetGeneratorType = ReflectionHelper.FindType("WidgetGenerator");
        Debug.Log("Widget generator type = " + _widgetGeneratorType.ToString());

        _widgetCountField = _widgetGeneratorType.GetField("NumberToGenerate", BindingFlags.Instance | BindingFlags.Public);
        Debug.Log("Widget count field = " + _widgetCountField.ToString());

        _indicatorWidgetType = ReflectionHelper.FindType("IndicatorWidget");
        Debug.Log("Indicator Widget type = " + _indicatorWidgetType.ToString());

        _indicatorLabelsField = _indicatorWidgetType.GetField("Labels", BindingFlags.Public | BindingFlags.Static);
        Debug.Log("Indicator labels field = " + _indicatorLabelsField.ToString());

        _knownIndicators = (List<string>) _indicatorLabelsField.GetValue(null);
        string indicators = "";
        foreach (string label in _knownIndicators)
        {
            if (indicators == "")
                indicators = label;
            else
                indicators += ", " + label;
        }
        Debug.LogFormat("The Following Indicators are present: {0}", indicators);

        KMModSettings modSettingsComponent = GetComponent<KMModSettings>();
        Debug.Log("Widget expand settings = " + modSettingsComponent.Settings);
        _modSettings = JsonConvert.DeserializeObject<WidgetExpandModSettings>(modSettingsComponent.Settings);
        Debug.Log(string.Format("Widget expand settings: minimum = {0}, maximum = {1}", _modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount));
        Debug.Log(string.Format("Enable custom indicators: {0}", _modSettings.enableCustomIndicators));

        KMGameInfo gameInfoComponent = GetComponent<KMGameInfo>();
        gameInfoComponent.OnStateChange += OnStateChange;
    }

    private void LateUpdate()
    {
        if (_refreshWidgetCount)
        {
            UpdateWidgetCount();
        }
    }

    private void OnStateChange(KMGameInfo.State state)
    {
        _refreshWidgetCount = _refreshWidgetCount || state == KMGameInfo.State.Transitioning || state == KMGameInfo.State.Setup;
    }

    private void RefreshSettings()
    {
        string ModSettings = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "WidgetExpand-settings.txt");
        if (File.Exists(ModSettings))
        {
            Debug.Log("Attempting to Refresh the settings");
            string settings = File.ReadAllText(ModSettings);
            _modSettings = JsonConvert.DeserializeObject<WidgetExpandModSettings>(settings);

            if (_modSettings.fileVersion != SettingsVersion)
            {
                Debug.Log("Settings Version changed - Adding new Settings");
                _modSettings.fileVersion = SettingsVersion;
                settings = JsonConvert.SerializeObject(_modSettings, Formatting.Indented);
                File.WriteAllText(ModSettings, settings);
                Debug.LogFormat("New settings = {0}", settings);
            }
        }
    }

    private void Shuffle()
    {
        if (_customIndicators == null)
        {
            string _letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            _customIndicators = new List<string>();

            foreach (var x in _letters)
                foreach (var y in _letters)
                    foreach (var z in _letters)
                        _customIndicators.Add(x.ToString() + y + z);

            foreach (var x in _knownIndicators)
                _customIndicators.Remove(x);
            _customIndicators.Remove("NLL");
        }

        var list = _customIndicators;
        int n = _customIndicators.Count;
        while (n-- > 0)
        {
            int k = UnityEngine.Random.Range(0, n+1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private List<string> MakeList()
    {
        Shuffle();

        int count = Math.Max(_modSettings.maximumWidgetCount - 12, (_modSettings.minimumWidgetCount + 1) / 2);
        count = Math.Min(count, _customIndicators.Count);

        List<string> labels = new List<string>(_knownIndicators);
        if (!_modSettings.enableCustomIndicators)
            return labels;

        labels.Add("NLL");


        Debug.Log("In Addition to the standard 11 Indicators as well as NLL");
        Debug.LogFormat("The following {0} may spawn on the upcoming bomb(s)",count);

        
        string indicators = "";
        for (var i = 0; i < count; i++)
        {
            if (indicators == "")
                indicators = _customIndicators[i];
            else
                indicators += ", " + _customIndicators[i];
            if ((i % 16) == 15)
            {
                Debug.Log(indicators);
                indicators = "";
            }
            labels.Add(_customIndicators[i]);
        }
        Debug.Log(indicators);

        return labels;
    }

    private void UpdateWidgetCount()
    {
        UnityEngine.Object widgetGenerator = FindObjectOfType(_widgetGeneratorType);
        if (widgetGenerator == null)
        {
            Debug.Log("Failed to find the widget generator object.");
            return;
        }

        //Get Fresh settings to apply to the next bomb
        RefreshSettings();

        //Because the rule manager resets the random seed to 1 before bomb generation
        UnityEngine.Random.InitState((int)Time.time);

        int widgetCount = UnityEngine.Random.Range(Mathf.Min(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount), Mathf.Max(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount) + 1);
        _widgetCountField.SetValue(widgetGenerator, widgetCount);
        Debug.Log(string.Format("Widget count set to {0}.", widgetCount));

        List<string> list = MakeList();
        _indicatorLabelsField.SetValue(null, list);
        
        _refreshWidgetCount = false;
    }
}
