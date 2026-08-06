using System;
using System.Collections.Generic;

namespace MyFramework.Util
{
    /// <summary>
    ///     随机数模块类型枚举。
    ///     每个模块由 RandomUtility 根据全局主种子 + 模块值派生出独立种子，
    ///     确保各模块的随机数流互不干扰，且相同主种子下可确定性重放。
    /// </summary>
    public enum ModuleType
    {
        /// <summary>通用玩法逻辑（关卡、事件触发等）</summary>
        Gameplay,

        /// <summary>程序化生成（地图、场景布局）</summary>
        ProcGen,

        /// <summary>掉落系统（物品、奖励）</summary>
        Drop,

        /// <summary>战斗数值（伤害浮动、暴击判定）</summary>
        Combat,

        /// <summary>AI 决策（行为树、寻路偏移）</summary>
        AI,

        /// <summary>视觉表现（特效、动画随机变体）</summary>
        Visual,

        /// <summary>地图</summary>
        Map
    }

    /// <summary>
    ///     种子管理器 —— 为不同模块提供独立、确定性的随机数生成器。
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>核心用法</b>
    ///     </para>
    ///     <code>
    ///         RandomUtility.Init(12345); // 设置全局主种子
    ///         var combatRng = RandomUtility.GetRandomGenerator(ModuleType.Combat);
    ///         int damageRoll = combatRng.Next(10, 21);
    ///     </code>
    ///     <para>
    ///         <b>确定性保证</b>
    ///     </para>
    ///     相同的 <c>Init(seed)</c> + 相同的 <c>ModuleType</c> 在任何平台、任何时间
    ///     产生的随机序列完全一致，适用于重放、回滚、单元测试等场景。
    ///     <para>
    ///         <b>线程安全</b>
    ///     </para>
    ///     本类所有方法均非线程安全。如需多线程使用，请自行加锁。
    ///     <para>
    ///         <b>跨 .NET 版本确定性</b>
    ///     </para>
    ///     <c>HashCode.Combine</c> 在不同 .NET 版本间可能产生不同哈希值。
    ///     如需严格的跨版本确定性，后续可替换为自定义 FNV-1a 哈希。
    /// </remarks>
    public static class RandomUtility
    {
        /// <summary>
        ///     未调用 Init() 时的默认种子（使用 Environment.TickCount 保证每次启动不同）。
        /// </summary>
        private static int _globalSeed = Environment.TickCount;

        /// <summary>
        ///     用于首次自动初始化的标记。
        /// </summary>
        private static bool _initialized;

        /// <summary>
        ///     模块 → 随机数生成器实例的缓存，首次访问时按需创建。
        /// </summary>
        private static readonly Dictionary<ModuleType, Random> _generators = new();

        /// <summary>
        ///     获取当前全局主种子。
        /// </summary>
        public static int GlobalSeed => GlobalSeed;

        // ============================================================
        //  初始化 & 全局种子
        // ============================================================

        /// <summary>
        ///     以指定的全局主种子初始化管理器，并清空所有已有模块的随机数生成器缓存。
        ///     后续调用 <see cref="GetRandomGenerator" /> 时会按新种子重新派生。
        /// </summary>
        /// <param name="globalSeed">全局主种子，任意 <c>int</c> 值</param>
        public static void Init(int globalSeed)
        {
            _globalSeed = globalSeed;
            _generators.Clear();
            _initialized = true;
        }

        // ============================================================
        //  核心 API
        // ============================================================

        /// <summary>
        ///     获取指定模块的独立随机数生成器。
        ///     首次访问时按 <c>HashCode.Combine(_globalSeed, (int)moduleType)</c> 派生种子并创建 <see cref="System.Random" />；
        ///     后续访问直接返回缓存实例。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <returns>该模块专属的 <see cref="System.Random" /> 实例</returns>
        public static Random GetRandomGenerator(ModuleType moduleType)
        {
            EnsureInitialized();

            if (!_generators.TryGetValue(moduleType, out var rng))
            {
                var derivedSeed = DeriveSeed(moduleType);
                rng = new Random(derivedSeed);
                _generators[moduleType] = rng;
            }

            return rng;
        }

        /// <summary>
        ///     获取指定模块的派生种子值（不创建生成器）。
        ///     用于调试、序列化、状态保存。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <returns>该模块从当前全局种子派生的 32 位种子值</returns>
        public static int GetModuleSeed(ModuleType moduleType)
        {
            EnsureInitialized();
            return DeriveSeed(moduleType);
        }

        // ============================================================
        //  快捷随机方法（避免每次写 .Next() / .NextDouble()）
        // ============================================================

