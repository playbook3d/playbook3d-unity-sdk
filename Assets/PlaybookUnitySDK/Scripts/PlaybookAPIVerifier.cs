using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Windows;

namespace PlaybookUnitySDK.Scripts
{
    public class PlaybookAPIVerifier : MonoBehaviour
    {
        [SerializeField]
        private string playbookAccountAPIKey;

        private static Team[] _teams;
        private static Workflow[] _workflows;
        private static UploadURLs _uploadUrls;
        private static UploadURLs _uploadZipUrls;

        public static int CurrTeamIndex { get; set; }
        public static int CurrWorkflowIndex { get; set; }

        private const string TeamsURL = "https://dev-accounts.playbook3d.com/teams";
        private const string WorkflowsURL = "https://dev-accounts.playbook3d.com/workflows";

        // TODO: Hide these URLs
        private const string BaseURL = "";
        private const string TokenEndpoint = "";
        private const string UploadEndpoint = "";
        private const string DownloadEndpoint = "";
        private const string XapiKey = "";

        private async void Start()
        {
            await SetAPIProperties();
        }

        private async Task SetAPIProperties()
        {
            AccessToken accessToken = await GetResponseAs<AccessToken>(
                $"{BaseURL}{TokenEndpoint}{playbookAccountAPIKey}"
            );

            Dictionary<string, string> headers =
                new()
                {
                    { "Authorization", $"Bearer {accessToken.access_token}" },
                    { "X-API-KEY", XapiKey },
                };

            _teams = await GetResponseWithWrapper<Team>(TeamsURL, headers);
            _workflows = await GetResponseWithWrapper<Workflow>(WorkflowsURL, headers);

            _uploadUrls = await GetResponseAs<UploadURLs>(
                $"{BaseURL}{UploadEndpoint}",
                new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {accessToken.access_token}" },
                }
            );

            await GetImageResults(accessToken);
        }

        private static async Task<T> GetResponseAs<T>(
            string url,
            Dictionary<string, string> headers = null
        )
        {
            using HttpClient client = new();

            try
            {
                if (headers != null)
                {
                    foreach (KeyValuePair<string, string> header in headers)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation(
                            header.Key,
                            header.Value
                        );
                    }
                }

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Debug.Log($"<color=red>{responseBody}</color>");

                T data = JsonUtility.FromJson<T>(responseBody);
                return data;
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Could not get response: {e}");
                return default;
            }
        }

        private static async Task<T[]> GetResponseWithWrapper<T>(
            string url,
            Dictionary<string, string> headers
        )
        {
            using HttpClient client = new();

            try
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                }

                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Debug.Log($"<color=cyan>{responseBody}</color>");

                T[] data = FromJson<T>("{\"items\":" + responseBody + "}");

                return data;
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Could not get response: {e}");
                return null;
            }
        }

        private static async Task UploadFile(string url, string imagePath)
        {
            using HttpClient client = new();

            try
            {
                byte[] pngData = File.ReadAllBytes(imagePath);
                ByteArrayContent content = new(pngData);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "image/png"
                );

                HttpResponseMessage response = await client.PutAsync(new Uri(url), content);
                response.EnsureSuccessStatusCode();

                string responseString = await response.Content.ReadAsStringAsync();
                Debug.Log($"<color=yellow>{responseString}</color>");
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Request error: {e}");
            }
        }

        public static string[] GetTeams()
        {
            if (_teams == null || _teams.Length == 0)
            {
                return new[] { "None" };
            }

            return _teams.Select(team => team.name).ToArray();
        }

        public static string[] GetWorkflows()
        {
            if (
                _teams == null
                || _teams.Length == 0
                || _workflows == null
                || _workflows.Length == 0
            )
            {
                return new[] { "None" };
            }

            var currTeamId = _teams[CurrTeamIndex].id;
            return _workflows
                .Where(workflow => workflow.team_id == currTeamId)
                .Select(workflow => workflow.name)
                .ToArray();
        }

        public static async Task UploadImageFile(PlaybookSDK.RenderPass pass, string path)
        {
            await UploadFile(_uploadUrls.GetUrl(pass), path);
        }

        private static async Task GetImageResults(AccessToken accessToken)
        {
            using HttpClient client = new();

            try
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(
                    "Authorization",
                    $"Bearer {accessToken.access_token}"
                );

                HttpResponseMessage response = await client.GetAsync(
                    $"{BaseURL}{DownloadEndpoint}"
                );
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();
                Debug.Log($"<color=yellow>{result}</color>");
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Could not get response: {e}");
            }
        }

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
            public string mask;
            public string depth;
            public string outline;

            public string GetUrl(PlaybookSDK.RenderPass pass)
            {
                return pass switch
                {
                    PlaybookSDK.RenderPass.Beauty => beauty,
                    PlaybookSDK.RenderPass.Mask => mask,
                    PlaybookSDK.RenderPass.Depth => depth,
                    PlaybookSDK.RenderPass.Outline => outline,
                    _ => beauty,
                };
            }
        }

        [Serializable]
        private class Wrapper<T>
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
