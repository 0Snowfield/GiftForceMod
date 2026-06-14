# 🎁 GiftForceMod

[![SMAPI](https://img.shields.io/badge/SMAPI-4.0+-blue)](https://smapi.io)

星露谷物语模组：**送礼送到村民不喜欢？嘴炮拯救一切！**

当你赠送给村民**喜欢/一般/不喜欢/讨厌**的礼物时，会弹出一个对话框，让你用嘴炮说服对方，或者花金币强行塞给他。

## ✨ 功能

- 🗣️ **正经说服**：成功率与好感度挂钩，成功按喜欢/最爱计算
- 💰 **那你就受着呗！**：花 100g 强行送礼，附带史莱姆暴毙音效
- 🔄 **两阶段对话框**：说服失败还能补救——换一个，或者继续强塞
- 🎵 **自定义音效**：弹窗音 + 搞怪音分别可配
- ⚙️ **全可配置**：花费、成功率、音效名称、最爱还是喜欢，都在 `config.json` 里改
- 🌐 **多语言**：中文 / English，自动跟随游戏语言

## 📦 安装

1. 下载最新版 zip
2. 解压到 `Stardew Valley/Mods/GiftForceMod/`
3. 通过 SMAPI 启动游戏

## ⚙️ 配置

编辑 `config.json`：

| 字段 | 默认值 | 说明 |
|------|--------|------|
| `ForceGiftCost` | 100 | 搞怪选项花费 |
| `PersuadeBaseChance` | 0.60 | 正经选项基础成功率 |
| `PersuadeMaxBonus` | 0.20 | 好感度加成上限 |
| `PersuadeToLoved` | true | 正经成功=最爱(false=喜欢) |
| `ForceToLoved` | false | 搞怪成功=最爱(false=喜欢) |
| `EnableSound` | true | 音效开关 |
| `SoundEffectName` | `"trashcan"` | 弹窗音效 |
| `ForceSoundEffectName` | `"slimedead"` | 搞怪音效 |

## 🌐 翻译

| 语言 | 文件 |
|------|------|
| 中文 | `i18n/default.json` |
| English | `i18n/en.json` |

## 🔧 兼容性

- Stardew Valley 1.6+
- SMAPI 4.0+
- 兼容 Stardew Valley Expanded 等大型模组

## 📜 许可证

[MIT](LICENSE)
