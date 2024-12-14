using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public int CurrTeamIndex { get; set; }
        public int CurrWorkflowIndex { get; set; }

        public event Action<string> ReceivedUploadUrl;

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

        #region Initialization

        private IEnumerator Start()
        {
            yield return StartCoroutine(InitializeNetworkProperties());

            ConnectWebSocket(PlaybookServerURL);
        }

        private IEnumerator InitializeNetworkProperties()
        {
            // Get the user's access token
            string accessTokenUrl = $"{AccountBaseURL}{TokenEndpoint}{PlaybookAccountAPIKey}";
            yield return StartCoroutine(
                GetResponseAs<AccessToken>(
                    accessTokenUrl,
                    accessToken => _accessToken = accessToken.access_token
                )
            );

            // Get the user's teams + workflows
            Dictionary<string, string> headers =
                new() { { "Authorization", $"Bearer {_accessToken}" }, { "X-API-KEY", XapiKey } };
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

            // Set request headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.SetRequestHeader("X-API-KEY", XapiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                PlaybookLogger.Log(
                    "Success response from running workflow.",
                    DebugLevel.Default,
                    Color.magenta
                );
            }
            else
            {
                PlaybookLogger.LogError($"Could not run workflow: {request.error}");
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
                        { "token", XapiKey },
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

            string runStatus = jsonArray[0]["run_status"]["type"].Value<string>();

            PlaybookLogger.Log(runStatus, DebugLevel.All);

            // Image was successfully rendered
            if (string.Equals(runStatus, "executed"))
            {
                string imageUrl = jsonArray[0]["run_status"]["url_image"].Value<string>();

                PlaybookLogger.Log(
                    "Got a result! Copy the result image URL from the PlaybookSDK component.",
                    DebugLevel.Default,
                    Color.green
                );
                PlaybookLogger.Log($"Result image URL: {imageUrl}", DebugLevel.All);

                ReceivedUploadUrl?.Invoke(imageUrl);
            }
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

        #endregion
    }
}
