// code-executor.js
// Helper functions for running code snippets in the browser

window.CodeExecutor = (function(){
    let pyodideReady = null;

    async function loadScript(url){
        return new Promise((resolve, reject)=>{
            const s = document.createElement('script');
            s.src = url;
            s.onload = resolve;
            s.onerror = reject;
            document.head.appendChild(s);
        });
    }

    async function getPyodide(){
        if(pyodideReady) return pyodideReady;
        if(!window.loadPyodide){
            await loadScript('https://cdn.jsdelivr.net/pyodide/v0.25.1/full/pyodide.js');
        }
        pyodideReady = window.loadPyodide({indexURL: 'https://cdn.jsdelivr.net/pyodide/v0.25.1/full/'});
        return pyodideReady;
    }

    async function ensureTypeScript(){
        if(!window.ts){
            await loadScript('https://cdnjs.cloudflare.com/ajax/libs/typescript/5.3.3/typescript.min.js');
        }
    }

    function runInIframe(jsCode){
        return new Promise((resolve)=>{
            const iframe = document.createElement('iframe');
            iframe.style.display = 'none';
            iframe.sandbox = 'allow-scripts';
            document.body.appendChild(iframe);
            let output = '';
            iframe.contentWindow.console.log = (...args)=>{ output += args.join(' ') + '\n'; };
            iframe.contentWindow.console.error = (...args)=>{ output += args.join(' ') + '\n'; };
            iframe.onload = () => {
                setTimeout(()=>{
                    document.body.removeChild(iframe);
                    resolve(output.trim());
                },10);
            };
            const doc = iframe.contentDocument;
            doc.open();
            doc.write(`<script>try{${jsCode}}catch(e){console.error(e);}</script>`);
            doc.close();
        });
    }

    async function runPython(code){
        const pyodide = await getPyodide();
        await pyodide.loadPackagesFromImports(code);
        return pyodide.runPython(code).toString();
    }

    async function runJavaScript(code){
        return runInIframe(code);
    }

    async function runTypeScript(code){
        await ensureTypeScript();
        const js = window.ts.transpile(code);
        return runInIframe(js);
    }

    async function runCSharp(code){
        if(window.DotNet && DotNet.invokeMethodAsync){
            try{
                return await DotNet.invokeMethodAsync('BookRenderer.Web', 'ExecuteSnippet', code);
            }catch(e){
                return e.toString();
            }
        }
        return 'C# execution environment not available.';
    }

    return {
        execute: async function(language, code){
            language = language.toLowerCase();
            if(language === 'javascript' || language === 'js') return runJavaScript(code);
            if(language === 'typescript' || language === 'ts') return runTypeScript(code);
            if(language === 'python' || language === 'py') return runPython(code);
            if(language === 'csharp' || language === 'cs') return runCSharp(code);
            return 'Unsupported language: ' + language;
        }
    };
})();

// Convenience wrapper used by site.js and views
window.executeCode = async function(button){
    const block = button.closest('.executable-code-block');
    if(!block) return;
    const language = (block.dataset.language || '').toLowerCase();
    const codeElement = block.querySelector('pre > code');
    const code = codeElement ? codeElement.textContent : '';
    const outputDiv = block.querySelector('.code-output');
    const chapter = document.getElementById('chapter-content');
    const allowed = (chapter.dataset.allowedLanguages || '').split(',').map(l=>l.trim().toLowerCase()).filter(l=>l);
    const allowExec = chapter.dataset.allowCodeExecution === 'true';
    const userAllow = document.querySelector('meta[name="user-enable-code-execution"]')?.content === 'True' || document.querySelector('meta[name="user-enable-code-execution"]')?.content === 'true';

    outputDiv.style.display = 'block';

    if(!(allowExec && userAllow && allowed.includes(language))){
        outputDiv.textContent = 'Code execution not allowed.';
        return;
    }

    outputDiv.textContent = 'Running...';
    try{
        const result = await window.CodeExecutor.execute(language, code);
        outputDiv.textContent = result;
    }catch(err){
        outputDiv.textContent = err.toString();
    }
};
