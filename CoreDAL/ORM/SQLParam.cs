using System;
using CoreDAL.ORM.Interfaces;

namespace CoreDAL.ORM
{
    public class SQLParam : ISQLParam
    {
        public virtual void SetOutputParameterValue(string propertyName, object value)
        {
            var property = GetType().GetProperty(propertyName);
            if (property != null &&
                property.CanWrite)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(this, convertedValue);
                }
                catch (InvalidCastException)    // 변환 실패 시 그냥 원본 값 할당
                {
                    property.SetValue(this, value);
                }
            }
        }
    }
}
