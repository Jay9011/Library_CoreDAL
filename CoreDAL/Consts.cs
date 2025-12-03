namespace SECUiDEA.CoreDAL
{
    public static class Consts
    {
        #region 공통

        public const string DatabaseKey = "Database";
        public const string DBTypeKey = "DbType";
        public const string PortKey = "Port";
        public const string UserIdKey = "UserId";
        public const string PasswordKey = "Password";

        #endregion

        #region SQL Server

        public const string ServerKey = "Server";
        public const string IntegratedSecurityKey = "IntegratedSecurity";
        public const string TrustServerCertificateKey = "TrustServerCertificate";

        #endregion

        #region Oracle

        public const string HostKey = "Host";
        public const string ServiceNameKey = "ServiceName";
        public const string ProtocolKey = "Protocol";

        #endregion
    }
}
