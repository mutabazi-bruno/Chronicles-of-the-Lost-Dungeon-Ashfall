// Browser-side half of saving on WebGL.
//
// Unity writes save.json into an in-memory emscripten filesystem. Nothing reaches
// IndexedDB - and so nothing survives the tab closing - until FS.syncfs is called.
// Application.ExternalEval used to do this but is deprecated in Unity 6, so the
// flush lives here instead.

mergeInto(LibraryManager.library, {

  // Push the in-memory filesystem out to IndexedDB.
  AshfallSyncSaveFs: function () {
    try {
      FS.syncfs(false, function (err) {
        if (err) {
          console.error('[Ashfall] save flush failed:', err);
        }
      });
    } catch (e) {
      console.error('[Ashfall] save flush threw:', e);
    }
  },

  // Closing a tab does not reliably raise OnApplicationQuit, and on mobile browsers
  // it often never fires at all. pagehide is the event that does fire in those
  // cases, so it is used to ask Unity for one last autosave.
  AshfallRegisterUnloadHook: function (objectNamePtr, methodNamePtr) {
    var objectName = UTF8ToString(objectNamePtr);
    var methodName = UTF8ToString(methodNamePtr);

    if (typeof window === 'undefined' || window.__ashfallUnloadHooked) {
      return;
    }
    window.__ashfallUnloadHooked = true;

    var flush = function () {
      try {
        // SendMessage is synchronous, so the save is written before the handler
        // returns and there is still a chance to sync it out.
        SendMessage(objectName, methodName);
      } catch (e) {
        // the page is going away regardless; never block teardown on this
      }
    };

    window.addEventListener('pagehide', flush);
    window.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'hidden') {
        flush();
      }
    });
  }
});
