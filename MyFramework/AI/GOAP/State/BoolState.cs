namespace MyFramework.AI.GOAP.State
{
    public class BoolState : GOAPStateBase<BoolState,bool,BoolStateComparer>
    {
        public override bool EqualsValue(BoolState other)
        {
            return this.value == other.value;
        }

        public override bool CompareCompareForPrecondition(BoolStateComparer comparer)
        {
            switch (comparer.value)
            {
                case BoolValue.是:
                    return value;
                case BoolValue.否:
                    return !value;
            }
            return false;
        }

        public override bool CompareForEffect(BoolStateComparer comparer)
        {
            return CompareCompareForPrecondition(comparer);
        }

        public override void ApplyEffect(BoolStateComparer comparer)
        {
            switch (comparer.value)
            {
                case BoolValue.是:
                    value = true;
                    break;
                case BoolValue.否:
                    value = false;
                    break;
            }
        }
    }

    public class BoolStateComparer : GOAPStateComparer<BoolState, BoolStateComparer>
    {
        public BoolValue value;

        public override bool EqualsComparer(BoolStateComparer other)
        {
            return this.value == other.value;
        }
    
        public bool GetValue()
        {
            switch (value)
            {
                case BoolValue.是:
                    return true;
                case BoolValue.否:
                    return false;
                default:
                    return false;
            }
        }
    }

    public enum BoolValue
    {
        是,
        否
    }
}