using UnityEngine;
using System.IO;
using System.Collections;

public class SaveJSONData : MonoBehaviour
{
    private GameObject gc;
    private DataProcessing dataProcessing;
    private string path = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gc = GameObject.Find("GameController");
        SetPath();
    }

    private void SetPath()
    {
        path = Application.persistentDataPath + "/" + System.DateTime.UtcNow.ToLocalTime().
            ToString("M-d-yy-HH-mm") + ".json";
        //path = Application.persistentDataPath + "/gameSaveData.json";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CreateDataToSave();
            SaveData();
            StartCoroutine(ExitPause());
        }
    }

    IEnumerator ExitPause()
    {
        yield return new WaitForSeconds(2f);
        Application.Quit();
    }

    private void CreateDataToSave()
    {
        dataProcessing = new DataProcessing(
            GetComponent<UserProfileData>().deaths,
            GetComponent<UserProfileData>().successfulAttacks,
            GetComponent<UserProfileData>().generatorsFixed,
            GetComponent<UserProfileData>().timesAttacked
            );
    }

    private void SaveData()
    {
        string json = JsonUtility.ToJson(dataProcessing);
        StreamWriter writer = new StreamWriter(path);
        writer.Write(json);
        writer.Close();
    }
}
