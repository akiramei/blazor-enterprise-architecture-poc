# 図書館貸出管理システム – 要求仕様（AI実装向け）

## 1. システム概要

本システムは、小〜中規模の図書館で運用することを想定した、**基本的な貸出管理システム**である。

本システムでは：

* 「図書タイトル（Book）」と「蔵書コピー（BookCopy）」を区別する
  例：『ハリー・ポッター 1巻』というタイトルに対し、「実物としての1冊・2冊…」が存在する。
* 利用者（Member）が蔵書コピーを借りる（Loan）、返す（Return）、予約する（Reservation）
* 貸出期限（DueDate）、延滞（Overdue）、予約の先着順などの一般的なルールを扱う

という、典型的なライブラリドメインを題材とする。

### スコープ

* **対象**

  * 図書タイトル・蔵書コピー・利用者の管理
  * 貸出／返却
  * 予約（先着順）
  * 貸出期限・延滞状態の判定

* **非対象**

  * 複雑な罰金計算
  * 本格的な職員権限管理
  * 外部図書館連携
  * 蔵書検索の高度な全文検索
  * デジタルコンテンツ（電子書籍）

---

## 2. ドメインモデル / 用語

### 2.1 図書タイトル（Book）

書籍のタイトル情報。
同名タイトルの蔵書コピーが複数存在しうる。

* 属性

  * `BookId`: 一意な識別子
  * `Title`: 書名
  * `Author`: 著者名
  * `Publisher`（任意）
  * `PublishedYear`（任意）
  * `IsActive`: 販売停止・廃棄などで館内利用不可の場合 false

### 2.2 蔵書コピー（BookCopy）

実物としての 1 冊を表す。
Book と 1:N の関係。

* 属性

  * `CopyId`: 一意な識別子
  * `BookId`
  * `CopyNumber`: BookId 内での冊番号（1, 2, 3 …）
  * `Status`: 以下のいずれか

    * `Available`（貸出可能）
    * `OnLoan`（貸出中）
    * `Reserved`（予約で押さえられている状態は扱うかは設計に委ねる）
    * `Inactive`（破損・廃棄などで利用不可）
  * `Location`（任意）
  * `Notes`（任意）

### 2.3 利用者（Member）

図書館の利用者。

* 属性

  * `MemberId`: 一意な識別子
  * `Name`: 利用者名
  * `Email`（任意）
  * `Phone`（任意）
  * `Status`: `Active` / `Suspended`

    * 延滞や違反により貸出停止になる場合に利用

### 2.4 貸出（Loan）

蔵書コピーを借りた記録。

* 属性

  * `LoanId`
  * `CopyId`
  * `MemberId`
  * `LoanDate`: 貸出日
  * `DueDate`: 返却期限
  * `ReturnDate`（任意）: 返却時に設定
  * `Status`: `OnLoan` / `Returned` / `Overdue`

    * `Overdue` は DueDate を過ぎたが ReturnDate がまだない場合

### 2.5 予約（Reservation）

貸出中の本に対して利用者が予約を入れる。

* 属性

  * `ReservationId`
  * `BookId`（蔵書単位ではなくタイトル単位で予約）
  * `MemberId`
  * `ReservedAt`: 予約日時
  * `Status`: `Waiting` / `Ready` / `Cancelled`

    * `Waiting`: 順番待ち中
    * `Ready`: 借りられる状態になった（返却され、受付で取り置き済み等）
    * `Cancelled`: 利用者がキャンセルした、または期限切れで取消された
  * `Position`: 予約の順番（1, 2, 3 …）

> 予約は **Book 単位（タイトル単位）** とし、返却されたコピーがどれでも貸出可能になる。

---

## 3. 機能要件

### 3.1 蔵書管理（Book / BookCopy）

#### 3.1.1 図書タイトル管理

1. 図書タイトル一覧の取得
2. Book の登録（Title, Author など）
3. Book の更新（Title, Author, IsActive …）
4. Book の削除または非アクティブ化（`IsActive = false`）

#### 3.1.2 蔵書コピー管理

1. 指定 BookId のコピー一覧取得
2. コピーの追加

   * `BookId`, `CopyNumber`（自動採番可）
3. コピーの状態変更（Available / Inactive）
4. コピーの削除（今回は非推奨。基本は `Inactive` でよい）

---

### 3.2 利用者管理（Member）

1. 利用者一覧の取得
2. Member の登録・更新
3. Member の状態変更（Active／Suspended）

   * `Suspended` の利用者は貸出不可とする

---

### 3.3 貸出（Loan）

#### 3.3.1 貸出の実行

ユーザー（Member）が蔵書コピーを借りる。

**入力**：`CopyId`, `MemberId`
**処理**：

1. `CopyId` が `Available` であること
2. Member が `Active` であること
3. Member に延滞中の Loan がないこと（オプション）
4. 貸出日 `LoanDate = today`
5. 貸出期限 `DueDate = LoanDate + LoanPeriod`（例：14日）。期間は仕様として固定でよい
6. Loan レコード作成
7. BookCopy の `Status = OnLoan` に変更

---

#### 3.3.2 返却（Return）

**入力**：`LoanId`
**処理**：

