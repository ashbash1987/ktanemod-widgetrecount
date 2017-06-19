using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class WidgetExpandService : MonoBehaviour
{
    public class WidgetExpandModSettings
    {
        public int minimumWidgetCount = 7;
        public int maximumWidgetCount = 7;
    }

    private WidgetExpandModSettings _modSettings = new WidgetExpandModSettings();

    private Type _widgetGeneratorType = null;
    private FieldInfo _widgetCountField = null;

    private Type _indicatorWidgetType = null;
    private FieldInfo _indicatorLabelsField = null;

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

        KMModSettings modSettingsComponent = GetComponent<KMModSettings>();
        Debug.Log("Widget expand settings = " + modSettingsComponent.Settings);
        _modSettings = JsonConvert.DeserializeObject<WidgetExpandModSettings>(modSettingsComponent.Settings);
        Debug.Log(string.Format("Widget expand settings: minimum = {0}, maximum = {1}", _modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount));

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

    private List<string> MakeList(int count)
    {
        List<string> labels = new List<string>
        {
            "SND",
            "CLR",
            "CAR",
            "IND",
            "FRQ",
            "SIG",
            "NSA",
            "MSA",
            "TRN",
            "BOB",
            "FRK",
            "NLL"
        };
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        for (var i = 0; i < count; i++)
        {
            
            string label;
            do
            {
                label = letters[UnityEngine.Random.Range(0, 26)].ToString();
                label += letters[UnityEngine.Random.Range(0, 26)].ToString();
                label += letters[UnityEngine.Random.Range(0, 26)].ToString();
            } while (labels.Contains(label));
            labels.Add(label);
        }

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

        //Because the rule manager resets the random seed to 1 before bomb generation
        UnityEngine.Random.InitState((int)Time.time);

        int widgetCount = UnityEngine.Random.Range(Mathf.Min(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount), Mathf.Max(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount) + 1);
        _widgetCountField.SetValue(widgetGenerator, widgetCount);
        Debug.Log(string.Format("Widget count set to {0}.", widgetCount));

        List<string> list = MakeList((_modSettings.minimumWidgetCount + _modSettings.maximumWidgetCount + 1) / 2);
        _indicatorLabelsField.SetValue(null, list);

        _refreshWidgetCount = false;
    }
}
