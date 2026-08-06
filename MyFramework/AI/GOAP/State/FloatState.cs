using System;

namespace MyFramework.AI.GOAP.State
{
   public class FloatState : GOAPStateBase<FloatState,float,FloatStateComparer>
   {
      public override bool EqualsValue(FloatState other)
      {
         return this.value == other.value;
      }

      public override bool CompareCompareForPrecondition(FloatStateComparer comparer)
      {
         switch (comparer.compareSymbol)
         {
            case NumberCompareSymbol.大于:
               return value > comparer.value;
            case NumberCompareSymbol.小于:
               return value < comparer.value;
            case NumberCompareSymbol.大于等于:
               return value >= comparer.value;
            case NumberCompareSymbol.小于等于:
               return value <= comparer.value;
            case NumberCompareSymbol.提升即可:
               return value > 0;
            case NumberCompareSymbol.下降即可:
               return value < 0;
            case NumberCompareSymbol.等于:
               return Math.Abs(value - comparer.value) < 0.001f;
         }
         return false;
      }

      public override bool CompareForEffect(FloatStateComparer comparer)
      {
         switch (comparer.compareSymbol)
         {
            case NumberCompareSymbol.大于:
               return value > comparer.value;
            case NumberCompareSymbol.小于:
               return value < comparer.value;
            case NumberCompareSymbol.大于等于:
               return value >= comparer.value;
            case NumberCompareSymbol.小于等于:
               return value <= comparer.value;
            case NumberCompareSymbol.提升即可:
               return false;
            case NumberCompareSymbol.下降即可:
               return false;
            case NumberCompareSymbol.等于:
               return Math.Abs(value - comparer.value) < 0.001f;
         }
         return false;
      }

      public override void ApplyEffect(FloatStateComparer comparer)
      {
         switch (comparer.compareSymbol)
         {
            case NumberCompareSymbol.等于:
               value = comparer.value;
               break;
            default:
               value += comparer.value;
               break;
         }
      }
   }

   public class FloatStateComparer : GOAPStateComparer<FloatState, FloatStateComparer>
   {
      public NumberCompareSymbol compareSymbol;
      public float value;
      public override bool EqualsComparer(FloatStateComparer other) 
      {
         return compareSymbol == other.compareSymbol;
      }
   }

   public enum NumberCompareSymbol //倾向器
   {
      大于,
      小于,
      大于等于,
      小于等于,
      提升即可,
      下降即可,
      等于
   }
}