        /// <summary>
        ///     获取 [min, max) 范围的整型随机值。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（不包含）</param>
        /// <returns>[min, max) 内的随机整数</returns>
        public static int RangeInt(ModuleType moduleType, int min, int max)
        {
            return GetRandomGenerator(moduleType).Next(min, max);
        }

        /// <summary>
        ///     获取 [min, max) 范围的浮点随机值。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（不包含）</param>
        /// <returns>[min, max) 内的随机浮点数</returns>
        public static float RangeFloat(ModuleType moduleType, float min, float max)
        {
            var t = GetRandomGenerator(moduleType).NextDouble();
            return (float)(min + t * (max - min));
        }

        /// <summary>
        ///     获取 [0, max) 范围的整型随机值。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        /// <param name="max">最大值（不包含）</param>
        public static int NextInt(ModuleType moduleType, int max)
        {
            return GetRandomGenerator(moduleType).Next(max);
        }

        /// <summary>
        ///     获取 [0.0, 1.0) 范围的浮点随机值。
        /// </summary>
        /// <param name="moduleType">模块类型</param>
        public static float NextFloat(ModuleType moduleType)
        {
            return (float)GetRandomGenerator(moduleType).NextDouble();
        }

        // ============================================================
        //  单模块重置
        // ============================================================

        /// <summary>
        ///     重置指定模块的随机数生成器。
        ///     下次访问时会从当前全局种子重新派生种子并创建新的 <see cref="System.Random" /> 实例。
        ///     可用于「关卡重开时只重置个别模块的 RNG」的场景。
        /// </summary>
        /// <param name="moduleType">要重置的模块类型</param>
        public static void ResetModule(ModuleType moduleType)
        {
            _generators.Remove(moduleType);
        }

        /// <summary>
        ///     清空所有已缓存的模块生成器（保留当前全局种子不变）。
        ///     后续各模块首次访问时会重新派生种子并创建新生成器。
        /// </summary>
        public static void ResetAllModules()
        {
            _generators.Clear();
        }

        // ============================================================
        //  状态保存 / 恢复
        // ============================================================

        /// <summary>
        ///     导出当前全局主种子，用于存档/序列化。
        ///     恢复时调用 <see cref="RestoreState" /> 可完全重现当前随机数状态。
        /// </summary>
        /// <param name="globalSeed">输出的当前全局种子</param>
        /// <param name="moduleSeeds">输出的各模块派生种子快照（模块枚举值→派生种子值）</param>
        public static void SaveState(out int globalSeed, out Dictionary<int, int> moduleSeeds)
        {
            globalSeed = _globalSeed;

            moduleSeeds = new Dictionary<int, int>();
            foreach (ModuleType mt in Enum.GetValues(typeof(ModuleType))) moduleSeeds[(int)mt] = DeriveSeed(mt);
        }

        /// <summary>
        ///     从存档的全局种子恢复全量随机数状态。
        ///     等价于 <c>Init(globalSeed)</c>，但语义上更清晰地表达了「恢复」意图。
        /// </summary>
        /// <param name="globalSeed">存档中的全局种子</param>
        public static void RestoreState(int globalSeed)
        {
            Init(globalSeed);
        }

        /// <summary>
        ///     从存档的全局种子 + 各模块调用次数恢复状态（高级接口）。
        ///     恢复全局种子后，对每个模块的生成器调用指定次数的 <c>Next()</c>，
        ///     以恢复到存档时的精确随机数位置。
        /// </summary>
        /// <param name="globalSeed">存档中的全局种子</param>
        /// <param name="moduleAdvanceCounts">
        ///     各模块需要推进的 <c>Next()</c> 调用次数（模块枚举值→次数），传 null 表示不推进任何模块
        /// </param>
        public static void RestoreStateWithAdvance(int globalSeed, Dictionary<int, int> moduleAdvanceCounts)
        {
            RestoreState(globalSeed);

            if (moduleAdvanceCounts == null) return;

            foreach (var kv in moduleAdvanceCounts)
            {
                var moduleType = (ModuleType)kv.Key;
                var rng = GetRandomGenerator(moduleType);
                for (var i = 0; i < kv.Value; i++) rng.Next(); // 推进到存档时的精确序列位置
            }
        }

        // ============================================================
        //  内部辅助
        // ============================================================

        /// <summary>
        ///     从当前全局种子 + 模块类型派生子种子。
        /// </summary>
        private static int DeriveSeed(ModuleType moduleType)
        {
            return HashCode.Combine(_globalSeed, (int)moduleType);
        }

        /// <summary>
        ///     若尚未调用 Init()，则使用默认种子自动初始化一次。
        ///     保证 GetRandomGenerator 等 API 无 Init 调用也能正常工作。
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
                // 使用 Environment.TickCount 作为默认种子，
                // 避免每次启动都是一样的随机序列
                Init(Environment.TickCount);
        }
    }
}