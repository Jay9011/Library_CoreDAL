using System;
using System.Collections.Generic;
using CoreDAL.Configuration.Interface;
using CoreDAL.Configuration.Models;

namespace CoreDAL.Configuration
{
    /// <summary>
    /// 데이터베이스 연결 정보를 반환해주는 팩토리
    /// </summary>
    public class DbConnectionFactory
    {
        public static IDbConnectionInfo CreateConnectionInfo(DatabaseType dbType, Dictionary<string, string> settings)
        {
            switch (dbType)
            {
                case DatabaseType.MSSQL:
                    return new MsSqlConnectionInfo().LoadFromSettings(settings);
                case DatabaseType.ORACLE:
                    return new OracleConnectionInfo().LoadFromSettings(settings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType), dbType, null);
            }
        }
    }
}
