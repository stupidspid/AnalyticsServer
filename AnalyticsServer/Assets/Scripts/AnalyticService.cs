using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Cysharp.Threading.Tasks;

public class AnalyticService : MonoBehaviour
{
    [SerializeField] private float coolDown;
    [SerializeField] private string serverUrl;
    
    private readonly string JSON_PATH = Path.Combine(Application.persistentDataPath, "sendingEvents.json");
    private List<EventData> sendingEvents = new();
    private bool isCooldownActive;

    private async void Start()
    {
        await LoadEvents();
    }

    public async UniTask TrackEvent(string type, string data)
    {
        EventData eventData = new EventData(type, data);
        sendingEvents.Add(eventData);
        SaveEvents();
        if (!isCooldownActive)
        {
            await UploadPostEvents();
        }
    }

    private async UniTask UploadPostEvents()
    {
        if(sendingEvents.Count <= 0) return;

        isCooldownActive = true;
        EventData currentEvent = sendingEvents[0];
        string json = JsonUtility.ToJson(currentEvent);
        
        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, json))
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bytes);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                sendingEvents.RemoveAt(0);
            }
        }

        UniTask.Delay(TimeSpan.FromSeconds(coolDown));
        isCooldownActive = false;
    }

    private void SaveEvents()
    {
        string json = JsonUtility.ToJson(sendingEvents);
        File.WriteAllText(JSON_PATH, json);
    }
    
    private async UniTask LoadEvents()
    {
        if (File.Exists(JSON_PATH))
        {
            string json = File.ReadAllText(JSON_PATH);
            List<EventData> eventList = JsonUtility.FromJson<List<EventData>>(json);
            sendingEvents = eventList;
        }

        while (sendingEvents.Count > 0)
        {
            if (!isCooldownActive)
            {
                await UploadPostEvents();
            }
        }
    }
    
    private class EventData
    {
        private string _type;
        private string _data;

        public EventData(string type, string data)
        {
            _type = type;
            _data = data;
        }
    }
}
