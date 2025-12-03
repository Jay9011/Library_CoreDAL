using System;
using System.Collections.Generic;
using CoreDAL.DALs;
using CoreDAL.DALs.Interface;

namespace CoreDAL.Configuration
{
    /// <summary>
    /// 데이터베이스 DAL 팩토리
    /// </summary>
    public class DbDALFactory
    {
        private static readonly Dictionary<DatabaseType, Lazy<ICoreDAL>> _instance = new Dictionary<DatabaseType, Lazy<ICoreDAL>>();
        private static readonly object _lock = new object();

        /// <summary>
        /// CoreDAL에 대한 인스턴스를 생성
        /// </summary>
        /// <param name="dbType">데이터베이스 타입</param>
        /// <returns></returns>
        public static ICoreDAL CreateCoreDal(DatabaseType dbType)
        {
            lock (_lock)
            {
                if (!_instance.ContainsKey(dbType))
                {
                    _instance[dbType] = new Lazy<ICoreDAL>(() => CreateNewInstance(dbType));
                }

                return _instance[dbType].Value;
            }
        }

        /// <summary>
        /// 데이터베이스 타입에 따라 새로운 인스턴스를 생성
        /// </summary>
        /// <param name="dbType">데이터베이스 타입</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static ICoreDAL CreateNewInstance(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.MSSQL:
                    return SqlServerDAL.Instance;
                case DatabaseType.ORACLE:
                    return OracleDAL.Instance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType));
            }
        }
    }
}
