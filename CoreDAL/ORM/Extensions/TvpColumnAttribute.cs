using System;

namespace CoreDAL.ORM.Extensions
{
    /// <summary>
    /// Table-Valued Parameter(TVP)의 컬럼 매핑을 위한 Attribute
    /// 프로퍼티에 적용하여 DataTable 컬럼과 매핑합니다.
    /// </summary>
    /// <example>
    /// <code>
    /// public class UserTvpItem
    /// {
    ///     [TvpColumn("UserId", Order = 0)]
    ///     public int Id { get; set; }
    ///     
    ///     [TvpColumn("UserName", Order = 1)]
    ///     public string Name { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TvpColumnAttribute : Attribute
    {
        /// <summary>
        /// DataTable 컬럼명 (null이면 프로퍼티 이름 사용)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 컬럼 순서 (TVP의 컬럼 순서와 일치해야 함)
        /// </summary>
        public int Order { get; set; } = int.MaxValue;

        /// <summary>
        /// 컬럼의 최대 크기 (문자열 등에 사용)
        /// </summary>
        public int MaxLength { get; set; } = -1;

        /// <summary>
        /// NULL 허용 여부
        /// </summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// TvpColumnAttribute 생성자
        /// </summary>
        /// <param name="name">DataTable 컬럼명 (null이면 프로퍼티 이름 사용)</param>
        public TvpColumnAttribute(string name = null)
        {
            Name = name;
        }
    }
}
