using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using File = UnityEngine.Windows.File;

namespace PlaybookUnitySDK.Scripts
{
    public class PlaybookNetwork : MonoBehaviour
    {
        public string PlaybookAccountAPIKey { get; set; }

        private AccessToken _accessToken;
        private Team[] _teams;
        private Workflow[] _workflows;
        private UploadURLs _uploadUrls;

        public static int CurrTeamIndex { get; set; }
        public static int CurrWorkflowIndex { get; set; }

        private const string TeamsURL = "https://dev-accounts.playbook3d.com/teams";
        private const string WorkflowsURL = "https://dev-accounts.playbook3d.com/workflows";

        // TODO: Hide these URLs
        private const string PlaybookServerURL = "";
        private const string AccountBaseURL = "";
        private const string ApiBaseURL = "";
        private const string TokenEndpoint = "";
        private const string UploadEndpoint = "";
        private const string DownloadEndpoint = "";
        private const string RunWorkflowEndpoint = "";
        private const string XapiKey = "";

        private ClientWebSocket _webSocket;

        private IEnumerator Start()
        {
            yield return StartCoroutine(InitializeNetworkProperties());

            Task connectTask = ConnectWebSocket(PlaybookServerURL);
            yield return new WaitUntil(() => connectTask.IsCompleted);

            Task receiveTask = ReceiveMessagesFromWebSocket();
        }

        private async void OnDestroy()
        {
            await CloseWebSocket();
        }

        private IEnumerator InitializeNetworkProperties()
        {
            string accessTokenUrl = $"{AccountBaseURL}{TokenEndpoint}{PlaybookAccountAPIKey}";
            yield return StartCoroutine(
                GetResponseAs<AccessToken>(
                    accessTokenUrl,
                    accessToken => _accessToken = accessToken
                )
            );

            // Get the user's teams + workflows
            Dictionary<string, string> headers =
                new()
                {
                    { "Authorization", $"Bearer {_accessToken.access_token}" },
                    { "X-API-KEY", XapiKey },
                };
            yield return StartCoroutine(
                GetResponseWithWrapper<Team>(TeamsURL, teams => _teams = teams, headers)
            );
            yield return StartCoroutine(
                GetResponseWithWrapper<Workflow>(
                    WorkflowsURL,
                    workflows => _workflows = workflows,
                    headers
                )
            );

            // Get the user's upload URLs
            string uploadUrl = $"{AccountBaseURL}{UploadEndpoint}";
            yield return StartCoroutine(
                GetResponseAs<UploadURLs>(
                    uploadUrl,
                    uploadUrls => _uploadUrls = uploadUrls,
                    new Dictionary<string, string>
                    {
                        { "Authorization", $"Bearer {_accessToken.access_token}" },
                    }
                )
            );

            // string resultUrl = $"{AccountBaseURL}{DownloadEndpoint}";
            // yield return StartCoroutine(
            //     GetResponseAs<RenderResult>(
            //         resultUrl,
            //         result => Debug.Log(result),
            //         new Dictionary<string, string>
            //         {
            //             { "Authorization", $"Bearer {_accessToken.access_token}" },
            //         }
            //     )
            // );
        }

        #region Get Response Methods

        private static IEnumerator GetResponseAs<T>(
            string url,
            Action<T> callback,
            Dictionary<string, string> headers = null
        )
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            // Add headers to the request, if any
            if (headers != null)
            {
                foreach ((string header, string value) in headers)
                {
                    request.SetRequestHeader(header, value);
                }
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
            else
            {
                callback(JsonUtility.FromJson<T>(request.downloadHandler.text));
            }
        }

        private static IEnumerator GetResponseWithWrapper<T>(
            string url,
            Action<T[]> callback,
            Dictionary<string, string> headers
        )
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            // Add headers to the request
            foreach ((string header, string value) in headers)
            {
                request.SetRequestHeader(header, value);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
            }
            else
            {
                callback(FromJson<T>($"{{\"items\":{request.downloadHandler.text}}}"));
            }
        }

        #endregion

        private IEnumerator RunWorkflow(string accessToken)
        {
            string url = $"{ApiBaseURL}{RunWorkflowEndpoint}{GetCurrentSelectedWorkflow().team_id}";

            RunWorkflowProperties data =
                new()
                {
                    id = GetCurrentSelectedWorkflow().id,
                    origin = "1",
                    inputs = new { },
                };
            string jsonBody = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using UnityWebRequest request = new(url, "POST");

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("X-API-KEY", XapiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=magenta>Success response from running workflow.</color>");
            }
            else
            {
                Debug.LogError($"Could not get response: {request.error}");
            }
        }

        private Workflow GetCurrentSelectedWorkflow()
        {
            if (
                // No teams exist
                _teams == null
                || _teams.Length == 0
                // No workflows exist
                || _workflows == null
                || _workflows.Length == 0
            )
            {
                return default;
            }

            string currTeamId = _teams[CurrTeamIndex].id;
            return _workflows.Where(workflow => workflow.team_id == currTeamId).ToArray()[
                CurrTeamIndex
            ];
        }

        #region Upload File Methods

        public void UploadImageFiles()
        {
            StartCoroutine(UploadRenderPassFiles(true, "png"));
        }

