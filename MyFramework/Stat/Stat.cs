using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MyFramework.Stat
{
    [Serializable]
    public class Stat
    {
        [SerializeField] [LabelText("基础值")] private float baseValue = 1f;

        [SerializeField] [LabelText("线性叠加区")] private List<float> modifiers = new();

        [SerializeField] [LabelText("加乘区")] private List<float> addPercentModifiers = new();

        [SerializeField] [LabelText("累乘区")] private List<float> multiplyPercentModifiers = new();

        private float _finalValue;

        [NonSerialized] public bool needCalculate = true;

        public Stat()
        {
        }

        public Stat(float initialValue)
        {
            baseValue = initialValue;
        }

        public float GetValue()
        {
            if (!needCalculate) return _finalValue;

            var finalValue = baseValue;

            // 线性叠加区
            foreach (var modifier in modifiers) finalValue += modifier;

            // 加乘区（百分比加成）
            if (addPercentModifiers.Count > 0)
                finalValue *= 1f + GetAddPercentTotal();

            // 累乘区
            if (multiplyPercentModifiers.Count > 0)
                foreach (var percent in multiplyPercentModifiers)
                    finalValue *= 1f + percent;

            _finalValue = finalValue;
            needCalculate = false;

            return _finalValue;
        }

        public void SetDefaultValue(float value)
        {
            baseValue = value;
            needCalculate = true;
        }

        public void AddModifier(float modifier)
        {
            modifiers.Add(modifier);
            needCalculate = true;
        }

        public void RemoveModifier(float modifier)
        {
            modifiers.Remove(modifier);
            needCalculate = true;
        }

        public void AddAddPercentModifier(float percentModifier)
        {
            addPercentModifiers.Add(percentModifier);
            needCalculate = true;
        }

        public void RemoveAddPercentModifier(float percentModifier)
        {
            addPercentModifiers.Remove(percentModifier);
            needCalculate = true;
        }

        public void AddMultiplyPercentModifier(float percentModifier)
        {
            multiplyPercentModifiers.Add(percentModifier);
            needCalculate = true;
        }

        public void RemoveMultiplyPercentModifier(float percentModifier)
        {
            multiplyPercentModifiers.Remove(percentModifier);
            needCalculate = true;
        }

        public float GetAddPercentTotal()
        {
            var total = 0f;
            foreach (var percent in addPercentModifiers) total += percent;
            return total;
        }

        public void Reset()
        {
            modifiers.Clear();
            addPercentModifiers.Clear();
            multiplyPercentModifiers.Clear();
            needCalculate = true;
        }
    }
}
