using UnityEngine;
using System.IO;
using TMPro;

public class RecallJSONData : MonoBehaviour
{
    private DataProcessing dataProcessing;
    private string path = "";

    private void OnEnable()
    {
        SetPath();

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogWarning($"RecallJSONData: No save file found in '{Application.persistentDataPath}'.");
            return;
        }

        LoadData();
    }

    private void SetPath()
    {
        //path = Application.persistentDataPath + "/gameSaveData.json";

        var dir = Application.persistentDataPath;
        if (!Directory.Exists(dir))
        {
            path = "";
            return;
        }

        var files = Directory.GetFiles(dir, "*.json");
        if (files == null || files.Length == 0)
        {
            path = "";
            return;
        }

        string newest = files[0];
        var newestTime = File.GetLastWriteTimeUtc(newest);

        for (int i = 1; i < files.Length; i++)
        {
            var t = File.GetLastWriteTimeUtc(files[i]);
            if (t > newestTime)
            {
                newest = files[i];
                newestTime = t;
            }
        }

        path = newest;
    }

    private void LoadData()
    {
        //StreamReader reader = new StreamReader(path);
        //string json = reader.ReadToEnd();

        //dataProcessing = JsonUtility.FromJson<DataProcessing>(json);
        //SaveImportedData();

        try
        {
            using (var reader = new StreamReader(path))
            {
                string json = reader.ReadToEnd();
                dataProcessing = JsonUtility.FromJson<DataProcessing>(json);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"RecallJSONData: Failed to read '{path}': {ex.Message}");
            return;
        }

        SaveImportedData();
    }

    private void SaveImportedData()
    {
        //GetComponent<UserProfileData>().shotsFired = dataProcessing.shotsFired;
        //shotText.text = GetComponent<UserProfileData>().shotsFired.ToString();

        //GetComponent<UserProfileData>().deaths = dataProcessing.deaths;
        //GetComponent<UserProfileData>().successfulAttacks = dataProcessing.successfulAttacks;
        //GetComponent<UserProfileData>().generatorsFixed = dataProcessing.generatorsFixed;
        //GetComponent<UserProfileData>().timesAttacked = dataProcessing.timesAttacked;

        var profile = GetComponent<UserProfileData>();
        if (profile == null)
        {
            Debug.LogError("RecallJSONData: UserProfileData component missing on same GameObject.");
            return;
        }

        if (dataProcessing == null)
        {
            Debug.LogError("RecallJSONData: No data loaded to import.");
            return;
        }

        profile.deaths = dataProcessing.deaths;
        profile.successfulAttacks = dataProcessing.successfulAttacks;
        profile.generatorsFixed = dataProcessing.generatorsFixed;
        profile.timesAttacked = dataProcessing.timesAttacked;
    }
}
