using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：見つけた数・ふきだし・図鑑・せってい・記号となみだ。</summary>
    public class UiPlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Boot();
            Brain.Paused = true;
        }

        [TearDown]
        public void TearDown() => Shutdown();

        [UnityTest, Timeout(60000)]
        public IEnumerator CounterPopupAndToast_FollowDiscoveries()
        {
            Assert.IsTrue(UI.CounterText.StartsWith("0 /"), UI.CounterText);
            yield return Tap(EyeA);
            yield return null;
            Assert.IsTrue(UI.CounterText.StartsWith("1 /"), UI.CounterText);
            Assert.AreEqual(1, UI.PopupCount, "反応の名前が出る");
            Assert.AreEqual(ReactionCatalog.Find(Director.LastId).Name, UI.LastPopup);
            StringAssert.Contains("あたらしい反応", UI.LastToast);
            yield return WaitIdle();
            // 設定で名前を消せる
            SaveSystem.Settings.names = false;
            yield return Tap(EyeB);
            Assert.AreEqual(1, UI.PopupCount);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Book_ListsEveryReactionAndBlocksTouches()
        {
            Assert.IsFalse(UI.IsPointerOverUI(BgPoint), "何もない所は UI ではない");
            Assert.IsFalse(UI.IsPointerOverUI(EyeA));
            UI.OpenBook();
            yield return null;
            Assert.IsTrue(UI.BookOpen);
            Assert.AreEqual(ReactionCatalog.Count, UI.BookEntries);
            Assert.IsTrue(UI.IsPointerOverUI(EyeA), "窓がひらいている間は、目にさわれない");
            UI.CloseBook();
            Assert.IsFalse(UI.BookOpen);
            Assert.IsFalse(UI.IsPointerOverUI(EyeA));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator HudButtons_AreUIAndEverythingElseIsNot()
        {
            yield return null;
            var button = UI.Root.Q<Button>("book-button");
            Assert.IsNotNull(button);
            Rect wb = button.worldBound;
            Vector2 centerPanel = wb.center;
            var vt = UI.Root.panel.visualTree;
            Vector2 screen = new Vector2(centerPanel.x / vt.layout.width * Screen.width, (1f - centerPanel.y / vt.layout.height) * Screen.height);
            Assert.IsTrue(UI.IsPointerOverUI(screen), "本のボタンの上");
            Assert.IsFalse(UI.IsPointerOverUI(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Settings_TogglesAndSwatchesPersist()
        {
            UI.OpenSettings();
            yield return null;
            Assert.IsTrue(UI.SettingsOpen);
            var sound = UI.Root.Q<Toggle>("toggle-sound");
            Assert.IsTrue(sound.value);
            sound.value = false;
            Assert.IsFalse(SaveSystem.Settings.sound);
            Assert.IsFalse(GM.audioManager.Play("pop"), "音を切ると鳴らない");
            sound.value = true;
            Assert.IsTrue(GM.audioManager.Play("pop"));
            Assert.AreEqual(Themes.Iris.Length, UI.Root.Q("iris-swatches").childCount);
            Assert.AreEqual(Themes.Lid.Length, UI.Root.Q("lid-swatches").childCount);
            UI.CloseSettings();
            Assert.IsFalse(UI.SettingsOpen);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Hints_AppearWhenIdle()
        {
            UI.hintInterval = 0.5f;
            UI.ResetHintTimer();
            GyoIn.NoteInput();
            yield return Seconds(1f);
            Assert.AreEqual(0, UI.HintsShown, "さわった直後はヒントを出さない");
            // 6 秒より前に入力があった扱いにする
            var field = typeof(GyoInput).GetProperty("LastInputTime");
            yield return Seconds(6.5f);
            Assert.GreaterOrEqual(UI.HintsShown, 1, "しばらくさわらないとヒント");
            _ = field;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator EmotesAndDrops_AppearAndDisappear()
        {
            GM.emotes.Spawn("♥", Eyes.A, new Vector2(0.5f, 0.8f));
            GM.emotes.Tear(Eyes.A, 1);
            GM.emotes.Sweat(Eyes.B);
            yield return null;
            Assert.AreEqual(1, GM.emotes.ActiveEmotes);
            Assert.AreEqual(2, GM.emotes.ActiveDrops);
            Assert.AreEqual("♥", GM.emotes.LastEmote);
            yield return Seconds(2.2f);
            Assert.AreEqual(0, GM.emotes.ActiveEmotes, "記号は消える");
            Assert.AreEqual(0, GM.emotes.ActiveDrops, "しずくは消える");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PageHidden_PausesIdleBehaviour()
        {
            GM.OnPageHidden();
            Assert.IsTrue(GM.PageHidden);
            Assert.IsTrue(Brain.Paused);
            GM.OnPageVisible();
            Assert.IsFalse(GM.PageHidden);
            Assert.IsFalse(Brain.Paused);
            yield return null;
        }
    }
}
