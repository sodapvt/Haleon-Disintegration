mergeInto(LibraryManager.library, {
  HALEON_ShowToast: function (messagePtr, duration) {
    var toast = document.getElementById('haleon-toast');
    if (!toast) {
      toast = document.createElement('div');
      toast.id = 'haleon-toast';
      toast.setAttribute('role', 'status');
      toast.setAttribute('aria-live', 'polite');
      toast.style.cssText = 'position:fixed;z-index:10000;left:50%;bottom:8%;' +
        'transform:translateX(-50%);box-sizing:border-box;max-width:90vw;width:max-content;' +
        'padding:14px 20px;border-radius:8px;background:#231f20;color:#fff;' +
        'box-shadow:0 3px 12px rgba(0,0,0,.3);font:16px/1.4 Arial,sans-serif;' +
        'text-align:center;pointer-events:none;';
      // The page root also contains native form inputs in fullscreen mode.
      document.documentElement.appendChild(toast);
    }
    clearTimeout(toast.haleonHideTimer);
    toast.textContent = UTF8ToString(messagePtr);
    toast.hidden = false;
    toast.haleonHideTimer = setTimeout(function () { toast.hidden = true; }, duration);
  },
  HALEON_ShowVideoFullscreenPrompt: function () {
    if (window.HALEON_ShowVideoFullscreenPrompt) {
      window.HALEON_ShowVideoFullscreenPrompt();
    }
  },
  HALEON_EnterPresentationFullscreen: function () {
    if (window.HALEON_EnterPresentationFullscreen) {
      window.HALEON_EnterPresentationFullscreen();
    }
  }
});
