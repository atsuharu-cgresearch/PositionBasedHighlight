# Dynamic Eye Highlight Deformation via Soft-body Simulation in 3D Anime Character Rendering

![Demo Preview](https://placehold.jp/900x400.png)

[![WebGL Demo](https://img.shields.io/badge/▶_WebGL_Demo-Play_Now-4CAF50?style=for-the-badge)](https://atsuharu-cgresearch.github.io/MyResearchDemo/)
[![Unity](https://img.shields.io/badge/Unity-2022.3.x-000000?style=flat-square&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](./LICENSE)

<br>

## 📝 概要
本研究は、弾性体シミュレーションを用いて、アニメキャラクターモデルの目のハイライトを生成する手法を提案します。

Keywords
- Non-Photorealistic Rendering → アニメ調レンダリング
- Position Based Dynamics
- 


## 背景・課題



目のハイライトは、感情などを表現するための記号・エフェクトの役割がある

現在よく使われるのは、テクスチャマッピングか、モーフィング

手描きアニメでは、物理現象を意識しつつ、繊細な動きがある
特に接写で


<br>

## 🔑 手法 — Position Based Dynamics
やりたい表現→拘束条件→シミュレーション→レンダリング

## ⚙️ 実装

### GPGPU — Compute Shaderによる並列化

基本

さらに最適化

<br>

### 設計の工夫

モジュール設計

取り替えて実験しやすい設計

長期にわたって維持しやすい
複数人での共同作業の経験はないが、、

<br>

## 🔄 試行錯誤のプロセス
現在の手法にたどり着くまでに試したこと
この研究に取り組むにあたっての根本的な考えなど

鏡面反射→変形　==>　変形するもの→鏡面反射

→奇抜な発想力と、課題の設定・根本的な考え方を示したい

<br>
