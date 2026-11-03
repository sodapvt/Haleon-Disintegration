mergeInto(LibraryManager.library, {
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
