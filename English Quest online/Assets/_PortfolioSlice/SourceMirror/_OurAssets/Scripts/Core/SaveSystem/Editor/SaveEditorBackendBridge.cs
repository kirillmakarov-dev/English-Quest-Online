using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudSave;
using UnityEngine;

#if UNITY_EDITOR
namespace EnglishKingdom.SaveSystem.Editor
{
    /// <summary>
    /// Editor-facing helpers to inspect and mutate local/cloud save payloads as raw JSON.
    /// </summary>
    internal static class SaveEditorBackendBridge
    {
        private static readonly JsonSerializerSettings s_jsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Populate
        };

        public static async UniTask<string> LoadLocalRawJsonAsync(SaveEditorDomainRegistry.DomainDescriptor domain)
        {
            EnsurePlayMode();

            string path = GetLocalPath(domain.Key);
            if (!File.Exists(path))
                return null;

            string json = await File.ReadAllTextAsync(path).AsUniTask(useCurrentSynchronizationContext: true);
            return PrettyPrintJson(json);
        }

        public static async UniTask SaveLocalRawJsonAsync(SaveEditorDomainRegistry.DomainDescriptor domain, string json)
        {
            EnsurePlayMode();

            string normalized = ValidateNormalizeAndDeserialize(domain, json);
            string path = GetLocalPath(domain.Key);
            await File.WriteAllTextAsync(path, normalized).AsUniTask(useCurrentSynchronizationContext: true);
        }

        public static UniTask DeleteLocalAsync(SaveEditorDomainRegistry.DomainDescriptor domain)
        {
            EnsurePlayMode();

            string path = GetLocalPath(domain.Key);
            if (File.Exists(path))
                File.Delete(path);

            return UniTask.CompletedTask;
        }

        public static async UniTask<string> LoadCloudRawJsonAsync(SaveEditorDomainRegistry.DomainDescriptor domain)
        {
            EnsurePlayMode();

            var keys = new HashSet<string> { domain.Key };

            try
            {
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys).AsUniTask();
                if (!result.TryGetValue(domain.Key, out var item))
                    return null;

                string json = item.Value.GetAs<string>();
                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return PrettyPrintJson(json);
            }
            catch (CloudSaveValidationException)
            {
                return null;
            }
        }

        public static async UniTask SaveCloudRawJsonAsync(SaveEditorDomainRegistry.DomainDescriptor domain, string json)
        {
            EnsurePlayMode();

            string normalized = ValidateNormalizeAndDeserialize(domain, json);
            var payload = new Dictionary<string, object> { { domain.Key, normalized } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload).AsUniTask();
        }

        public static UniTask DeleteCloudAsync(SaveEditorDomainRegistry.DomainDescriptor domain)
        {
            EnsurePlayMode();
            var options = new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions();
            return CloudSaveService.Instance.Data.Player.DeleteAsync(domain.Key, options).AsUniTask();
        }

        public static string BuildDefaultWrapperJson(SaveEditorDomainRegistry.DomainDescriptor domain)
        {
            var root = new JObject
            {
                ["version"] = domain.LatestVersion,
                ["savedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["data"] = JObject.FromObject(Activator.CreateInstance(domain.DataType))
            };

            return root.ToString(Formatting.Indented);
        }

        private static string ValidateNormalizeAndDeserialize(
            SaveEditorDomainRegistry.DomainDescriptor domain,
            string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                throw new InvalidOperationException("JSON is empty.");

            JObject root = JObject.Parse(rawJson);
            JToken dataToken = root["data"];
            if (dataToken == null)
                throw new InvalidOperationException("JSON must include a top-level 'data' object.");

            // Enforce shape and simulate runtime deserialize expectations.
            _ = dataToken.ToObject(domain.DataType, JsonSerializer.CreateDefault(s_jsonSettings));

            if (root["version"] == null)
                root["version"] = domain.LatestVersion;

            if (root["savedAt"] == null)
                root["savedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return root.ToString(Formatting.None);
        }

        private static string PrettyPrintJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;

            JToken token = JToken.Parse(json);
            return token.ToString(Formatting.Indented);
        }

        private static string GetLocalPath(string key)
        {
            string safeKey = key.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            return Path.Combine(Application.persistentDataPath, safeKey);
        }

        private static void EnsurePlayMode()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("Save Data Inspector operations are available only during Play Mode.");
        }
    }
}
#endif
