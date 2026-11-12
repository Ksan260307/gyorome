using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gyorome.EditorTools
{
    /// <summary>カタログから REACTIONS.md（反応の一覧）を書き出す。セットアップと同時に更新される。</summary>
    public static class ReactionDocs
    {
        public static string DefaultPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "REACTIONS.md"));

        static readonly string[] CategoryOrder = { "タップ", "スワイプ・なでる", "長押し", "2 本指", "ふる・かたむける", "くみあわせ", "ひとりでに" };

        [MenuItem("Gyorome/Export Reaction List", priority = 30)]
        public static void ExportFromMenu()
        {
            Export(DefaultPath);
            Debug.Log("[ReactionDocs] " + DefaultPath);
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            var all = ReactionCatalog.All;
            sb.AppendLine("# はんのう一覧");
            sb.AppendLine();
            sb.AppendLine($"ぎょろめの反応は **{all.Count} しゅ**。このファイルは `Gyorome/Setup Project`（または `Gyorome/Export Reaction List`）がカタログ（`unity/Assets/Scripts/Reactions/ReactionCatalog.*.cs`）から作ります。手で直さないでください。");
            sb.AppendLine();
            sb.AppendLine("- **名前**: 画面に出るオノマトペ。**きっかけ**: 図鑑に出るヒント（まだ見つけていなくても見える）");
            sb.AppendLine("- **優先**: 同じ操作に複数の反応があるとき、数字の大きいものが先に選ばれる。同じ数字の中からは、まだ見つけていないものが出やすい重みつきの抽選");
            sb.AppendLine("- 場所: 目 = どちらかの目、ひとみ = 目のまん中、上 / 下 = 目の上の方 / 下の方、外 = 目の外、両目 = 2 本の指がべつべつの目、どこでも = 画面のどこでも・端末");
            sb.AppendLine();
            var cats = new List<string>(CategoryOrder);
            foreach (var r in all) if (!cats.Contains(r.Category)) cats.Add(r.Category);
            int n = 0;
            foreach (var cat in cats)
            {
                int count = 0;
                foreach (var r in all) if (r.Category == cat) count++;
                if (count == 0) continue;
                sb.AppendLine($"## {cat}（{count}）");
                sb.AppendLine();
                sb.AppendLine("| # | id | 名前 | きっかけ | 操作 | 場所 | 優先 |");
                sb.AppendLine("| ---: | --- | --- | --- | --- | --- | ---: |");
                foreach (var r in all)
                {
                    if (r.Category != cat) continue;
                    n++;
                    sb.AppendLine($"| {n} | `{r.Id}` | {r.Name} | {r.Hint} | {Gestures(r)} | {ZoneName(r.Zone)} | {r.Priority} |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static void Export(string path)
        {
            File.WriteAllText(path, Build(), new UTF8Encoding(false));
        }

        static string Gestures(Reaction r)
        {
            if (r.IsIdle) return "ひとりでに";
            var names = new List<string>();
            foreach (var g in r.Gestures) names.Add(GestureName(g));
            return string.Join(" / ", names);
        }

        public static string GestureName(GestureType g)
        {
            switch (g)
            {
                case GestureType.Tap: return "タップ";
                case GestureType.LongPressBegin: return "長押し";
                case GestureType.Swipe: return "スワイプ";
                case GestureType.Flick: return "はやいスワイプ";
                case GestureType.DragBegin: return "ドラッグ";
                case GestureType.Rub: return "こする";
                case GestureType.Circle: return "円をかく";
                case GestureType.PinchBegin: return "ピンチ";
                case GestureType.TwoFingerTap: return "2 本指タップ";
                case GestureType.TwoFingerSwipe: return "2 本指スワイプ";
                case GestureType.RotateBegin: return "ひねる";
                case GestureType.Shake: return "ふる";
                case GestureType.TiltBegin: return "かたむける";
                case GestureType.Idle: return "ひとりでに";
                default: return g.ToString();
            }
        }

        public static string ZoneName(Zone z)
        {
            switch (z)
            {
                case Zone.Any: return "どこでも";
                case Zone.Eye: return "目";
                case Zone.LeftEye: return "左の目";
                case Zone.RightEye: return "右の目";
                case Zone.Background: return "外";
                case Zone.Pupil: return "ひとみ";
                case Zone.EyeTop: return "上";
                case Zone.EyeBottom: return "下";
                case Zone.BothEyes: return "両目";
                default: return z.ToString();
            }
        }
    }
}
