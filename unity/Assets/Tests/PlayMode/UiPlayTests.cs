using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：画面の上のもの（ふきだし・図鑑は出さない）・せってい・記号となみだ。</summary>
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
        public IEnumerator NoBookCounterOrNameBubbles()
        {
            Assert.IsNull(UI.Root.Q("book"), "図鑑はない");
            Assert.IsNull(UI.Root.Q("book-button"));
            Assert.IsNull(UI.Root.Q("counter"));
            Assert.IsNull(UI.Root.Q("popups"), "反応の名前のふきだしはない");
            Assert.IsNull(UI.Root.Q("toast"));
            Assert.IsNull(UI.Root.Q("hint"));
            yield return Tap(EyeA);
            yield return null;
            Assert.AreEqual(0, UI.Root.Query<Label>(className: "popup").ToList().Count, "タップしても文字は出ない");
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SettingsButton_IsUIAndEverythingElseIsNot()
        {
            yield return null;
            var button = UI.Root.Q<Button>("settings-button");
            Assert.IsNotNull(button);
            Vector2 c = button.worldBound.center;
            var vt = UI.Root.panel.visualTree;
            Vector2 screen = new Vector2(c.x / vt.layout.width * Screen.width, (1f - c.y / vt.layout.height) * Screen.height);
            Assert.IsTrue(UI.IsPointerOverUI(screen), "歯車のボタンの上");
            Assert.IsFalse(UI.IsPointerOverUI(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)));
            Assert.IsFalse(UI.IsPointerOverUI(EyeA));
            Assert.IsFalse(UI.IsPointerOverUI(BgPoint));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Settings_TogglesAndSwatchesPersist()
        {
            UI.OpenSettings();
            yield return null;
            Assert.IsTrue(UI.SettingsOpen);
            Assert.IsTrue(UI.IsPointerOverUI(EyeA), "窓がひらいている間は、目にさわれない");
            var sound = UI.Root.Q<Toggle>("toggle-sound");
            Assert.IsTrue(sound.value);
            sound.value = false;
            Assert.IsFalse(SaveSystem.Settings.sound);
            Assert.IsFalse(GM.audioManager.Play("pop"), "音を切ると鳴らない");
            sound.value = true;
            Assert.IsTrue(GM.audioManager.Play("pop"));
            Assert.IsNull(UI.Root.Q("iris-swatches"), "黒目の色は選ばない");
            Assert.AreEqual(Themes.Lid.Length, UI.Root.Q("lid-swatches").childCount);
            UI.CloseSettings();
            Assert.IsFalse(UI.SettingsOpen);
            Assert.IsFalse(UI.IsPointerOverUI(EyeA));
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
