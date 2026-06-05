using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine.Networking;

namespace TheFoundation.Editor
{
    
    [InitializeOnLoad]
    public static class SceneUsagePoller
    {
        static float timer = 0f;
        const float refreshInterval = 500f;

        private static int counter;
        private static bool isFetching = false;
        public static HashSet<string> UsedScenes = new HashSet<string>();

        static SceneUsagePoller()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            timer += Time.deltaTime;

            if (!isFetching && timer > refreshInterval)
            {
                timer = 0;
                counter++;
                _ = FetchUsedScenes();
            }
        }

        static async Task FetchUsedScenes()
        {
            if (isFetching) return;
    
            isFetching = true;

            string url = "https://lexfolio.lex-soignant.be/api/get-used-scenes";

            var payload = new { user = System.Environment.UserName };
            string json = JsonConvert.SerializeObject(payload);

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var op = request.SendWebRequest();

            try
            {
                while (!op.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    //Debug.LogError("FetchUsedScenes Error: " + request.error);
                    return;
                }

                string jsonResponse = request.downloadHandler.text;

                SceneListResponse response;
                try
                {
                    response = JsonConvert.DeserializeObject<SceneListResponse>(jsonResponse);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("JSON Parse Error: " + e.Message);
                    return;
                }

                if (response?.data != null)
                {
                    UsedScenes.Clear();
                    foreach (var item in response.data)
                        if (!string.IsNullOrEmpty(item.scene))
                            UsedScenes.Add(item.scene);

                    //Debug.Log("SCENES RECEIVED : " + UsedScenes.Count);
                }
                else
                {
                    //Debug.Log("No data returned.");
                }
            }
            finally
            {
                request.Dispose();
                isFetching = false;
            }
        }


        class SceneListResponse
        {
            public bool success;
            public string message;
            public List<SceneItem> data;
            public object errors;
        }

        class SceneItem
        {
            public string scene;
        }

    }

}
