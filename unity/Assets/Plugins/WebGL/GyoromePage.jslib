// ぎょろめ：ブラウザのページの出来事をゲームに伝える（タブを切りかえた・もどった）、ふるえ、画面の端の安全な余白
mergeInto(LibraryManager.library, {
  GyoRegisterPageEvents: function () {
    if (typeof document === "undefined" || window.__gyoPageEvents) return;
    window.__gyoPageEvents = true;
    var send = function (method) {
      try { SendMessage("Game", method); } catch (e) { }
    };
    document.addEventListener("visibilitychange", function () {
      send(document.hidden ? "OnPageHidden" : "OnPageVisible");
    });
    window.addEventListener("pagehide", function () { send("OnPageHidden"); });
    window.addEventListener("pageshow", function () { send("OnPageVisible"); });
  },

  // スマホのノッチなど、画面の端の隠れる部分の幅（画面に対する割合）。side: 0 左 1 上 2 右 3 下
  GyoSafeInset: function (side) {
    try {
      var el = window.__gyoSafeEl;
      if (!el) {
        el = document.createElement("div");
        el.style.cssText = "position:fixed;left:0;top:0;width:0;height:0;visibility:hidden;pointer-events:none;" +
          "padding-left:env(safe-area-inset-left);padding-top:env(safe-area-inset-top);" +
          "padding-right:env(safe-area-inset-right);padding-bottom:env(safe-area-inset-bottom);";
        document.body.appendChild(el);
        window.__gyoSafeEl = el;
      }
      var cs = window.getComputedStyle(el);
      var v = [cs.paddingLeft, cs.paddingTop, cs.paddingRight, cs.paddingBottom][side];
      var px = parseFloat(v) || 0;
      var total = side % 2 == 0 ? window.innerWidth : window.innerHeight;
      return total > 0 ? px / total : 0;
    } catch (e) { return 0; }
  },

  GyoVibrate: function (ms) {
    try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) { }
  },
});
