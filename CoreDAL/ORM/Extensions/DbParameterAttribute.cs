using System;
using System.Data;

namespace CoreDAL.ORM.Extensions
{
    [AttributeUsage((AttributeTargets.Property))]
    public class DbParameterAttribute : Attribute
    {
        public string Name { get; set; }
        public DbType DbType { get; set; }
        public ParameterDirection Direction { get; set; }
        public int? Size { get; set; }
        public byte? Precision { get; set; }
        public byte? Scale { get; set; }
        public bool IsNullable { get; set; } = true;

        public DbParameterAttribute(string name = null, DbType dbType = DbType.AnsiString, ParameterDirection direction = ParameterDirection.Input)
        {
            Name = name;
            DbType = dbType;
            Direction = direction;
        }
    }
}
