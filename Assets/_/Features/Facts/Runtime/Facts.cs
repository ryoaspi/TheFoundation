using System;

namespace TheFundation.Runtime.Data
{
    public interface IFact
    {
        object GetObjectValue();
        void SetObjectValue(object value);
        bool IsPersistent { get; set; }
        bool IsSaved { get; set; }
        
        Type valueType { get; }
        
    }
    public class Facts<T> : IFact
    {
        public T Value;
        public bool IsPersistent { get; set; }
        public bool IsSaved { get; set; }
        public Type valueType { get; }
        
        public Facts(T value, bool isPersistent, bool isSaved = true)
        {
            Value = value;
            IsPersistent = isPersistent;
            IsSaved = isSaved;
            valueType = typeof(T);
        }
        
        public object GetObjectValue() => Value;

        public void SetObjectValue(object value)
        {
            if (value is T castedValue)
                Value = castedValue;
            else
                throw new ArgumentException($"Cannot cast {value.GetType()} to {typeof(T)}", nameof(value));
        }
        

    }

    public interface IFactDeclaration
    {
        void DeclareFacts(FactDictionary facts);
            
    }
}