using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace GiftForceMod
{
    /// <summary>模组入口点</summary>
    public class ModEntry : Mod
    {
        /*********
        ** 静态单例
        *********/
        public static ModEntry Instance { get; private set; }

        /*********
        ** 字段
        *********/
        private ModConfig Config;

        private NPC _currentNpc;
        private Farmer _currentFarmer;
        private Item _currentItem;
        private int _currentTaste;

        private Action _pendingAction = null;

        private System.Reflection.MethodInfo _giftMethod;
        private bool _giftMethodNeedsFarmer;
        private bool _giftMethodNeedsItem;

        /*********
        ** Entry
        *********/
        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // 加载配置
            Config = helper.ReadConfig<ModConfig>();
            helper.WriteConfig(Config);

            // 加载翻译
            var i18n = helper.Translation;

            // 初始化 Harmony
            NpcGiftPatches.Initialize(this.Monitor);
            var harmony = new Harmony(this.ModManifest.UniqueID);
            PatchGiftMethod(harmony);
            PatchTasteMethod(harmony);

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            Monitor.Log("GiftForceMod 已加载！", LogLevel.Info);
        }

        /*********
        ** 补丁注册
        *********/
        private void PatchGiftMethod(Harmony harmony)
        {
            var candidates = new List<(string name, Type[] paramTypes, string prefixName, bool needsFarmer, bool needsItem)>
            {
                ("tryToReceiveActiveObject", new[] { typeof(Farmer), typeof(bool) },
                 nameof(NpcGiftPatches.TryToReceiveActiveObject_Prefix_FarmerBool), true, false),
                ("tryToReceiveActiveObject", new[] { typeof(Farmer) },
                 nameof(NpcGiftPatches.TryToReceiveActiveObject_Prefix_Farmer), true, false),
                ("tryToReceiveActiveObject", Type.EmptyTypes,
                 nameof(NpcGiftPatches.TryToReceiveActiveObject_Prefix_NoArgs), false, false),
                ("receiveGift", new[] { typeof(StardewValley.Object), typeof(Farmer), typeof(bool), typeof(float), typeof(bool) },
                 nameof(NpcGiftPatches.ReceiveGift_Prefix), true, true),
            };

            foreach (var (name, paramTypes, prefixName, needsFarmer, needsItem) in candidates)
            {
                var method = AccessTools.Method(typeof(NPC), name, paramTypes);
                if (method != null)
                {
                    harmony.Patch(original: method, prefix: new HarmonyMethod(typeof(NpcGiftPatches), prefixName));
                    _giftMethod = method;
                    _giftMethodNeedsFarmer = needsFarmer;
                    _giftMethodNeedsItem = needsItem;
                    return;
                }
            }

            Monitor.Log("所有送礼方法签名均未匹配！送礼拦截不会生效。", LogLevel.Error);
        }

        private void PatchTasteMethod(Harmony harmony)
        {
            var method = AccessTools.Method(typeof(NPC), "getGiftTasteForThisItem", new[] { typeof(Item) });
            if (method != null)
            {
                harmony.Patch(original: method,
                    prefix: new HarmonyMethod(typeof(NpcGiftPatches), nameof(NpcGiftPatches.GetGiftTasteForThisItem_Prefix)));
            }
            else
            {
                Monitor.Log("找不到 NPC.getGiftTasteForThisItem(Item)，强制喜好可能无效", LogLevel.Warn);
            }
        }

        /*********
        ** 第一层对话框（送礼）
        *********/
        public void ShowFirstDialog(NPC npc, Farmer who, Item gift, int taste)
        {
            _currentNpc = npc;
            _currentFarmer = who;
            _currentItem = gift;
            _currentTaste = taste;

            PlayDialogSound();

            var i18n = Helper.Translation;
            var choices = new List<Response>
            {
                new Response("persuade", i18n.Get("dialog.first.persuade")),
                new Response("force", i18n.Get("dialog.first.force", new { cost = Config.ForceGiftCost }))
            };

            Game1.currentLocation.createQuestionDialogue(
                i18n.Get("dialog.first.question", new { npc = npc.displayName, item = gift.DisplayName }),
                choices.ToArray(),
                OnFirstDialogAnswer
            );
        }

        private void OnFirstDialogAnswer(Farmer who, string answer)
        {
            if (answer == "persuade")
            {
                double successChance = GetPersuadeChance(_currentNpc);
                double roll = new Random().NextDouble();

                if (roll < successChance)
                {
                    int targetTaste = Config.PersuadeToLoved ? 0 : 2;
                    _pendingAction = () => ExecuteForcedGift(targetTaste);
                }
                else
                {
                    _pendingAction = () => ShowSecondDialog(_currentNpc, _currentFarmer, _currentItem);
                }
            }
            else if (answer == "force")
            {
                if (Game1.player.Money >= Config.ForceGiftCost)
                {
                    Game1.player.Money -= Config.ForceGiftCost;
                    PlayForceSound();
                    _pendingAction = () => ExecuteForcedGift(Config.ForceToLoved ? 0 : 2);
                }
                else
                {
                    var i18n = Helper.Translation;
                    Game1.addHUDMessage(new HUDMessage(
                        i18n.Get("hud.notEnoughGold", new { cost = Config.ForceGiftCost }),
                        HUDMessage.error_type));
                    PlayDialogSound();
                    ShowFirstDialog(_currentNpc, _currentFarmer, _currentItem, _currentTaste);
                }
            }
        }

        /*********
        ** 第二层对话框
        *********/
        public void ShowSecondDialog(NPC npc, Farmer who, Item gift)
        {
            PlayDialogSound();

            var i18n = Helper.Translation;
            var choices = new List<Response>
            {
                new Response("cancel", i18n.Get("dialog.second.cancel")),
                new Response("force", i18n.Get("dialog.second.force", new { cost = Config.ForceGiftCost }))
            };

            Game1.currentLocation.createQuestionDialogue(
                i18n.Get("dialog.second.question", new { npc = npc.displayName }),
                choices.ToArray(),
                OnSecondDialogAnswer
            );
        }

        private void OnSecondDialogAnswer(Farmer who, string answer)
        {
            if (answer == "cancel")
            {
                _currentNpc = null;
                _currentFarmer = null;
                _currentItem = null;
            }
            else if (answer == "force")
            {
                if (Game1.player.Money >= Config.ForceGiftCost)
                {
                    Game1.player.Money -= Config.ForceGiftCost;
                    PlayForceSound();
                    _pendingAction = () => ExecuteForcedGift(Config.ForceToLoved ? 0 : 2);
                }
                else
                {
                    var i18n = Helper.Translation;
                    Game1.addHUDMessage(new HUDMessage(
                        i18n.Get("hud.notEnoughGold", new { cost = Config.ForceGiftCost }),
                        HUDMessage.error_type));
                    PlayDialogSound();
                    ShowSecondDialog(_currentNpc, _currentFarmer, _currentItem);
                }
            }
        }

        /*********
        ** 执行送礼
        *********/
        private void ExecuteForcedGift(int giftTasteLevel)
        {
            try
            {
                if (_giftMethod == null)
                {
                    Monitor.Log("未找到送礼方法，无法执行！", LogLevel.Error);
                    return;
                }

                NpcGiftPatches.ForcedTaste = giftTasteLevel;
                NpcGiftPatches.BypassNextCheck = true;

                int paramCount = _giftMethod.GetParameters().Length;
                object[] args = paramCount switch
                {
                    0 => null,
                    1 => new object[] { _currentFarmer },
                    2 => new object[] { _currentFarmer, false },
                    _ => new object[] { _currentItem, _currentFarmer, true, 1f, true },
                };

                _giftMethod.Invoke(_currentNpc, args);
            }
            catch (Exception ex)
            {
                Monitor.Log($"ExecuteForcedGift 出错: {ex}", LogLevel.Error);
            }
        }

        /*********
        ** 延迟执行
        *********/
        private void OnUpdateTicked(object sender, StardewModdingAPI.Events.UpdateTickedEventArgs e)
        {
            if (_pendingAction == null) return;
            if (Game1.activeClickableMenu != null || Game1.dialogueUp) return;

            var action = _pendingAction;
            _pendingAction = null;
            action();
        }

        /*********
        ** 工具
        *********/
        private double GetPersuadeChance(NPC npc)
        {
            int friendship = 0;
            if (_currentFarmer.friendshipData.TryGetValue(npc.Name, out Friendship f))
                friendship = f.Points;

            int maxPoints = npc.datable.Value ? 3500 : 2500;
            double bonus = ((double)friendship / maxPoints) * Config.PersuadeMaxBonus;
            return Math.Clamp(Config.PersuadeBaseChance + bonus, 0.0, 1.0);
        }

        private void PlayDialogSound()
        {
            if (Config.EnableSound && !string.IsNullOrEmpty(Config.SoundEffectName))
            {
                try { Game1.playSound(Config.SoundEffectName); }
                catch (Exception ex) { Monitor.Log($"播放音效失败: {ex.Message}", LogLevel.Warn); }
            }
        }

        private void PlayForceSound()
        {
            if (Config.EnableSound && !string.IsNullOrEmpty(Config.ForceSoundEffectName))
            {
                try { Game1.playSound(Config.ForceSoundEffectName); }
                catch (Exception ex) { Monitor.Log($"播放搞怪音效失败: {ex.Message}", LogLevel.Warn); }
            }
        }
    }
}
