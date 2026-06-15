using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class DataStore : MonoBehaviour
{
    private static DataStore _instance;
    private const string FULL_JSON_KEY = "FullJsonData";
    private bool isSavedPanelActive = false;

    public event Action<string, object> OnDataChanged;

    private List<int> selectedPointIndices = new List<int>();

    public List<int> GetSelectedPointIndices()
    {
        return selectedPointIndices;
    }

    public void AddSelectedPointIndex(int index)
    {
        if (!selectedPointIndices.Contains(index))
        {
            selectedPointIndices.Add(index);
            OnDataChanged?.Invoke("SelectedPointIndices", selectedPointIndices);
            _selectedData["SelectedPointIndices"] = new List<int>(selectedPointIndices);
        }
    }

    public void RemoveSelectedPointIndex(int index)
    {
        if (selectedPointIndices.Contains(index))
        {
            selectedPointIndices.Remove(index);
            OnDataChanged?.Invoke("SelectedPointIndices", selectedPointIndices);
            _selectedData["SelectedPointIndices"] = new List<int>(selectedPointIndices);
        }
    }

    public void ClearSelectedPointIndices()
    {
        selectedPointIndices.Clear();
        OnDataChanged?.Invoke("SelectedPointIndices", selectedPointIndices);
        _selectedData["SelectedPointIndices"] = new List<int>(selectedPointIndices);
    }

    public void SetFullJsonData(string jsonData)
    {
        SetTransformerData(FULL_JSON_KEY, jsonData);
    }

    public string GetFullJsonData()
    {
        return GetTransformerData<string>(FULL_JSON_KEY);
    }

    public void SetSavedPanelActive(bool isActive)
    {
        isSavedPanelActive = isActive;
    }

    public bool IsSavedPanelActive()
    {
        return isSavedPanelActive;
    }    
    public static DataStore Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DataStore");
                _instance = go.AddComponent<DataStore>();
                if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
            }
            return _instance;
        }
    }

    private bool colliderInteractionEnabled = true;

    private Dictionary<string, object> _transformerData = new Dictionary<string, object>();
    private Dictionary<string, object> _selectedData = new Dictionary<string, object>();
    private Dictionary<string, object> _savedData = new Dictionary<string, object>();
    private Dictionary<string, object> _detailData = new Dictionary<string, object>();
    private Dictionary<string, object> _annotationData = new Dictionary<string, object>();
    private Dictionary<string, Color[]> _colorData = new Dictionary<string, Color[]>();
    public void SetColliderInteractionEnabled(bool enabled)
    {
        colliderInteractionEnabled = enabled;
    }

    public bool GetColliderInteractionEnabled()
    {
        return colliderInteractionEnabled;
    }

    public int GetSavedItemsCount()
    {
        if (_savedData.TryGetValue("SavedIds", out object value) && value is List<string> savedIds)
        {
            return savedIds.Count;
        }
        return 0;
    }

    public void SetTransformerData(string key, object value)
    {
        _transformerData[key] = value;
    }

    public T GetTransformerData<T>(string key)
    {
        if (_transformerData.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    public IEnumerable<string> GetTransformerDataKeys()
    {
        return _transformerData.Keys;
    }

    public void SetSelectedData(string key, object value)
    {
        _selectedData[key] = value;
    }

    public T GetSelectedData<T>(string key)
    {
        if (_selectedData.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    public void SetSavedData(string key, object value, bool updateState = true)
    {
        _savedData[key] = value;
        if (updateState)
        {
            UpdateSavedDataState();
        }
    }

    public T GetSavedData<T>(string key)
    {
        if (_savedData.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    private void UpdateSavedDataState()
    {
    }

    public void SetDetailData(string key, object value, bool updateState = true)
    {
        _detailData[key] = value;
        if (updateState)
        {
            UpdateDetailDataState();
        }
    }

    public T GetDetailData<T>(string key)
    {
        if (_detailData.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    public void ClearDetailData()
    {
        _detailData.Clear();
        UpdateDetailDataState();
    }

    private void UpdateDetailDataState()
    {
    }

    public void SetAnnotationData(string key, object value)
    {
        _annotationData[key] = value;
    }

    public T GetAnnotationData<T>(string key)
    {
        if (_annotationData.TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }

    public void SetCurrentColourArray(string key, Color[] colorArray)
    {
        _savedData[key] = colorArray;
    }

    public Color[] GetCurrentColourArray(string key)
    {
        if (_savedData.TryGetValue(key, out object value) && value is Color[] colorArray)
        {
            return colorArray;
        }
        return null;
    }

    public void ClearTransformerData() { _transformerData.Clear(); }
    public void ClearSelectedData() { _selectedData.Clear(); }

    public void ClearSavedData()
    {
        _savedData.Clear();
        UpdateSavedDataState();
        OnDataChanged?.Invoke("SavedDataCleared", null);
    }

    public void ClearAnnotationData() { _annotationData.Clear(); }

    public void ClearAllData()
    {
        ClearTransformerData();
        ClearSelectedData();
        ClearSavedData();
        ClearAnnotationData();
        ClearDetailData();
    }

    public void DebugLogAllData()
    {
        Debug.Log("Transformer Data at datastore:");
        foreach (var kvp in _transformerData)
        {
            if (kvp.Value is float[] floatArray)
            {
                int sampleSize = Mathf.Min(5, floatArray.Length);
                Debug.Log($"  {kvp.Key}: Array of length {floatArray.Length}, First {sampleSize} values: {string.Join(", ", floatArray.Take(sampleSize))}");
            }
            else
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }
    }
}
        















