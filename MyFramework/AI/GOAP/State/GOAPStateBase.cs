using System;

namespace MyFramework.AI.GOAP.State
{
   public abstract  class GOAPStateBase //类似于接口
   {
      public abstract bool EqualsValue(GOAPStateBase other);
      public abstract void SetValue(GOAPStateBase other);
      public abstract GOAPStateBase Copy();
      public abstract GOAPStateComparer GetComparer(); //获取比较器
      public abstract Type GetComparerType(); //获取比较器类型
      public abstract bool CompareForPrecondition(GOAPStateComparer comparer); //调用比较器里的比较函数
      public abstract bool CompareForEffect(GOAPStateComparer comparer); //
      public abstract void ApplyEffect(GOAPStateComparer comparer); //应用影响
   }

//方便子类实现
   public abstract class GOAPStateBase<T, V,C> : GOAPStateBase where T : GOAPStateBase<T, V,C>,new() where C:GOAPStateComparer,new()
   {
      public V value;
      public abstract bool EqualsValue(T other);
   
      //保证传入的GOAPStateBase最终通过T类型比较 （类型安全）
      public override bool EqualsValue(GOAPStateBase other)
      {
         return EqualsValue((T)other);
      }

      //该部分设置Value都是T类型不存在装箱问题
      public virtual void SetValue(T other)
      {
         this.value = other.value;
      }

      public override void SetValue(GOAPStateBase other)
      {
         SetValue((T)other);
      }

      public virtual void SetValue(V value)
      {
         this.value = value;
      }
   
      public virtual V GetValue()
      {
         return value;
      }

      public override GOAPStateBase Copy()
      {
         return new T() {value = this.value};
      }

      public virtual C GeStateComparer()
      {
         return new C();
      }

      public override GOAPStateComparer GetComparer()
      {
         return GeStateComparer();
      }

      public abstract bool CompareCompareForPrecondition(C comparer);
   
      public override bool CompareForPrecondition(GOAPStateComparer comparer)
      {
         return CompareCompareForPrecondition((C)comparer);
      } 
   
      public abstract bool CompareForEffect(C comparer);
   
      public override bool CompareForEffect(GOAPStateComparer comparer)
      {
         return CompareForEffect((C)comparer);
      }
   
      public abstract void ApplyEffect(C comparer);

      public override void ApplyEffect(GOAPStateComparer comparer)
      {
         ApplyEffect((C)comparer);
      }

      public override Type GetComparerType()
      {
         return typeof(C);
      }
   }
}