        public void UploadZipFiles()
        {
            StartCoroutine(UploadRenderPassFiles(false, "zip"));
        }

        private IEnumerator UploadRenderPassFiles(bool isImage, string extension)
        {
            string rendersFolderPath = PlaybookFileUtilities.GetRendersFolderPath(this);

            foreach (
                PlaybookCapturePasses.RenderPass renderPass in Enum.GetValues(
                        typeof(PlaybookCapturePasses.RenderPass)
                    )
                    .Cast<PlaybookCapturePasses.RenderPass>()
            )
            {
                string fileName = $"{renderPass}Pass.{extension}";
                string filePath = Path.Combine(rendersFolderPath, fileName);
                string url = _uploadUrls.GetUrl(renderPass, !isImage);
                string contentType = isImage ? "image/png" : "application/zip";

                yield return StartCoroutine(UploadFile(url, filePath, contentType));
            }

            StartCoroutine(RunWorkflow(_accessToken.access_token));
        }

        /// <summary>
        /// Upload the file in the given path to the given URL.
        /// </summary>
        private static IEnumerator UploadFile(string url, string filePath, string contentType)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"{filePath} does not exist.");
                yield return null;
            }

            byte[] pngData = File.ReadAllBytes(filePath);

            using UnityWebRequest request = UnityWebRequest.Put(url, pngData);
            request.SetRequestHeader("Content-Type", contentType);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"<color=white>Successfully uploaded image {filePath}</color>");
            }
            else
            {
                Debug.LogError(request.error);
            }
        }

        #endregion

        #region PlaybookNetworkEditor Helper Methods

        /// <summary>
        /// Get the user's teams.
        /// </summary>
        public string[] GetTeams()
        {
            if (_teams == null || _teams.Length == 0)
            {
                return new[] { "None" };
            }

            return _teams.Select(team => team.name).ToArray();
        }

        /// <summary>
        /// Get the workflows of the currently selected team.
        /// </summary>
        public string[] GetWorkflows()
        {
            if (
                // No teams exist
                _teams == null
                || _teams.Length == 0
                // No workflows exist
                || _workflows == null
                || _workflows.Length == 0
            )
            {
                return new[] { "None" };
            }

            string currTeamId = _teams[CurrTeamIndex].id;
            return _workflows
                .Where(workflow => workflow.team_id == currTeamId)
                .Select(workflow => workflow.name)
                .ToArray();
        }

        #endregion

        #region Websocket

        private async Task ConnectWebSocket(string uri)
        {
            var url = new Uri($"{uri}/?token={XapiKey}&auth_token={_accessToken.access_token}");
            Debug.Log($"<color=red>{url}</color>");
            _webSocket = new ClientWebSocket();

            _webSocket.Options.SetRequestHeader("token", XapiKey);
            _webSocket.Options.SetRequestHeader("auth_token", _accessToken.access_token);
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            try
            {
                await _webSocket.ConnectAsync(url, CancellationToken.None);
                Debug.Log("WebSocket connected!");
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSocket connection failed: {e.Message}");
            }
        }

        private async Task ReceiveMessagesFromWebSocket()
        {
            byte[] buffer = new byte[1024 * 4];
            while (_webSocket?.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );

                if (result.MessageType != WebSocketMessageType.Close)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log($"Message received: {receivedMessage}");
                }
            }
        }

        private async Task CloseWebSocket()
        {
            if (_webSocket == null)
                return;

            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing connection",
                    CancellationToken.None
                );
            }

            _webSocket.Dispose();
            _webSocket = null;
        }

        #endregion

        #region Network Helpers

        [Serializable]
        private struct AccessToken
        {
            public string access_token;
        }

        [Serializable]
        private struct Team
        {
            public string id;
            public string name;
        }

        [Serializable]
        private struct Workflow
        {
            public string id;
            public string team_id;
            public string name;
        }

        [Serializable]
        private struct UploadURLs
        {
            public string beauty;
            public string beauty_zip;
            public string mask;
            public string mask_zip;
            public string depth;
            public string depth_zip;
            public string outline;
            public string outline_zip;

            public string GetUrl(PlaybookCapturePasses.RenderPass pass, bool isZip = false)
            {
                if (!isZip)
                {
                    return pass switch
                    {
                        PlaybookCapturePasses.RenderPass.Beauty => beauty,
                        PlaybookCapturePasses.RenderPass.Mask => mask,
                        PlaybookCapturePasses.RenderPass.Depth => depth,
                        PlaybookCapturePasses.RenderPass.Outline => outline,
                    };
                }

                return pass switch
                {
                    PlaybookCapturePasses.RenderPass.Beauty => beauty_zip,
                    PlaybookCapturePasses.RenderPass.Mask => mask_zip,
                    PlaybookCapturePasses.RenderPass.Depth => depth_zip,
                    PlaybookCapturePasses.RenderPass.Outline => outline_zip,
                };
            }
        }

        [Serializable]
        private struct RenderResult
        {
            public string render_result;
        }

        [Serializable]
        private struct RunWorkflowProperties
        {
            public string id;
            public string origin;
            public object inputs;
        }

        [Serializable]
        private struct Wrapper<T>
        {
            public T[] items;
        }

        private static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.items;
        }

        #endregion
    }
}
