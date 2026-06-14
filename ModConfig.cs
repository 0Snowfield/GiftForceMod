namespace GiftForceMod
{
    /// <summary>模组配置，可通过 config.json 编辑</summary>
    public class ModConfig
    {
        /// <summary>搞怪选项花费的金币数</summary>
        public int ForceGiftCost { get; set; } = 100;

        /// <summary>正经选项基础成功率 (0~1)</summary>
        public double PersuadeBaseChance { get; set; } = 0.60;

        /// <summary>好感度加成上限 (0~1)，实际加成 = (好感度/最大好感度) * 此值</summary>
        public double PersuadeMaxBonus { get; set; } = 0.20;

        /// <summary>正经选项成功时是否直接视为最爱（false=喜欢）</summary>
        public bool PersuadeToLoved { get; set; } = true;

        /// <summary>搞怪选项是否视为最爱（false=喜欢）</summary>
        public bool ForceToLoved { get; set; } = false;

        /// <summary>是否启用音效</summary>
        public bool EnableSound { get; set; } = true;

        /// <summary>弹出对话框时播放的音效名称（游戏内置音效）</summary>
        public string SoundEffectName { get; set; } = "trashcan";

        /// <summary>搞怪选项播放的音效名称（游戏内置音效）</summary>
        public string ForceSoundEffectName { get; set; } = "slimedead";
    }
}
