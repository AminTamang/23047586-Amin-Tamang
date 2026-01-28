window.markdownHelper = {
    // Rich text editor functions
    executeCommand: function (command, value = null) {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return; // Exit if editor not found

        editor.focus(); // Ensure editor has focus for commands

        try {
            if (command === 'formatBlock' && value) {
                document.execCommand('formatBlock', false, value); // Apply block formatting (H1, H2, etc.)
            } else if (value) {
                document.execCommand(command, false, value); // Apply command with value (bold, italic, etc.)
            } else {
                document.execCommand(command, false, null); // Apply command without value
            }

            // Update content to trigger Blazor binding
            editor.dispatchEvent(new Event('input', { bubbles: true }));
        } catch (e) {
            console.error('Command failed:', command, e); // Log any execution errors
        }
    },

    insertLink: function (url, text) {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return; // Exit if editor not found

        editor.focus(); // Ensure editor has focus

        try {
            if (!text || text.trim() === '') {
                text = url; // Use URL as link text if none provided
            }

            const linkHtml = `<a href="${url}" target="_blank">${text}</a>`; // Create link HTML
            document.execCommand('insertHTML', false, linkHtml); // Insert link into editor
            editor.dispatchEvent(new Event('input', { bubbles: true })); // Trigger update
        } catch (e) {
            console.error('Insert link failed:', e); // Log link insertion errors
        }
    },

    getEditorHtml: function () {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return ''; // Return empty string if editor missing

        // Clean empty paragraphs to keep HTML clean
        let html = editor.innerHTML;
        html = html.replace(/<p><\/p>/g, ''); // Remove empty paragraphs
        html = html.replace(/<div><\/div>/g, ''); // Remove empty divs

        return html || ''; // Return cleaned HTML or empty string
    },

    setEditorHtml: function (html) {
        const editor = document.getElementById('richTextEditor');
        if (editor) {
            editor.innerHTML = html || ''; // Set editor content

            // Set cursor to end for better UX when editing
            const range = document.createRange();
            const selection = window.getSelection();
            range.selectNodeContents(editor);
            range.collapse(false); // Move cursor to end
            selection.removeAllRanges();
            selection.addRange(range);

            editor.dispatchEvent(new Event('input', { bubbles: true })); // Trigger Blazor update
        }
    },

    clearFormatting: function () {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return; // Exit if editor not found

        editor.focus();
        document.execCommand('removeFormat', false, null); // Remove all text formatting
        editor.dispatchEvent(new Event('input', { bubbles: true })); // Trigger update
    },

    // Get plain text for word count calculation
    getPlainText: function () {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return ''; // Return empty if no editor
        return editor.textContent || editor.innerText || ''; // Extract plain text without HTML tags
    }
};