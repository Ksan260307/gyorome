using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Gyorome.Tests
{
    /// <summary>単体テスト：目玉リグ（視線・まぶた・まばたき・気分・ずれ）。メッシュなしで Transform だけを確かめる。</summary>
    public class EyeRigTests
    {
        readonly List<GameObject> _gos = new List<GameObject>();

        EyeRig Make(bool isLeft = true)
        {
            var parent = new GameObject("Stage");
            _gos.Add(parent);
            var rig = EyeRig.Build(isLeft ? "EyeA" : "EyeB", null, isLeft, parent.transform);
            rig.Radius = 100f;
            rig.BasePosition = new Vector3(isLeft ? -300f : 300f, 0f, 100f);
            // ゲームと同じく、カメラ（-Z 側）の方を向く
            rig.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            rig.Snap();
            return rig;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _gos) if (g != null) Object.DestroyImmediate(g);
            _gos.Clear();
        }

        static void Run(EyeRig rig, float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 60f)
            {
                rig.Step(1f / 60f);
                rig.Apply();
            }
        }

        [Test]
        public void Build_CreatesBallIrisAndLids()
        {
            var rig = Make();
            Assert.IsNotNull(rig.ball);
            Assert.IsNotNull(rig.irisT);
            Assert.IsNotNull(rig.lidUpper);
            Assert.IsNotNull(rig.lidLower);
            Assert.AreSame(rig.ball, rig.irisT.parent, "虹彩は白目の子（いっしょに回る）");
            Assert.AreSame(rig.transform, rig.lidUpper.parent, "まぶたは回らない");
            Assert.AreEqual(100f, rig.transform.localScale.x, 1e-3f, "大きさ = 半径");
        }

        [Test]
        public void Look_ClampsToLimits()
        {
            var rig = Make();
            rig.Look(new Vector2(200f, -200f));
            Assert.AreEqual(GyoConst.MaxYawDeg, rig.GazeTarget.x);
            Assert.AreEqual(-GyoConst.MaxPitchDeg, rig.GazeTarget.y);
            rig.LookDir(new Vector2(0.5f, 0.5f));
            Assert.AreEqual(GyoConst.MaxYawDeg * 0.5f, rig.GazeTarget.x, 1e-3f);
        }

        [Test]
        public void Gaze_FollowsTargetSmoothly()
        {
            var rig = Make();
            rig.Look(new Vector2(30f, 10f));
            rig.Step(1f / 60f);
            Assert.Less(rig.Gaze.x, 30f, "すぐには追いつかない");
            Assert.Greater(rig.Gaze.x, 0f);
            Run(rig, 1f);
            Assert.AreEqual(30f, rig.Gaze.x, 0.5f);
            Assert.AreEqual(10f, rig.Gaze.y, 0.5f);
        }

        [Test]
        public void LookingRight_TurnsTheIrisToScreenRight()
        {
            var rig = Make();
            Vector3 irisBefore = rig.irisT.position;
            // 画面の右（ワールド +X）の点を見る
            Vector3 target = rig.WorldCenter + new Vector3(500f, 0f, -300f);
            Vector2 yp = rig.YawPitchTo(target);
            Assert.Greater(yp.x, 10f, "右を見る = yaw が正");
            Assert.AreEqual(0f, yp.y, 1e-3f);
            rig.LookAt(target);
            rig.Snap();
            Vector3 forward = rig.GazeDirection;
            Assert.Greater(forward.x, 0.3f, "白目の前（虹彩の向き）がワールド +X にかたむく");
            Assert.Less(forward.z, 0f, "カメラ（-Z）の方を向いたまま");
            _ = irisBefore;
        }

        [Test]
        public void LookingUp_IsPositivePitch()
        {
            var rig = Make();
            Vector2 yp = rig.YawPitchTo(rig.WorldCenter + new Vector3(0f, 400f, -300f));
            Assert.Greater(yp.y, 10f);
            rig.LookAt(rig.WorldCenter + new Vector3(0f, 400f, -300f));
            rig.Snap();
            Assert.Greater(rig.GazeDirection.y, 0.3f);
        }

        [Test]
        public void Lids_RotateAroundXWhenClosing()
        {
            var rig = Make();
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, rig.lidUpper.localRotation), 1e-3f, "ひらいているときは、作ったままの姿勢");
            rig.Lids(0f, 0f);
            rig.Snap();
            Assert.AreEqual(GyoConst.UpperTravelDeg, Quaternion.Angle(Quaternion.identity, rig.lidUpper.localRotation), 0.1f);
            Assert.AreEqual(GyoConst.LowerTravelDeg, Quaternion.Angle(Quaternion.identity, rig.lidLower.localRotation), 0.1f);
            // 上まぶたは前へ下りる（ローカル +Z 側へ回る）
            Vector3 up = rig.lidUpper.localRotation * Vector3.up;
            Assert.Greater(up.z, 0.5f);
            Assert.AreEqual(0f, rig.Openness, 1e-3f);
        }

        [Test]
        public void Blink_ClosesThenOpens()
        {
            var rig = Make();
            rig.Blink(0.2f);
            Assert.IsTrue(rig.Blinking);
            Assert.AreEqual(1, rig.Blinks);
            float min = 1f;
            for (int i = 0; i < 20; i++)
            {
                rig.Step(0.01f);
                rig.Apply();
                min = Mathf.Min(min, rig.Openness);
            }
            Assert.Less(min, 0.15f, "まばたきのまん中で閉じる");
            Assert.IsFalse(rig.Blinking);
            Assert.AreEqual(1f, rig.Openness, 1e-3f, "終わるとひらく");
            rig.Blink();
            rig.Blink();
            Assert.AreEqual(2, rig.Blinks, "まばたき中はもう一度始めない");
        }

        [Test]
        public void Tilt_IsMirroredBetweenEyes()
        {
            var left = Make(true);
            var right = Make(false);
            left.TiltTarget = right.TiltTarget = 20f;
            left.Snap();
            right.Snap();
            float zl = left.lidUpper.localRotation.eulerAngles.z;
            float zr = right.lidUpper.localRotation.eulerAngles.z;
            Assert.AreEqual(20f, Mathf.DeltaAngle(0f, zl), 0.1f);
            Assert.AreEqual(-20f, Mathf.DeltaAngle(0f, zr), 0.1f);
        }

        [Test]
        public void MoodBaselines_ChangeLidsAndReset()
        {
            var rig = Make();
            rig.SetMood(Mood.Angry, 100f);
            rig.ResetToNeutral();
            Assert.Less(rig.UpperTarget, 0.8f);
            Assert.Greater(rig.TiltTarget, 10f);
            rig.SetMood(Mood.Happy, 100f);
            rig.ResetToNeutral();
            Assert.Less(rig.LowerTarget, 0.7f, "うれしいと下まぶたが上がる");
            Assert.AreEqual(0f, rig.TiltTarget);
            rig.SetMood(Mood.Love, 100f);
            rig.ResetToNeutral();
            Assert.AreEqual(PupilShape.Heart, rig.Shape);
            rig.SetMood(Mood.Normal, 0f);
            rig.ResetToNeutral();
            Assert.AreEqual(PupilShape.Round, rig.Shape);
            Assert.AreEqual(1f, rig.UpperTarget);
            Assert.AreEqual(1f, rig.LowerTarget);
            Assert.AreEqual(GyoConst.PupilNormal, rig.PupilTarget);
        }

        [Test]
        public void Kick_MovesTheEyeAndSettles()
        {
            var rig = Make();
            rig.Kick(new Vector2(0f, 3f));
            rig.Step(1f / 60f);
            rig.Apply();
            Assert.Greater(rig.Offset.y, 0.01f);
            Assert.Greater(rig.transform.localPosition.y, rig.BasePosition.y);
            Run(rig, 4f);
            Assert.Less(rig.Offset.magnitude, 0.01f);
        }

        [Test]
        public void Scale_SquashesAroundTheRadius()
        {
            var rig = Make();
            rig.ScaleTarget = new Vector3(1.2f, 0.8f, 1f);
            rig.Snap();
            Assert.AreEqual(120f, rig.transform.localScale.x, 1e-3f);
            Assert.AreEqual(80f, rig.transform.localScale.y, 1e-3f);
        }
    }
}
