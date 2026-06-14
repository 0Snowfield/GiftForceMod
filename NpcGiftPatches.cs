using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace GiftForceMod
{
    /// <summary>Harmony 补丁：拦截 NPC 收礼流程</summary>
    internal static class NpcGiftPatches
    {
        private static IMonitor Monitor;

        /// <summary>是否跳过下一次拦截（Prefix 看到此标记直接放行）</summary>
        public static bool BypassNextCheck = false;

        /// <summary>强制指定礼物喜好等级（null = 不强制），用完自动重置</summary>
        public static int? ForcedTaste = null;

        /// <summary>实际匹配到的拦截方法名（由 ModEntry 设置）</summary>
        public static string PatchedMethodName = null;

        /// <summary>初始化，由 ModEntry 调用</summary>
        public static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /*********
        ** Prefix: tryToReceiveActiveObject(Farmer who)
        *********/
        public static bool TryToReceiveActiveObject_Prefix_Farmer(NPC __instance, Farmer who)
        {
            return InterceptGift(__instance, who, who?.ActiveObject);
        }

        /*********
        ** Prefix: tryToReceiveActiveObject(Farmer who, bool probe) — 1.6 实际签名
        *********/
        public static bool TryToReceiveActiveObject_Prefix_FarmerBool(NPC __instance, Farmer who, bool probe)
        {
            // probe=true 是试探性检查（如检查是否能送礼），直接放行
            if (probe)
                return true;

            return InterceptGift(__instance, who, who?.ActiveObject);
        }

        /*********
        ** Prefix: tryToReceiveActiveObject() — 无参版
        *********/
        public static bool TryToReceiveActiveObject_Prefix_NoArgs(NPC __instance)
        {
            return InterceptGift(__instance, Game1.player, Game1.player?.ActiveObject);
        }

        /*********
        ** Prefix: receiveGift(Object o, Farmer giver, ...)
        *********/
        public static bool ReceiveGift_Prefix(NPC __instance, StardewValley.Object o, Farmer giver,
            bool updateGiftLimitInfo, float friendshipChangeMultiplier, bool showResponse)
        {
            return InterceptGift(__instance, giver, o);
        }

        /*********
        ** Prefix: getGiftTasteForThisItem(Item item)
        *********/
        public static bool GetGiftTasteForThisItem_Prefix(Item item, ref int __result)
        {
            try
            {
                if (ForcedTaste.HasValue)
                {
                    __result = ForcedTaste.Value;
                    ForcedTaste = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"GetGiftTasteForThisItem_Prefix 出错: {ex}", LogLevel.Error);
                return true;
            }
        }

        /*********
        ** 通用拦截逻辑
        *********/
        private static bool InterceptGift(NPC npc, Farmer who, Item item)
        {
            try
            {
                if (BypassNextCheck)
                {
                    BypassNextCheck = false;
                    return true;
                }

                if (who == null || item == null)
                    return true;

                // 防重入：已有对话框在显示中，阻止第二次调用（修复双击导致状态被覆盖）
                if (Game1.activeClickableMenu != null)
                    return false;

                // 今天已送过礼 → 放行，让游戏弹原版「今天已送过」提示
                if (who.friendshipData.TryGetValue(npc.Name, out Friendship friendship)
                    && friendship.GiftsToday > 0)
                    return true;

                // 检查物品是否可作为礼物
                if (!item.canBeGivenAsGift())
                    return true;

                // 获取 NPC 对此物品的喜好等级
                int taste = npc.getGiftTasteForThisItem(item);

                // 最爱(loved, taste=0) → 放行
                if (taste == 0)
                    return true;

                // 拦截 → 弹出对话框
                ModEntry.Instance?.ShowFirstDialog(npc, who, item, taste);

                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"InterceptGift 出错: {ex}", LogLevel.Error);
                return true;
            }
        }
    }
}
