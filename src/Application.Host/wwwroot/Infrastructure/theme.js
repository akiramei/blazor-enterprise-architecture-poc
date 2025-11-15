// テーマ管理用JavaScriptモジュール

let dotNetRef = null;

/**
 * テーマリスナーの初期化
 */
export function initThemeListener(dotNetReference) {
    dotNetRef = dotNetReference;

    // システムテーマの変更を監視
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    mediaQuery.addEventListener('change', (e) => {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnSystemThemeChanged', e.matches);
        }
    });
}

/**
 * システムがダークモードを好むかを取得
 */
export function getSystemPrefersDark() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

/**
 * テーマをDOMに適用
 */
export function applyTheme(theme) {
    const root = document.documentElement;

    if (theme === 'dark') {
        root.setAttribute('data-bs-theme', 'dark');
        root.classList.add('dark-theme');
        root.classList.remove('light-theme');
    } else {
        root.setAttribute('data-bs-theme', 'light');
        root.classList.add('light-theme');
        root.classList.remove('dark-theme');
    }
}
