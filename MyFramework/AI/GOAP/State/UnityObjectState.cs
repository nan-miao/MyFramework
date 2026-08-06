namespace MyFramework.AI.GOAP.State
{
    public class UnityObjectState : GOAPStateBase<UnityObjectState,UnityEngine.Object,UnityObjectStateComparer>
    {
        public override bool EqualsValue(UnityObjectState other)
        {
            return this.value == other.value;
        }

        public override bool CompareCompareForPrecondition(UnityObjectStateComparer comparer)
        {
            switch (comparer.symbol)
            {
                case BoolValue.是:
                    return this.value == comparer.value;
                case BoolValue.否:
                    return this.value != comparer.value;
            }
        
            return this.value == comparer.value;
        }

        public override bool CompareForEffect(UnityObjectStateComparer comparer)
        {
            return CompareCompareForPrecondition(comparer);
        }

        public override void ApplyEffect(UnityObjectStateComparer comparer)
        {
            if (comparer.symbol == BoolValue.是)
            { 
                this.value = comparer.value;
            }
        }
    }

    public class UnityObjectStateComparer : GOAPStateComparer<UnityObjectState, UnityObjectStateComparer>
    {
        public BoolValue symbol;//用于判断对象是否相等
        public UnityEngine.Object value;

        public override bool EqualsComparer(UnityObjectStateComparer other)
        {
            switch (other.symbol)
            {
                case BoolValue.是:
                    return this.value == other.value;
                case BoolValue.否:
                    return this.value != other.value;
            }
            return false;
        }
    }

    public enum UnityObjectSymbol
    {
        是,
        否,
        为空,
        不为空,
    }
}