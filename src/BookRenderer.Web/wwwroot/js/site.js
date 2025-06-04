// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Initialize libraries when document is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Mermaid
    if (typeof mermaid !== 'undefined') {
        mermaid.initialize({ 
            startOnLoad: true,
            theme: 'default',
            securityLevel: 'loose'
        });
    }
    
    // Initialize KaTeX for math rendering
    if (typeof renderMathInElement !== 'undefined') {
        renderMathInElement(document.body, {
            delimiters: [
                {left: '$$', right: '$$', display: true},
                {left: '$', right: '$', display: false},
                {left: '\\(', right: '\\)', display: false},
                {left: '\\[', right: '\\]', display: true}
            ]
        });
    }
    
    // Initialize Prism for syntax highlighting
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }
});

// Function to re-initialize libraries after content is dynamically loaded
function reinitializeLibraries() {
    // Re-initialize Mermaid for new content
    if (typeof mermaid !== 'undefined') {
        mermaid.init(undefined, document.querySelectorAll('.mermaid:not([data-processed])'));
    }
    
    // Re-initialize KaTeX for new content
    if (typeof renderMathInElement !== 'undefined') {
        renderMathInElement(document.body, {
            delimiters: [
                {left: '$$', right: '$$', display: true},
                {left: '$', right: '$', display: false},
                {left: '\\(', right: '\\)', display: false},
                {left: '\\[', right: '\\]', display: true}
            ]
        });
    }
    
    // Re-initialize Prism for new content
    if (typeof Prism !== 'undefined') {
        Prism.highlightAll();
    }
}

// Function for executing code blocks (placeholder for future implementation)
async function executeCode(button) {
    if (window.executeCode) {
        await window.executeCode(button);
    }
}
