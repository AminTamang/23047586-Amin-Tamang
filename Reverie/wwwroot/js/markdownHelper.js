// markdownHelper.js - Enhanced for rich text WYSIWYG editor
window.markdownHelper = {
    // Existing functions
    insertAtCursor: function (textareaId, text) {
        const textarea = document.getElementById(textareaId);
        if (!textarea) return '';

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const content = textarea.value;

        const newText = content.substring(0, start) + text + content.substring(end);
        textarea.value = newText;

        textarea.focus();
        const newPosition = start + text.length;
        textarea.setSelectionRange(newPosition, newPosition);

        return newText;
    },

    wrapSelection: function (textareaId, before, after) {
        const textarea = document.getElementById(textareaId);
        if (!textarea) return '';

        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        const content = textarea.value;

        let newText;
        if (start !== end) {
            const selected = content.substring(start, end);
            newText = content.substring(0, start) + before + selected + after + content.substring(end);
        } else {
            newText = content.substring(0, start) + before + after + content.substring(end);
        }

        textarea.value = newText;
        textarea.focus();

        if (start === end) {
            textarea.setSelectionRange(start + before.length, start + before.length);
        }

        return newText;
    },

    // NEW: Rich text editor functions
    executeCommand: function (command, value = null) {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return;

        editor.focus();

        // Special handling for formatBlock
        if (command === 'formatBlock' && value) {
            document.execCommand('formatBlock', false, value);
        } else if (value) {
            document.execCommand(command, false, value);
        } else {
            document.execCommand(command, false, null);
        }

        // Update content
        editor.dispatchEvent(new Event('input', { bubbles: true }));
    },

    insertLink: function (url, text) {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return;

        editor.focus();

        // Create link HTML
        const linkHtml = `<a href="${url}" target="_blank" rel="noopener noreferrer">${text}</a>`;

        // Insert at cursor
        document.execCommand('insertHTML', false, linkHtml);

        editor.dispatchEvent(new Event('input', { bubbles: true }));
    },

    getEditorHtml: function () {
        const editor = document.getElementById('richTextEditor');
        return editor ? editor.innerHTML : '';
    },

    setEditorHtml: function (html) {
        const editor = document.getElementById('richTextEditor');
        if (editor) {
            editor.innerHTML = html;
        }
    },

    // Convert HTML to Markdown (for saving)
    htmlToMarkdown: function (html) {
        if (!html) return '';

        let md = html;

        // Remove divs but keep their content
        md = md.replace(/<div[^>]*>/gi, '\n');
        md = md.replace(/<\/div>/gi, '\n');

        // Headings
        md = md.replace(/<h1[^>]*>(.*?)<\/h1>/gi, '# $1\n\n');
        md = md.replace(/<h2[^>]*>(.*?)<\/h2>/gi, '## $1\n\n');
        md = md.replace(/<h3[^>]*>(.*?)<\/h3>/gi, '### $1\n\n');

        // Bold/Strong
        md = md.replace(/<strong[^>]*>(.*?)<\/strong>/gi, '**$1**');
        md = md.replace(/<b[^>]*>(.*?)<\/b>/gi, '**$1**');

        // Italic/Em
        md = md.replace(/<em[^>]*>(.*?)<\/em>/gi, '*$1*');
        md = md.replace(/<i[^>]*>(.*?)<\/i>/gi, '*$1*');

        // Underline (markdown doesn't have underline, use emphasis)
        md = md.replace(/<u[^>]*>(.*?)<\/u>/gi, '*$1*');

        // Links
        md = md.replace(/<a[^>]*href="([^"]*)"[^>]*>(.*?)<\/a>/gi, '[$2]($1)');

        // Lists
        md = md.replace(/<ul[^>]*>/gi, '\n');
        md = md.replace(/<\/ul>/gi, '\n');
        md = md.replace(/<ol[^>]*>/gi, '\n');
        md = md.replace(/<\/ol>/gi, '\n');
        md = md.replace(/<li[^>]*>(.*?)<\/li>/gi, '- $1\n');

        // Paragraphs and line breaks
        md = md.replace(/<p[^>]*>(.*?)<\/p>/gi, '$1\n\n');
        md = md.replace(/<br\s*\/?>/gi, '\n');

        // Blockquotes
        md = md.replace(/<blockquote[^>]*>(.*?)<\/blockquote>/gi, '> $1\n\n');

        // Code
        md = md.replace(/<code[^>]*>(.*?)<\/code>/gi, '`$1`');
        md = md.replace(/<pre[^>]*>(.*?)<\/pre>/gi, '```\n$1\n```\n\n');

        // Remove all other HTML tags
        md = md.replace(/<[^>]*>/g, '');

        // Clean up multiple newlines
        md = md.replace(/\n{3,}/g, '\n\n');

        // Trim and return
        return md.trim();
    },

    // Convert Markdown to HTML (for loading into editor)
    markdownToHtml: function (markdown) {
        if (!markdown) return '';

        let html = markdown;

        // Headings
        html = html.replace(/^# (.*?)$/gm, '<h1>$1</h1>');
        html = html.replace(/^## (.*?)$/gm, '<h2>$1</h2>');
        html = html.replace(/^### (.*?)$/gm, '<h3>$1</h3>');

        // Bold
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/__(.*?)__/g, '<strong>$1</strong>');

        // Italic
        html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
        html = html.replace(/_(.*?)_/g, '<em>$1</em>');

        // Links
        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');

        // Lists
        html = html.replace(/^- (.*?)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/s, '<ul>$1</ul>');

        // Ordered lists (simple support)
        html = html.replace(/^\d+\. (.*?)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>)/s, '<ol>$1</ol>');

        // Blockquotes
        html = html.replace(/^> (.*?)$/gm, '<blockquote>$1</blockquote>');

        // Code
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');

        // Line breaks (preserve paragraphs)
        html = html.replace(/\n\n/g, '</p><p>');
        html = html.replace(/\n/g, '<br>');

        // Wrap in paragraph if needed
        if (!html.startsWith('<h') && !html.startsWith('<ul') && !html.startsWith('<ol') && !html.startsWith('<blockquote')) {
            html = '<p>' + html + '</p>';
        }

        // Fix nested paragraphs
        html = html.replace(/<\/p><p>/g, '</p><p>');

        return html;
    },

    // Clear editor formatting
    clearFormatting: function () {
        const editor = document.getElementById('richTextEditor');
        if (!editor) return;

        editor.focus();
        document.execCommand('removeFormat', false, null);
        editor.dispatchEvent(new Event('input', { bubbles: true }));
    }
};