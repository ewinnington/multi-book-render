// Theme Switcher JavaScript
class ThemeSwitcher {
    constructor() {
        this.currentTheme = this.getStoredTheme() || 'light';
        this.init();
    }

    init() {
        // Apply the stored theme on page load
        this.applyTheme(this.currentTheme);
        
        // Set up event listeners
        this.setupEventListeners();
        
        // Update UI elements
        this.updateThemeToggleButton();
        
        console.log(`Theme initialized: ${this.currentTheme}`);
    }

    setupEventListeners() {
        // Theme toggle button
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', () => this.toggleTheme());
        }

        // Theme dropdown items (if any)
        const themeDropdownItems = document.querySelectorAll('[data-theme-value]');
        themeDropdownItems.forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const theme = e.target.getAttribute('data-theme-value');
                this.setTheme(theme);
            });
        });
    }

    getStoredTheme() {
        // Try to get theme from localStorage first
        let theme = localStorage.getItem('theme');
        
        // If not found, try to get from server (user preferences)
        if (!theme) {
            const themeMetaTag = document.querySelector('meta[name="user-theme"]');
            if (themeMetaTag) {
                theme = themeMetaTag.getAttribute('content');
            }
        }
        
        // Default to light theme
        return theme || 'light';
    }

    applyTheme(theme) {
        // Apply theme to document
        document.documentElement.setAttribute('data-theme', theme);
        
        // Update body class for Bootstrap compatibility
        document.body.classList.remove('theme-light', 'theme-dark');
        document.body.classList.add(`theme-${theme}`);
        
        // Store in localStorage for immediate persistence
        localStorage.setItem('theme', theme);
        
        this.currentTheme = theme;
        
        console.log(`Theme applied: ${theme}`);
    }

    setTheme(theme) {
        if (theme !== this.currentTheme) {
            this.applyTheme(theme);
            this.updateThemeToggleButton();
            this.saveThemeToServer(theme);
        }
    }

    toggleTheme() {
        const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
    }

    updateThemeToggleButton() {
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            const sunIcon = themeToggle.querySelector('.fa-sun');
            const moonIcon = themeToggle.querySelector('.fa-moon');
            
            if (this.currentTheme === 'dark') {
                themeToggle.setAttribute('title', 'Switch to Light Theme');
                if (sunIcon) sunIcon.style.display = 'none';
                if (moonIcon) moonIcon.style.display = 'inline';
            } else {
                themeToggle.setAttribute('title', 'Switch to Dark Theme');
                if (sunIcon) sunIcon.style.display = 'inline';
                if (moonIcon) moonIcon.style.display = 'none';
            }
        }
    }

    async saveThemeToServer(theme) {
        try {
            const response = await fetch('/Account/UpdateTheme', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ theme: theme })
            });

            if (response.ok) {
                console.log(`Theme saved to server: ${theme}`);
            } else {
                console.warn('Failed to save theme to server');
            }
        } catch (error) {
            console.warn('Error saving theme to server:', error);
            // Theme will still work locally via localStorage
        }
    }

    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    // Public method to get current theme
    getCurrentTheme() {
        return this.currentTheme;
    }

    // Public method for external theme changes
    setThemeFromExternal(theme) {
        this.setTheme(theme);
    }
}

// Initialize theme switcher when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.themeSwitcher = new ThemeSwitcher();
});

// Handle theme preference from user settings form
document.addEventListener('DOMContentLoaded', function() {
    const themeSelect = document.getElementById('Theme');
    if (themeSelect) {
        themeSelect.addEventListener('change', function() {
            const selectedTheme = this.value;
            if (window.themeSwitcher) {
                window.themeSwitcher.setThemeFromExternal(selectedTheme);
            }
        });
    }
});

// Detect system theme preference changes
if (window.matchMedia) {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    mediaQuery.addEventListener('change', function(e) {
        // Only auto-switch if user hasn't manually selected a theme
        const hasManualTheme = localStorage.getItem('theme');
        if (!hasManualTheme && window.themeSwitcher) {
            const systemTheme = e.matches ? 'dark' : 'light';
            window.themeSwitcher.setThemeFromExternal(systemTheme);
        }
    });
}

// Export for global access
window.ThemeSwitcher = ThemeSwitcher;
