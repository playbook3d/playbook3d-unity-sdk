using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using UnityEngine;
using UnityEngine.Networking;
using File = UnityEngine.Windows.File;

namespace PlaybookUnitySDK.Runtime
{
    /// <summary>
    /// This class is responsible for sending and receiving information from
    /// Playbook's API.
    /// </summary>
    public class PlaybookNetwork : MonoBehaviour
    {
        public string PlaybookAccountAPIKey { get; set; }

        private string _accessToken;
        private Team[] _teams;
        private Workflow[] _workflows;
        private UploadUrls _uploadUrls;

        private SocketIOUnity _socket;

        private List<string> _activeRunIds = new();

        public int CurrTeamIndex { get; set; }
        public int CurrWorkflowIndex { get; set; }

        public event Action<string> ReceivedUploadUrl;
        public event Action FinishedFileUpload;

        private string _playbookServerURL;
        private string _accountBaseURL;
        private string _apiBaseURL;
        private string _xApiKey;
        private const string TokenEndpoint = "/token-wrapper/get-tokens/";
        private const string UploadEndpoint = "/upload-assets/get-upload-urls/";
        private const string DownloadEndpoint = "/upload-assets/get-download-urls/";
        private const string RunWorkflowEndpoint = "/run_workflow/";
        private const string TeamsURL = "/teams";
        private const string WorkflowsURL = "/workflows";

        #region Initialization

        private void SetPlaybookApiUrls(PlaybookUrls urls){
            _xApiKey = urls.X_API_KEY;
            _apiBaseURL = urls.API_BASE_URL;
            _accountBaseURL = urls.ACCOUNTS_BASE_URL;
            _playbookServerURL = urls.WEBSOCKET_BASE_URL;
        }

        private IEnumerator Start()
        {
            yield return StartCoroutine(InitializeNetworkProperties());

            ConnectWebSocket(_playbookServerURL);
        }

        private IEnumerator InitializeNetworkProperties()
        {
            // Get the user's access token
            string accessTokenUrl = $"https://dev-accounts.playbook3d.com/token-wrapper/get-tokens/{PlaybookAccountAPIKey}";
            yield return StartCoroutine(
                GetResponseAs<AccessToken>(
                    accessTokenUrl,
                    accessToken => _accessToken = accessToken.access_token
                )
            );

            yield return StartCoroutine(
                GetPlaybookUrls(
                    _accessToken,
                    response => {
                            SetPlaybookApiUrls(response);
                        }
                    )
            );


            // Get the user's teams + workflows
            Dictionary<string, string> headers =
                new() { { "Authorization", $"Bearer {_accessToken}" }, { "X-API-KEY", _xApiKey } };
            yield return StartCoroutine(
                GetResponseWithWrapper<Team>($"{_accountBaseURL}{TeamsURL}", teams => _teams = teams, headers)
            );
            yield return StartCoroutine(
                GetResponseWithWrapper<Workflow>(
                    $"{_accountBaseURL}{WorkflowsURL}",
                    workflows => _workflows = workflows,
                    headers
                )
            );

            // Get the user's upload URLs
            string uploadUrl = $"{_accountBaseURL}{UploadEndpoint}";
            yield return StartCoroutine(
                GetResponseAs<UploadUrls>(
                    uploadUrl,
                    uploadUrls => _uploadUrls = uploadUrls,
                    new Dictionary<string, string> { { "Authorization", $"Bearer {_accessToken}" } }
                )
            );
        }

        #endregion

        private void OnDestroy()
        {
            if (_socket != null)
            {
                _socket.Disconnect();
                _socket.Dispose();
            }
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
                PlaybookLogger.LogError($"Could not get response {request.error}");
            }
            else
            {
                callback(JsonUtility.FromJson<T>(request.downloadHandler.text));
            }
        }

