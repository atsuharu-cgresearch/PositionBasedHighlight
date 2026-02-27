# [Position Based Highlight]

> **[🚀 ブラウザでデモを実行する](https://あなたのユーザー名.github.io/リポジトリ名/)**
> ※Unity WebGLを使用してビルドされています。

## 📝 概要
[ここに研究の背景や目的を1〜2行で記入]
例：本プロジェクトは、UnityのCompute Shaderを用いたGPGPU実装により、複雑な物理シミュレーションをリアルタイムで実行し、HLSLによるNPR（非フォトリアルレンダリング）を用いて視覚化する試みです。

---

## 🛠 技術的特徴
### ⚡ GPGPUによる物理シミュレーション
Compute Shaderを活用し、本来CPU負荷の高い計算を並列化することで高速化を実現しました。

* **実装内容:** [例：パーティクルベースの流体計算 / 剛体衝突判定]
* **工夫点:** [例：共有メモリ（Shared Memory）を活用したメモリアクセスの最適化]
* **数式:** $$
    \mathbf{v}_{next} = \mathbf{v}_{curr} + \frac{\mathbf{F}}{m} \Delta t
    $$
    （※このようにアルゴリズムの根拠となる数式を書くと専門性が伝わります）

### 🎨 HLSLによるNPRシェーダー
URP（Universal Render Pipeline）上で、独自のHLSLシェーダーを実装しました。

* **輪郭線抽出:** [例：背面法と深度法を組み合わせたアーティファクトの少ない描画]
* **トーンマッピング:** [例：多階調のセルルックに加え、ハッチング処理を追加]

---

## 📂 主要なソースコード
「特にここを見てほしい」というコードへのリンクです。

* [ParticleSim.compute](./Assets/Scripts/Shaders/ParticleSim.compute) : GPGPUのメインロジック
* [NPR_Toon.shader](./Assets/Shaders/NPR_Toon.shader) : HLSLによる描画ロジック

---

## 📺 デモ動画 / スクリーンショット
| 物理計算の様子 | シェーディング結果 |
| :--- | :--- |
| ![Demo GIF 1](画像のURL) | ![Demo Image 1](画像のURL) |
| [簡単な説明] | [簡単な説明] |

---

## 🏗 開発環境
- **Engine:** Unity 2022.3.x (URP)
- **Language:** C#, HLSL (Compute Shader)
- **Library:** [もしあれば記入]
- **Target:** WebGL / Windows

## 📜 実行方法
1. 本リポジトリをクローンします。
2. Unity 2022.3.x でプロジェクトを開きます。
3. `Assets/Scenes/Main.unity` を開き、再生ボタンを押してください。