using Newtonsoft.Json;
using System;
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

    private bool _refreshWidgetCount = true;

    private void Start()
    {
        _widgetGeneratorType = ReflectionHelper.FindType("WidgetGenerator");
        Debug.Log("Widget generator type = " + _widgetGeneratorType.ToString());

        _widgetCountField = _widgetGeneratorType.GetField("NumberToGenerate", BindingFlags.Instance | BindingFlags.Public);
        Debug.Log("Widget count field = " + _widgetCountField.ToString());

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

        _refreshWidgetCount = false;
    }
}