        private static IEnumerator GetPlaybookUrls(string token, Action<PlaybookUrls> callback){
            using UnityWebRequest request = UnityWebRequest.Get("https://dev-api.playbook3d.com/get-secrets");
            request.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                PlaybookLogger.LogError($"Could not get response {request.error}");
            }
            else
            {
                PlaybookUrls response = JsonUtility.FromJson<PlaybookUrls>(request.downloadHandler.text);
                callback(response);
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
                PlaybookLogger.LogError($"Could not get response {request.error}");
            }
            else
            {
                callback(FromJson<T>($"{{\"items\":{request.downloadHandler.text}}}"));
            }
        }

        #endregion

        /// <summary>
        /// Run the currently selected render workflow.
        /// </summary>
        private IEnumerator RunWorkflow(string accessToken)
        {
            string url = $"{_apiBaseURL}{RunWorkflowEndpoint}{GetCurrentSelectedWorkflow().team_id}";

            Dictionary<string, object> inputs = new();
            
            OverrideNodeInputs(ref inputs, "4", "Dinosaur in field");

            RunWorkflowProperties data =
                new()
                {
                    id = GetCurrentSelectedWorkflow().id,
                    origin = "2",
                    inputs = inputs,
                };

            string jsonBody = JsonConvert.SerializeObject(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using UnityWebRequest request = new(url, "POST");

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            // Set request headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("X-API-KEY", _xApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                JObject requestData = JObject.Parse(request.downloadHandler.text);

                if (requestData["run_id"] == null)
                {
                    PlaybookLogger.LogError(
                        "Received an unexpected response from running the workflow."
                    );
                }
                else
                {
                    PlaybookLogger.Log(
                        "Success response from running the workflow.",
                        DebugLevel.Default,
                        Color.magenta
                    );
                    PlaybookLogger.Log(
                        "Starting up GPUs...",
                        DebugLevel.Default,
                        Color.magenta
                    );

                    _activeRunIds.Add(requestData["run_id"].Value<string>());
                }
            }
            else
            {
                PlaybookLogger.LogError($"Could not run workflow: {request.error}");
            }
        }

        /// <summary>
        /// Override the inputs of the node with the given node ID.
        /// </summary>
        private void OverrideNodeInputs(
            ref Dictionary<string, object> inputs,
            string nodeId,
            string value,
            string triggerWords = ""
        )
        {
            Dictionary<string, string> overrideValues = new()
            {
                { "default_value", value }
            };

            if (!string.IsNullOrEmpty(triggerWords))
            {
                overrideValues.Add("trigger_words", triggerWords);
            }

            inputs[nodeId] = overrideValues;
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
                CurrWorkflowIndex
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
            string rendersFolderPath = PlaybookFileUtilities.GetRendersFolderPath();

            foreach (
                PlaybookCapturePasses.RenderPass renderPass in Enum.GetValues(
                        typeof(PlaybookCapturePasses.RenderPass)
                    )
                    .Cast<PlaybookCapturePasses.RenderPass>()
            )
            {
                string fileName = $"{renderPass}Pass.{extension}";
                string filePath = $"{rendersFolderPath}/{fileName}";
                string url = _uploadUrls.GetUrl(renderPass, !isImage);
                string contentType = isImage ? "image/png" : "application/zip";

                yield return StartCoroutine(UploadFile(url, filePath, contentType));
            }

            FinishedFileUpload?.Invoke();

            StartCoroutine(RunWorkflow(_accessToken));
        }

        /// <summary>
        /// Upload the file in the given path to the given URL.
        /// </summary>
        private static IEnumerator UploadFile(string url, string filePath, string contentType)
        {
            if (!File.Exists(filePath))
            {
                PlaybookLogger.LogError($"{filePath} does not exist.");
                yield return null;
            }

            byte[] pngData = File.ReadAllBytes(filePath);

            using UnityWebRequest request = UnityWebRequest.Put(url, pngData);
            request.SetRequestHeader("Content-Type", contentType);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                PlaybookLogger.LogError(request.error);
            }
            else 
            {
                PlaybookLogger.Log($"Successfully uploaded {filePath}", DebugLevel.All);
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

        /// <summary>
        /// Connect to the given WebSocket.
        /// </summary>
        private void ConnectWebSocket(string uri)
        {
            Uri url = new(uri);

            _socket = new SocketIOUnity(
                url,
                new SocketIOOptions
                {
                    Query = new Dictionary<string, string>
                    {
                        { "token", _xApiKey },
                        { "auth_token", _accessToken },
                    },
                    Reconnection = true,
                    ReconnectionAttempts = 3,
                    Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                }
            );

            _socket.OnConnected += OnSocketConnected;
            _socket.OnDisconnected += OnSocketDisconnected;

            _socket.On("run_info", ReceiveRunInfoResponse);

            _socket.Connect();
        }

        private static void OnSocketDisconnected(object sender, string e)
        {
            PlaybookLogger.Log("Disconnected from socket.", DebugLevel.All, Color.red);
        }

        private static void OnSocketConnected(object sender, EventArgs e)
        {
            PlaybookLogger.Log("Connected to socket.", DebugLevel.All, Color.green);
        }

        /// <summary>
        /// Receive the response given by the WebSockets "run_info" event.
        /// </summary>
        private void ReceiveRunInfoResponse(SocketIOResponse response)
        {
            JArray jsonArray = JArray.Parse(response.ToString());

            if (jsonArray[0]["run_id"] == null)
            {
                PlaybookLogger.LogError("Received an unexpected response from run_info.");
                return;
            }

            // The received workflow was not ran through the Unity SDK. Ignore
            if (!_activeRunIds.Contains(jsonArray[0]["run_id"].Value<string>()))
                return;

            if (jsonArray[0]["run_status"]?["type"] == null)
            {
                PlaybookLogger.LogError("Received an unexpected response from run_info.");
                return;
            }

            string runStatus = jsonArray[0]["run_status"]["type"].Value<string>();

            PlaybookLogger.Log(runStatus, DebugLevel.All, Color.white);

            if (!string.Equals(runStatus, "executed"))
                return;

            if (jsonArray[0]["run_status"]["url_image"] == null)
            {
                PlaybookLogger.LogError("Received an unexpected response from run_info.");
                return;
            }

            // Image was successfully rendered
            string imageUrl = jsonArray[0]["run_status"]["url_image"].Value<string>();

            PlaybookLogger.Log(
                "Got a result! Copy the result image URL from the PlaybookSDK component.",
                DebugLevel.Default,
                Color.green
            );
            PlaybookLogger.Log($"Result image URL: {imageUrl}", DebugLevel.All, Color.green);

            ReceivedUploadUrl?.Invoke(imageUrl);
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
        private struct UploadUrls
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
                        _ => throw new ArgumentOutOfRangeException(nameof(pass), pass, null),
                    };
                }

                return pass switch
                {
                    PlaybookCapturePasses.RenderPass.Beauty => beauty_zip,
                    PlaybookCapturePasses.RenderPass.Mask => mask_zip,
                    PlaybookCapturePasses.RenderPass.Depth => depth_zip,
                    PlaybookCapturePasses.RenderPass.Outline => outline_zip,
                    _ => throw new ArgumentOutOfRangeException(nameof(pass), pass, null),
                };
            }
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

        [Serializable]
        private struct PlaybookUrls
        {
            public string ACCOUNTS_BASE_URL;
            public string API_BASE_URL;
            public string X_API_KEY;
            public string WEBSOCKET_BASE_URL;
        }

        #endregion
    }
}

