mergeInto(LibraryManager.library, {
  $HaleonFormInput: {
    fields: {},
    publish: function (entry) {
      if (entry.removing) return;
      entry.lastBrowserValue = entry.element.value;
      SendMessage(entry.receiver, 'OnBrowserInput', JSON.stringify({
        id: entry.id, value: entry.element.value
      }));
    },
    create: function (id, receiver, label, email, limit) {
      var input = document.createElement('input');
      input.type = email ? 'email' : 'text';
      input.inputMode = email ? 'email' : 'text';
      input.autocomplete = email ? 'email' : 'off';
      input.name = email ? 'email' : 'haleon-field-' + id;
      input.autocapitalize = email ? 'none' : 'words';
      input.spellcheck = false;
      input.enterKeyHint = id === 2 ? 'done' : 'next';
      input.setAttribute('aria-label', label);
      input.placeholder = label;
      input.className = 'haleon-form-input';
      if (limit > 0) input.maxLength = limit;
      // Below the rotate/fullscreen overlays (z-index 10), above the canvas.
      input.style.cssText = 'position:fixed;z-index:5;box-sizing:border-box;margin:0;' +
        'padding:0;border:0;border-radius:0;background:transparent;' +
        'appearance:none;-webkit-appearance:none;box-shadow:none;' +
        'outline:none;transform-origin:top left;';
      var entry = { id: id, receiver: receiver, element: input, lastBrowserValue: '', lastUnityValue: undefined };
      input.addEventListener('focus', function () {
        // The native input renders text and caret together over Unity's artwork.
        // Keyboard resize must not place a portrait blocker over the focused field.
        document.documentElement.classList.add('haleon-editing');
      });
      input.addEventListener('input', function () { HaleonFormInput.publish(entry); });
      input.addEventListener('change', function () { HaleonFormInput.publish(entry); });
      input.addEventListener('compositionend', function () { HaleonFormInput.publish(entry); });
      input.addEventListener('blur', function () {
        HaleonFormInput.publish(entry);
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
  HALEON_FormInputUpdate: function (id, receiverPtr, labelPtr, valuePtr, email, limit, x, y, width, height, fontSize, alignment, textStylePtr, visible) {
    var entry = HaleonFormInput.fields[id];
    if (!entry) {
      entry = HaleonFormInput.create(id, UTF8ToString(receiverPtr), UTF8ToString(labelPtr), email, limit);
      HaleonFormInput.fields[id] = entry;
    }
    var input = entry.element;
    var styleJson = UTF8ToString(textStylePtr);
    if (entry.styleJson !== styleJson) {
      entry.textStyle = JSON.parse(styleJson);
      entry.styleJson = styleJson;
      var style = entry.textStyle;
      input.style.fontFamily = JSON.stringify(style.family) + ',sans-serif';
      input.style.color = style.color;
      input.style.webkitTextFillColor = style.color;
      input.style.caretColor = style.color;
      input.style.fontWeight = style.weight;
      input.style.fontStyle = style.italic ? 'italic' : 'normal';
      input.placeholder = style.placeholder;
      input.style.setProperty('--haleon-placeholder-family', JSON.stringify(style.placeholderFamily) + ',sans-serif');
      input.style.setProperty('--haleon-placeholder-color', style.placeholderColor);
      input.style.setProperty('--haleon-placeholder-weight', style.placeholderWeight);
      input.style.setProperty('--haleon-placeholder-style', style.placeholderItalic ? 'italic' : 'normal');
    }
    var rect = Module.canvas.getBoundingClientRect();
    var visualFontSize = Math.max(1, fontSize * rect.height);
    var browserFontSize = Math.max(16, visualFontSize);
    var fontScale = visualFontSize / browserFontSize;
    input.style.display = visible ? 'block' : 'none';
    input.style.left = (rect.left + x * rect.width) + 'px';
    input.style.top = (rect.top + y * rect.height) + 'px';
    input.style.width = (width * rect.width / fontScale) + 'px';
    input.style.height = (height * rect.height / fontScale) + 'px';
    // Preserve Unity's visual size while keeping the computed font at least
    // 16px so iOS does not zoom the page when the keyboard opens.
    input.style.fontSize = browserFontSize + 'px';
    input.style.transform = 'scale(' + fontScale + ')';
    input.style.setProperty('--haleon-placeholder-size', (browserFontSize * entry.textStyle.placeholderSizeRatio) + 'px');
    input.style.textAlign = alignment === 1 ? 'center' : alignment === 2 ? 'right' : 'left';
    var value = UTF8ToString(valuePtr);
    // Some keyboards/autofill providers change value without input/change, and
    // may drop focus at the same time. Preserve and publish those changes before
    // applying Unity's previous value, even when the input is no longer focused.
    if (input.value !== entry.lastBrowserValue) {
      HaleonFormInput.publish(entry);
    } else if (document.activeElement !== input && value !== entry.lastUnityValue) {
      if (input.value !== value) input.value = value;
      entry.lastBrowserValue = input.value;
    }
    entry.lastUnityValue = value;
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
