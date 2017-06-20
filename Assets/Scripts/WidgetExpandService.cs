using Newtonsoft.Json;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class WidgetExpandService : MonoBehaviour
{
    public class WidgetExpandModSettings
    {
        public bool allowWidgetCountChange = true;
        public int minimumWidgetCount = 7;
        public int maximumWidgetCount = 7;
        
        public bool allowSerialNumberChange = true;
    }

    private WidgetExpandModSettings _modSettings = new WidgetExpandModSettings();
    private KMModSettings modSettingsComponent;

    private Type _widgetGeneratorType = null;
    private FieldInfo _widgetCountField = null;

    private bool _refreshWidgetCount = true;

    private Type _serialNumberType;
    private FieldInfo _serialNumberArrayField;
    private MethodInfo _serialNumberStartMethod;

    private void DebugLog(object text, params object[] formatting)
    {
        Debug.LogFormat("[WidgetExpander] " + text, formatting);
    }

    private void ReadSettings()
    {
        _modSettings = JsonConvert.DeserializeObject<WidgetExpandModSettings>(modSettingsComponent.Settings);
        DebugLog("Widget Expansion: {0}\nMinimum: {1}\nMaximum: {2}", _modSettings.allowWidgetCountChange ? "Enabled" : "Disabled", _modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount);
        DebugLog("Serial Number Change: ", _modSettings.allowSerialNumberChange ? "Enabled" : "Disabled");
    }

    private void Start()
    {
        _widgetGeneratorType = ReflectionHelper.FindType("WidgetGenerator");
        _widgetCountField = _widgetGeneratorType.GetField("NumberToGenerate", BindingFlags.Instance | BindingFlags.Public);

        modSettingsComponent = GetComponent<KMModSettings>();
        
        _serialNumberType = ReflectionHelper.FindType("SerialNumber");
        _serialNumberArrayField = _serialNumberType.GetField("possibleCharArray", BindingFlags.NonPublic | BindingFlags.Instance);
        _serialNumberStartMethod = _serialNumberType.GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance);

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
        _refreshWidgetCount = _refreshWidgetCount || state == KMGameInfo.State.Gameplay;

        if (_refreshWidgetCount && _modSettings.allowSerialNumberChange)
        {
            StartCoroutine(ReplaceSerialNumber());
        }
       
    }

    private void UpdateWidgetCount()
    {
        UnityEngine.Object widgetGenerator = FindObjectOfType(_widgetGeneratorType);
        if (widgetGenerator == null)
        {
            return;
        }

        //Because the rule manager resets the random seed to 1 before bomb generation
        UnityEngine.Random.InitState((int)Time.time);

        ReadSettings();

        int widgetCount = UnityEngine.Random.Range(Mathf.Min(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount), Mathf.Max(_modSettings.minimumWidgetCount, _modSettings.maximumWidgetCount) + 1);
        _widgetCountField.SetValue(widgetGenerator, _modSettings.allowWidgetCountChange ? widgetCount : 5);
        if (_modSettings.allowWidgetCountChange)
        {
            DebugLog("Widget count set to {0}.", widgetCount);
        }

        _refreshWidgetCount = false;
    }

    IEnumerator ReplaceSerialNumber()
    {
        object serialNumber = null;
        yield return new WaitUntil(() => { serialNumber = FindObjectOfType(_serialNumberType); return serialNumber != null; });

        DebugLog("Replacing serial number...");

        _serialNumberArrayField.SetValue(serialNumber, new char[]
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
            }
        );

        _serialNumberStartMethod.Invoke(serialNumber, null);
    }
}