1. Loan 状態が `OnLoan` または `Overdue` であること
2. `ReturnDate = today` をセット
3. Loan の `Status = Returned` に変更
4. BookCopy を `Available` に戻す
5. 対応する Book の予約が存在する場合：

   * 最も `Position` が小さい `Reservation` を `Ready` に変更
   * BookCopy の `Status` を `Reserved` に変更（取り置きとして確保）
   * 次の利用者が来館した時点で貸出される想定（貸出処理は別途）

---

### 3.4 延滞（Overdue）

* `Loan.DueDate < today AND ReturnDate == null` の場合自動的に延滞とみなす
* 延滞中は Member を貸出不可（オプション）
* 実装方式は自由：

  * 動的判定（アクセスの度に計算）
  * バッチ処理で `Status = Overdue` に更新
  * AI 実装では前者（動的判定）で十分

---

### 3.5 予約（Reservation）

#### 3.5.1 予約の作成

**入力**：`BookId`, `MemberId`
**条件**：

1. Member が Active であること
2. Member に未返却 Loan がない（オプション）
3. Book の利用可能なコピーが **1冊もない** 場合のみ予約可能

   * 「借りられるコピーが空いているなら予約せず直接貸し出せる」というルール
4. Book 内での予約 `Position` を末尾に追加（先着順）

---

#### 3.5.2 予約のキャンセル

**入力**：`ReservationId`
**処理**：

* `Status = Cancelled` に変更
* Position は保持したままでも、再連番してもよい（実装で明示する）

---

#### 3.5.3 貸出準備（Ready）

返却時に、予約者の先頭が「借りられる番」になる。

* 指定 Book の Reservation のうち
  `Status = Waiting` で最も Position が小さいものを
  `Ready` に変更する

* BookCopy は `Reserved` 状態とする

---

#### 3.5.4 予約者への貸出（Ready → Loan）

予約者が来館したと仮定し、貸出を行う。

**入力**：`ReservationId`, `CopyId`
**処理**：

1. 対象 Reservation が `Ready` 状態であること
2. 対象 Member が Active であること
3. BookCopy が `Reserved` であること
4. 通常の Loan と同様に貸出処理
5. Reservation を `Status = Cancelled` または `Completed` に変更する

   * 今回は `Cancelled` 扱いでよい

---

## 4. ビジネスルール / 不変条件

必ず守るべきルール：

### 4.1 本とコピーの関係

* Book は 1:N の BookCopy を持つ
* BookCopy は常に BookId を持ち、孤立しない

### 4.2 コピーのステータス整合性

* BookCopy の `Status` は以下のいずれか：

  * `Available` → 貸出可能
  * `OnLoan` → 1件だけ Loan があり、ReturnDate 未設定
  * `Reserved` → Reservation により確保済み
  * `Inactive` → 使用不可

コピーは同時に 2人に貸出されてはならない。

### 4.3 貸出・返却

* 貸出は必ず `Available` または `Reserved` のコピーに対して行う
  （予約による貸出の場合のみ `Reserved`）
* `DueDate < today AND ReturnDate is null` は延滞とみなす
* 返却成功後、コピー状態は必ず `Available` または `Reserved` のどちらか

### 4.4 予約の順序

* 同じ Book に対する予約の `Position` は重複不可
* 常に先頭（最小 Position）の予約から処理される
* 予約順序を手動で変更してはならない

### 4.5 予約と貸出の関係

* Book において、**利用可能なコピーが存在する場合は予約不可**
* 既存予約の `Ready` が存在する場合、その BookCopy は該当予約者に優先権がある

---

## 5. 非機能 / 前提（AI向け）

1. **技術スタック自由**
2. **ユーザーは1〜複数を想定可能。認証はスコープ外**
3. **同時更新**

   * 主に Loan と Reservation の整合性チェック
   * 単一ユーザー前提でもよいが、できればトランザクションで整合性を担保する実装が望ましい
4. **UI 最小限**

   * Book一覧
   * BookCopy一覧
   * 貸出・返却画面
   * 予約一覧 / 予約受付

---

## 6. スコープ外（明示）

* 詳細な罰金計算（延滞料金）
* 書誌情報の外部API連携（ISBN検索）
* 複数館の在庫移動
* 電子書籍
* 職員権限（貸出上限・閲覧権限など）

---

## 7. 代表的ユースケース（コマンド名イメージ）

**Book / BookCopy**

1. `CreateBook(title, author, publisher?, publishedYear?)`
2. `UpdateBook(bookId, title?, author?, publisher?, publishedYear?, isActive?)`
3. `AddCopy(bookId)`
4. `GetBookCopies(bookId)`
5. `SetCopyInactive(copyId)`

**Member**
6. `CreateMember(name, email?, phone?)`
7. `UpdateMember(memberId, name?, status?)`

**Loan**
8. `LendCopy(copyId, memberId)`
9. `ReturnCopy(loanId)`
10. `GetLoansByMember(memberId)`

**Reservation**
11. `ReserveBook(bookId, memberId)`
12. `CancelReservation(reservationId)`
13. `ProcessReturn(copyId)` → Ready 状態を作り出し、コピーを確保する
14. `CheckoutReservedCopy(reservationId, copyId)` → Ready → Loan
