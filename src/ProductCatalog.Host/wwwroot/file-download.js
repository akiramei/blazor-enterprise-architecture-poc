// ファイルダウンロードヘルパー関数
//
// 【パターン: Blazor Serverでのファイルダウンロード】
//
// 使用シナリオ:
// - C#側で生成したバイナリデータ（CSV, Excel, PDFなど）をブラウザでダウンロード
// - Base64エンコードされたデータを受け取り、Blobとしてダウンロード
//
// 実装ガイド:
// - Base64文字列をデコードしてBlobを作成
// - オブジェクトURLを作成して<a>タグでダウンロード
// - 使用後はメモリリークを防ぐためrevokeObjectURL
//
// AI実装時の注意:
// - ファイル名、MIMEタイプ、Base64データを引数で受け取る
// - ダウンロード後は必ずURL.revokeObjectURLでクリーンアップ
// - 非同期処理ではないがPromiseを返すことで統一性を保つ

window.downloadFile = function (filename, contentType, base64Content) {
    try {
        // Base64デコード
        const byteCharacters = atob(base64Content);
        const byteNumbers = new Array(byteCharacters.length);

        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);

        // Blobを作成
        const blob = new Blob([byteArray], { type: contentType });

        // オブジェクトURLを作成
        const url = URL.createObjectURL(blob);

        // <a>タグを作成してクリック
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();

        // クリーンアップ
        document.body.removeChild(link);
        URL.revokeObjectURL(url);

        console.log(`File downloaded: ${filename}`);
    } catch (error) {
        console.error('Error downloading file:', error);
        throw error;
    }
};
