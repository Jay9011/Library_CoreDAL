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
                    // DBNull.Value는 null로 변환
                    if (value == null || value == DBNull.Value)
                    {
                        property.SetValue(this, null);
                        return;
                    }

                    // Nullable 타입의 경우 내부 타입으로 변환
                    var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    var convertedValue = Convert.ChangeType(value, targetType);
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
