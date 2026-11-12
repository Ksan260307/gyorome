using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：起動すると、ふたつの目が画面の半分ずつに、カメラの方を向いて置かれる。</summary>
    public class BootPlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Boot();
        }

        [TearDown]
        public void TearDown() => Shutdown();

        [UnityTest, Timeout(60000)]
        public IEnumerator TwoEyesFillEachHalfOfTheScreen()
        {
            Assert.IsNotNull(Eyes.A);
            Assert.IsNotNull(Eyes.B);
            Assert.IsTrue(Eyes.A.isLeft);
            Assert.IsFalse(Eyes.B.isLeft);
            var cam = GM.cam;
            Vector3 va = cam.WorldToViewportPoint(Eyes.A.WorldCenter);
            Vector3 vb = cam.WorldToViewportPoint(Eyes.B.WorldCenter);
            Assert.Greater(va.z, 0f);
            Assert.Greater(vb.z, 0f);
            if (ScreenLayout.IsPortrait(Screen.width, Screen.height))
            {
                Assert.Greater(va.y, 0.5f, "縦長では A が上");
                Assert.Less(vb.y, 0.5f);
                Assert.AreEqual(0.5f, va.x, 0.05f);
            }
            else
            {
                Assert.Less(va.x, 0.5f, "横長では A が左");
                Assert.Greater(vb.x, 0.5f);
                Assert.AreEqual(0.5f, va.y, 0.05f);
            }
            // 目の中心は、画面の置き場所と一致する（1 ワールド単位 = 1 ピクセル）
            Vector3 sa = cam.WorldToScreenPoint(Eyes.A.transform.position);
            Assert.AreEqual(Eyes.PlacementA.center.x, sa.x, 4f);
            Assert.AreEqual(Eyes.PlacementA.center.y, sa.y, 4f);
            // 目は画面いっぱい：半径は半分の領域の短い辺の 40%
            Assert.AreEqual(Eyes.PlacementA.radius, Eyes.A.Radius, 0.01f, $"screen={Screen.width}x{Screen.height} region={Eyes.PlacementA.region}");
            Assert.AreEqual(ScreenLayout.Radius(Eyes.PlacementA.region), Eyes.PlacementA.radius, 0.01f);
            Assert.Greater(Eyes.A.Radius, Mathf.Min(Screen.width, Screen.height) * 0.15f);
            yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator EyesFaceTheCameraWithIrisInFront()
        {
            foreach (var e in Eyes.Both)
            {
                // 虹彩はカメラ（-Z 側）に向いている = 白目の中心よりカメラに近い
                float irisDist = Vector3.Distance(e.irisRenderer.bounds.center, GM.cam.transform.position);
                float centerDist = Vector3.Distance(e.WorldCenter, GM.cam.transform.position);
                Assert.Less(irisDist, centerDist - e.Radius * 0.7f, e.name + " の虹彩が前にない");
                Assert.Greater(e.Openness, 0.9f, "まぶたはひらいている");
                Assert.IsNotNull(e.ballRenderer.sharedMaterial);
                Assert.IsNotNull(e.irisRenderer.sharedMaterial);
                Assert.IsTrue(e.ballRenderer.isVisible || Application.isBatchMode);
            }
            Assert.IsNotNull(GM.background, "背景");
            Assert.Greater(GM.background.transform.localPosition.z, Eyes.A.WorldCenter.z, "背景は目のうしろ");
            yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ThemeIsAppliedAtStart()
        {
            var it = Themes.IrisOf(SaveSystem.Settings.irisTheme);
            var lt = Themes.LidOf(SaveSystem.Settings.lidTheme);
            Assert.AreEqual(it.outer, Eyes.A.IrisTarget);
            Assert.AreEqual(lt.lid, Eyes.B.LidColor);
            Assert.Less(Eyes.A.IrisTarget.r + Eyes.A.IrisTarget.g + Eyes.A.IrisTarget.b, 0.4f, "黒目は黒");
            GM.SetLidTheme(2);
            Assert.AreEqual(Themes.Lid[2].lid, Eyes.A.LidColor);
            Assert.AreEqual(Themes.Lid[2].backgroundBottom, GM.cam.backgroundColor);
            yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PortraitLayout_StacksTheEyes()
        {
            Eyes.Layout(1080, 1920);
            Assert.Greater(Eyes.A.transform.localPosition.y, Eyes.B.transform.localPosition.y);
            Assert.AreEqual(Eyes.A.transform.localPosition.x, Eyes.B.transform.localPosition.x, 1e-3f);
            Assert.AreEqual(960f * 0.5f * GyoConst.EyeFill, Eyes.A.Radius, 0.01f);
            GM.Layout(true);
            yield return null;
            Assert.AreEqual(ScreenLayout.IsPortrait(Screen.width, Screen.height), Eyes.PlacementA.center.y > Eyes.PlacementB.center.y);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ScreenPointsMapToEyes()
        {
            Assert.AreEqual(0, Eyes.Hit(EyeA));
            Assert.AreEqual(1, Eyes.Hit(EyeB));
            Assert.AreEqual(-1, Eyes.Hit(BgPoint));
            // さわった点は画面の面（z = 0）、目の中心はそのうしろ（z = 半径）で、遠近のぶんだけ外側にある
            Vector3 w = Eyes.ScreenToWorld(EyeA);
            float k = (Eyes.CameraDistance + Eyes.A.Radius) / Eyes.CameraDistance;
            Assert.AreEqual(Eyes.A.WorldCenter.x, w.x * k, 1e-2f);
            Assert.AreEqual(Eyes.A.WorldCenter.y, w.y * k, 1e-2f);
            Assert.AreEqual(0f, w.z, 1e-3f, "さわった点は目の面（z = 0）");
            yield return null;
        }
    }
}
