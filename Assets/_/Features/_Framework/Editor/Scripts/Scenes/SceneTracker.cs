using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;

namespace TheFoundation.Editor
{
    [InitializeOnLoad]
    public static class SceneTracker
    {
        static SceneTracker()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static async void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            Debug.Log("Scene ouverte : " + scene.name);
            await SendSceneInfoToServer(scene.name);
        }

        private static async Task SendSceneInfoToServer(string sceneName)
        {
            //string url = "http://localhost:8001/api/patch-used-scene";
            string url = "https://lexfolio.lex-soignant.be/api/patch-used-scene";
            

            var payload = new
            {
                scene = sceneName,
                user = System.Environment.UserName,
                time = System.DateTime.UtcNow.ToString("o")
            };

            string json = JsonConvert.SerializeObject(payload);

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var op = request.SendWebRequest();

            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log(jsonResponse);
                ServerResponse response = JsonConvert.DeserializeObject<ServerResponse>(jsonResponse);

                if (response.data != null)
                {
                    Debug.Log("Data count: " + response.data.Count);
                    foreach (UsedScene item in response.data)
                    {
                        Debug.Log("Data item: " + item.scene);
                        Debug.Log("Data item: " + item.user);
                    }
                }
                else
                {
                    Debug.Log("No data returned.");
                }
            }
            else
            {
                Debug.LogError("Erreur serveur: " + request.error);
            }
        }
    }

    [System.Serializable]
    public class UsedScene
    {
        public string scene;
        public string user;
        public string time;
    }

    [System.Serializable]
    public class ServerResponse
    {
        public bool success;
        public string message;
        public List<UsedScene> data;
        public string errors;
    }

}
