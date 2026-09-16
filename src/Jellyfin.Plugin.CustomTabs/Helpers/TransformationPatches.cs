using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.CustomTabs.Attributes;
using Jellyfin.Plugin.CustomTabs.Configuration;
using Jellyfin.Plugin.CustomTabs.Model;

namespace Jellyfin.Plugin.CustomTabs.Helpers
{
    public static class TransformationPatches
    {
        private static bool IsJf12() =>
            (JellyfinVersionAttribute.GetVersion() ?? "").StartsWith("12.");

        public static string IndexHtml(PatchRequestPayload payload)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(CustomTabsPlugin).Namespace}.Inject.addCustomTabs.js")!;
            using TextReader reader = new StreamReader(stream);

            string regex = Regex.Replace(payload.Contents!, "(</body>)", $"<script defer>{reader.ReadToEnd()}</script>$1");

            return regex;
        }

        public static string HomeHtmlChunk(PatchRequestPayload payload)
        {
            string buffer = payload.Contents!;
            {
                Stream stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"{typeof(CustomTabsPlugin).Namespace}.Inject.tabTemplate.html")!;
                using TextReader reader = new StreamReader(stream);

                string tabTemplate = reader.ReadToEnd();
                string finalReplacement = "";
                for (int i = 0; i < CustomTabsPlugin.Instance.Configuration.Tabs.Length; ++i)
                {
                    TabConfig tabConfig = CustomTabsPlugin.Instance.Configuration.Tabs[i];

                    finalReplacement += tabTemplate
                        .Replace("{{tab_id}}", $"customTab_{i}")
                        .Replace("{{tab_index}}", $"{i + 2}")
                        .Replace("{{tab_content}}", tabConfig.ContentHtml);
                }

                finalReplacement = finalReplacement
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Replace("  ", " ")
                    .Replace("'undefined'", "\\'undefined\\'");

                // JF 12 renamed favoritesTab → homeTab and changed data-index to 0
                string anchorPattern = IsJf12()
                    ? @"(id=""homeTab"" data-index=""0"">)"
                    : @"(id=""favoritesTab"" data-index=""1""> <div class=""sections""></div> </div>)";

                buffer = Regex.Replace(buffer, anchorPattern, $"$1{finalReplacement}");
            }

            return buffer;
        }

        public static string MainBundle(PatchRequestPayload payload)
        {
            string replacementText =
                "window.PlaybackManager=this.playbackManager;console.log(\"PlaybackManager is now globally available:\",window.PlaybackManager);";

            string regex = Regex.Replace(payload.Contents!, @"(this\.playbackManager=e,)", $"$1{replacementText}");

            return regex;
        }
    }
}
