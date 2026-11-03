mergeInto(LibraryManager.library, {
  $HaleonFormInput: {
    fields: {},
    publish: function (entry) {
      if (entry.removing) return;
      SendMessage(entry.receiver, 'OnBrowserInput', JSON.stringify({
        id: entry.id, value: entry.element.value
      }));
    },
    create: function (id, receiver, label, email, limit) {
      var input = document.createElement('input');
      input.type = email ? 'email' : 'text';
      input.inputMode = email ? 'email' : 'text';
      input.autocomplete = 'off';
      input.autocapitalize = email ? 'none' : 'words';
      input.spellcheck = false;
      input.enterKeyHint = id === 2 ? 'done' : 'next';
      input.setAttribute('aria-label', label);
      if (limit > 0) input.maxLength = limit;
      // Below the rotate/fullscreen overlays (z-index 10), above the canvas.
      input.style.cssText = 'position:fixed;z-index:5;box-sizing:border-box;margin:0;' +
        'padding:0 12px;border:0;border-radius:0;background:transparent;color:transparent;' +
        'caret-color:#231f20;outline:none;font-family:Arial,sans-serif;';
      var entry = { id: id, receiver: receiver, element: input };
      input.addEventListener('focus', function () {
        input.style.background = '#fff';
        input.style.color = '#231f20';
        // Keyboard resize must not place a portrait blocker over the focused field.
        document.documentElement.classList.add('haleon-editing');
      });
      input.addEventListener('input', function () { HaleonFormInput.publish(entry); });
      input.addEventListener('change', function () { HaleonFormInput.publish(entry); });
      input.addEventListener('blur', function () {
        HaleonFormInput.publish(entry);
        input.style.background = 'transparent';
        input.style.color = 'transparent';
        document.documentElement.classList.remove('haleon-editing');
      });
      input.addEventListener('keydown', function (event) {
        if (event.key === 'Enter' && !event.isComposing) {
          event.preventDefault();
          var next = HaleonFormInput.fields[id + 1];
          if (next && next.element.style.display !== 'none') next.element.focus();
          else input.blur();
        }
      });
      document.body.appendChild(input);
      return entry;
    }
  },
  HALEON_FormInputUpdate__deps: ['$HaleonFormInput'],
  HALEON_FormInputUpdate: function (id, receiverPtr, labelPtr, valuePtr, email, limit, x, y, width, height, visible) {
    var entry = HaleonFormInput.fields[id];
    if (!entry) {
      entry = HaleonFormInput.create(id, UTF8ToString(receiverPtr), UTF8ToString(labelPtr), email, limit);
      HaleonFormInput.fields[id] = entry;
    }
    var input = entry.element;
    var rect = Module.canvas.getBoundingClientRect();
    input.style.display = visible ? 'block' : 'none';
    input.style.left = (rect.left + x * rect.width) + 'px';
    input.style.top = (rect.top + y * rect.height) + 'px';
    input.style.width = (width * rect.width) + 'px';
    input.style.height = (height * rect.height) + 'px';
    // At least 16px prevents iOS zooming when an input receives focus.
    input.style.fontSize = Math.max(16, height * rect.height * 0.45) + 'px';
    if (document.activeElement !== input) {
      var value = UTF8ToString(valuePtr);
      if (input.value !== value) input.value = value;
    }
  },
  HALEON_FormInputRemoveAll__deps: ['$HaleonFormInput'],
  HALEON_FormInputRemoveAll: function () {
    Object.keys(HaleonFormInput.fields).forEach(function (id) {
      var entry = HaleonFormInput.fields[id];
      entry.removing = true;
      entry.element.remove();
    });
    HaleonFormInput.fields = {};
    document.documentElement.classList.remove('haleon-editing');
  }
});
