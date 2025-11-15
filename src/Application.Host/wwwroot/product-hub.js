// SignalR Product Hub クライアント接続
window.productHub = {
    connection: null,

    // SignalR接続を開始
    start: async function (dotNetHelper) {
        if (this.connection) {
            console.log('ProductHub: 既に接続されています');
            return;
        }

        try {
            // SignalR接続を構築
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/products")
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // ProductChanged イベントを購読
            this.connection.on("ProductChanged", () => {
                console.log('ProductHub: ProductChanged イベント受信');

                // .NET側のメソッドを呼び出し
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnProductChanged');
                }
            });

            // 接続開始
            await this.connection.start();
            console.log('ProductHub: 接続成功');

        } catch (err) {
            console.error('ProductHub: 接続エラー:', err);
            // 5秒後に再接続を試みる
            setTimeout(() => this.start(dotNetHelper), 5000);
        }

        // 再接続時の処理
        this.connection.onreconnected(() => {
            console.log('ProductHub: 再接続成功');
        });

        // 切断時の処理
        this.connection.onclose(() => {
            console.log('ProductHub: 接続が切断されました');
        });
    },

    // SignalR接続を停止
    stop: async function () {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log('ProductHub: 接続を停止しました');
            } catch (err) {
                console.error('ProductHub: 停止エラー:', err);
            } finally {
                this.connection = null;
            }
        }
    }
};
