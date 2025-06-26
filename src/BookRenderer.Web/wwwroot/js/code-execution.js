document.addEventListener('DOMContentLoaded', () => {
    // This function can be called multiple times, e.g., after loading new content dynamically.
    function initializeCodeExecution() {
        // Select all JavaScript code blocks that haven't been initialized yet.
        const jsCodeBlocks = document.querySelectorAll('pre code.language-js:not([data-initialized]), pre code.language-javascript:not([data-initialized])');

        jsCodeBlocks.forEach(codeBlock => {
            codeBlock.dataset.initialized = 'true'; // Mark as initialized
            const pre = codeBlock.parentElement;

            // Create a toolbar for buttons
            const toolbar = document.createElement('div');
            toolbar.className = 'code-toolbar';

            // Create the "Run" button
            const runButton = document.createElement('button');
            runButton.textContent = '▶ Run';
            runButton.className = 'run-code-btn';

            // Create the "Edit" button
            const editButton = document.createElement('button');
            editButton.textContent = 'Edit';
            editButton.className = 'edit-code-btn';

            // Add buttons to toolbar. Edit on the left, Run on the right.
            toolbar.appendChild(editButton);
            toolbar.appendChild(runButton);

            // Add toolbar to the <pre> element. Using insertBefore to place it before the <code>.
            pre.insertBefore(toolbar, codeBlock);

            // Create the output area, initially hidden
            const outputArea = document.createElement('div');
            outputArea.className = 'code-output';
            outputArea.style.display = 'none';
            pre.after(outputArea);

            editButton.addEventListener('click', () => {
                const isEditing = codeBlock.isContentEditable;

                if (isEditing) {
                    // Stop editing
                    codeBlock.contentEditable = 'false';
                    editButton.textContent = 'Edit';
                    pre.classList.remove('is-editing');

                    // Re-run Prism highlighting on the block to fix syntax colors
                    if (window.Prism) {
                        // Get the raw code from the potentially messy DOM
                        const currentCode = codeBlock.textContent;
                        // Clear the code block of any previous Prism spans and set the new content
                        codeBlock.textContent = currentCode;
                        // Re-highlight the element
                        Prism.highlightElement(codeBlock);
                    }
                } else {
                    // Start editing
                    codeBlock.contentEditable = 'true';
                    editButton.textContent = 'Done';
                    pre.classList.add('is-editing');
                    codeBlock.focus();
                }
            });

            runButton.addEventListener('click', () => {
                const code = codeBlock.textContent;
                outputArea.innerHTML = '';
                outputArea.style.display = 'block';

                const capturedLogs = [];
                const oldLog = console.log;

                // Temporarily override console.log to capture its output
                console.log = (...args) => {
                    oldLog.apply(console, args); // Preserve original console behavior
                    const formatted = args.map(arg => {
                        try {
                            if (arg instanceof Error) return arg.stack || arg.message;
                            if (typeof arg === 'object' && arg !== null) return JSON.stringify(arg, null, 2);
                            return String(arg);
                        } catch (e) {
                            return '[Unserializable Object]';
                        }
                    }).join(' ');
                    capturedLogs.push(formatted);
                };

                try {
                    // Use the Function constructor for safer, isolated execution
                    const result = new Function(code)();

                    // Display captured logs
                    if (capturedLogs.length > 0) {
                        const logOutput = document.createElement('pre');
                        logOutput.textContent = capturedLogs.join('\n');
                        outputArea.appendChild(logOutput);
                    }

                    // Display the return value, if any
                    if (result !== undefined) {
                        const resultHeader = document.createElement('div');
                        resultHeader.className = 'output-header';
                        resultHeader.textContent = 'Return Value:';
                        
                        const resultOutput = document.createElement('pre');
                        resultOutput.textContent = String(result);

                        outputArea.appendChild(resultHeader);
                        outputArea.appendChild(resultOutput);
                    }

                    // If there's no output at all, show a message
                    if (capturedLogs.length === 0 && result === undefined) {
                        outputArea.innerHTML = '<pre class="empty-output">[No output produced]</pre>';
                    }
                } catch (e) {
                    // Display any execution errors
                    const errorOutput = document.createElement('pre');
                    errorOutput.className = 'error-output';
                    errorOutput.textContent = e.stack || e.message;
                    outputArea.appendChild(errorOutput);
                } finally {
                    // IMPORTANT: Restore the original console.log function
                    console.log = oldLog;
                }
            });
        });
    }

    // Since chapter content might be loaded dynamically (e.g., via AJAX),
    // we use a MutationObserver to re-run the initialization when the DOM changes.
    // This assumes your main content is inside an element with id="chapter-content".
    // Adjust the selector if your content container is different.
    const contentContainer = document.getElementById('chapter-content') || document.body;
    const observer = new MutationObserver(() => {
        initializeCodeExecution();
    });
    observer.observe(contentContainer, {
        childList: true,
        subtree: true
    });

    // Run once on initial page load
    initializeCodeExecution();
});