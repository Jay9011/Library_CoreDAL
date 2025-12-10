using System.Data;
using CoreDAL.Configuration;
using CoreDAL.Configuration.Interface;
using CoreDAL.Configuration.Models;
using CoreDAL.DALs.Interface;
using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;
using Xunit.Abstractions;

namespace SECUiDEA.CoreDAL.TestProject
{
    /// <summary>
    /// TVP (Table-Valued Parameter) 통합 테스트
    /// 실제 DB 연결이 필요합니다.
    /// </summary>
    public class TvpIntegrationTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public TvpIntegrationTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #region 테스트 설정

        /// <summary>
        /// 테스트용 DB 연결 정보
        /// </summary>
        private static IDbConnectionInfo CreateTestConnectionInfo()
        {
            return new MsSqlConnectionInfo
            {
                Server = "localhost",
                Database = "TEST",
                UserId = "sa",
                Password = "s1access!",
                Port = 1433,
                IntegratedSecurity = false
            };
        }

        private static ICoreDAL GetTestDAL()
        {
            return DbDALFactory.CreateCoreDal(DatabaseType.MSSQL);
        }

        #endregion

        #region 테스트용 엔티티 클래스

        /// <summary>
        /// TVP 전용 클래스
        /// </summary>
        public class UserTvpItem
        {
            [TvpColumn("UserId", Order = 0)]
            public int Id { get; set; }

            [TvpColumn("UserName", Order = 1)]
            public string Name { get; set; }
        }

        /// <summary>
        /// DbParameter와 TvpColumn 혼합 사용 클래스
        /// </summary>
        public class UserEntity : SQLParam
        {
            [DbParameter]
            public int? SEQ { get; set; }

            [DbParameter]
            [TvpColumn("ID", Order = 0)]
            public string ID { get; set; }

            [DbParameter]
            public string PW { get; set; }

            [DbParameter]
            [TvpColumn("NAME", Order = 1)]
            public string NAME { get; set; }
        }

        #endregion

        #region TVP 프로시저 실행 테스트

        /// <summary>
        /// TVP로 대량 INSERT 테스트
        /// 
        /// 필요한 DB 객체:
        /// CREATE TYPE dbo.UserListType AS TABLE (UserId INT, UserName NVARCHAR(100));
        /// CREATE PROCEDURE USP_TVP_INSERT_USERS @Users dbo.UserListType READONLY AS BEGIN ... END
        /// </summary>
        [Fact]
        public async Task TvpInsert_WithDataTable_ShouldInsertMultipleRows()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testPrefix = $"TVP_TEST_{Guid.NewGuid():N}";

            var users = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = $"{testPrefix}_User1" },
                new UserTvpItem { Id = 2, Name = $"{testPrefix}_User2" },
                new UserTvpItem { Id = 3, Name = $"{testPrefix}_User3" }
            };

            // List → DataTable 변환
            DataTable userTable = users.ToDataTable();

            _outputHelper.WriteLine($"DataTable 생성됨: {userTable.Rows.Count}행, {userTable.Columns.Count}컬럼");
            _outputHelper.WriteLine($"컬럼: {string.Join(", ", userTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

            try
            {
                // Act
                var result = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_INSERT_USERS",
                    new Dictionary<string, object> { { "Users", userTable } },
                    isReturn: true
                );

                // Assert
                Assert.True(result.IsSuccess, $"TVP INSERT 실패: {result.Message}");
                Assert.Equal(3, result.ReturnValue); // 3개 삽입됨

                _outputHelper.WriteLine($"TVP INSERT 성공: {result.ReturnValue}행 삽입됨");
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_DELETE_USERS",
                    new Dictionary<string, object> { { "NamePrefix", testPrefix } }
                );
            }
        }

        /// <summary>
        /// TVP로 대량 UPDATE 후 결과 테이블 반환 테스트
        /// 각 행별 처리 결과를 확인할 수 있습니다.
        /// </summary>
        [Fact]
        public async Task TvpUpdate_WithResultTable_ShouldReturnPerRowResults()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testPrefix = $"TVP_UPD_{Guid.NewGuid():N}";

            // 먼저 테스트 데이터 INSERT
            for (int i = 1; i <= 3; i++)
            {
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_TEST_INSERT",
                    new Dictionary<string, object>
                    {
                        { "Id", i },
                        { "Name", $"{testPrefix}_User{i}" }
                    }
                );
            }

            // UPDATE할 데이터
            var updates = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = $"{testPrefix}_Updated1" },
                new UserTvpItem { Id = 2, Name = $"{testPrefix}_Updated2" },
                new UserTvpItem { Id = 999, Name = "존재하지않는사용자" }  // 실패 케이스
            };

            DataTable updateTable = updates.ToDataTable();

            try
            {
                // Act
                var result = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_UPDATE_USERS_WITH_RESULT",
                    new Dictionary<string, object> { { "Users", updateTable } },
                    isReturn: true
                );

                // Assert
                Assert.True(result.IsSuccess, $"TVP UPDATE 실패: {result.Message}");
                Assert.NotNull(result.DataSet);
                Assert.True(result.DataSet.Tables.Count > 0);

                // 결과 테이블에서 각 행별 결과 확인
                var resultTable = result.DataSet.Tables[0];
                _outputHelper.WriteLine($"처리 결과 ({resultTable.Rows.Count}행):");

                foreach (DataRow row in resultTable.Rows)
                {
                    var userId = row["UserId"];
                    var success = row["Success"];
                    var errorMessage = row["ErrorMessage"];
                    _outputHelper.WriteLine($"  UserId: {userId}, Success: {success}, Error: {errorMessage}");
                }

                // 성공 2건, 실패 1건 (존재하지 않는 사용자)
                var successCount = resultTable.AsEnumerable().Count(r => Convert.ToBoolean(r["Success"]));
                Assert.Equal(2, successCount);
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_DELETE_USERS",
                    new Dictionary<string, object> { { "NamePrefix", testPrefix } }
                );
            }
        }

        /// <summary>
        /// 혼합 Attribute 클래스로 단건 INSERT + TVP UPDATE 테스트
        /// </summary>
        [Fact]
        public async Task MixedAttributes_SingleInsertAndBulkUpdate_ShouldWork()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testId = $"MIXED_{Guid.NewGuid():N}";

            // 단건 INSERT용 (DbParameter 사용)
            var user = new UserEntity
            {
                SEQ = 1,
                ID = testId,
                PW = "password123",
                NAME = "원본이름"
            };

            try
            {
                // Act 1: 단건 INSERT (DbParameter로 모든 필드 전달)
                var insertResult = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_MIXED_INSERT_USER",
                    user,
                    isReturn: true
                );

                Assert.True(insertResult.IsSuccess, $"단건 INSERT 실패: {insertResult.Message}");
                _outputHelper.WriteLine($"단건 INSERT 성공");

                // Act 2: TVP UPDATE (TvpColumn이 있는 ID, NAME만 전달)
                var updates = new List<UserEntity>
                {
                    new UserEntity { ID = testId, NAME = "수정된이름" }
                };

                DataTable updateTable = updates.ToDataTable();
                _outputHelper.WriteLine($"UPDATE용 DataTable 컬럼: {string.Join(", ", updateTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");

                var updateResult = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_MIXED_UPDATE_USERS",
                    new Dictionary<string, object> { { "Users", updateTable } },
                    isReturn: true
                );

                Assert.True(updateResult.IsSuccess, $"TVP UPDATE 실패: {updateResult.Message}");
                Assert.Equal(1, updateResult.ReturnValue);
                _outputHelper.WriteLine($"TVP UPDATE 성공: {updateResult.ReturnValue}행 수정됨");

                // Assert: 데이터 확인
                var selectResult = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_MIXED_SELECT_USER",
                    new Dictionary<string, object> { { "ID", testId } },
                    isReturn: true
                );

                Assert.True(selectResult.IsSuccess);
                Assert.NotNull(selectResult.DataSet);
                Assert.Equal(1, selectResult.DataSet.Tables[0].Rows.Count);
                Assert.Equal("수정된이름", selectResult.DataSet.Tables[0].Rows[0]["NAME"]);

                _outputHelper.WriteLine($"혼합 Attribute 테스트 성공: 단건 INSERT + TVP UPDATE 완료");
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_MIXED_DELETE_USER",
                    new Dictionary<string, object> { { "ID", testId } }
                );
            }
        }

        #endregion

        #region 트랜잭션 + TVP 테스트

        /// <summary>
        /// 트랜잭션 내에서 TVP 사용 테스트
        /// </summary>
        [Fact]
        public async Task Transaction_WithTvp_ShouldWorkCorrectly()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testPrefix = $"TX_TVP_{Guid.NewGuid():N}";

            var users = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = $"{testPrefix}_User1" },
                new UserTvpItem { Id = 2, Name = $"{testPrefix}_User2" }
            };

            DataTable userTable = users.ToDataTable();

            try
            {
                // Act - 트랜잭션 내에서 TVP INSERT
                using (var tx = dal.BeginTransaction(connectionInfo.ToConnectionString()))
                {
                    var result = tx.ExecuteProcedure(
                        "USP_TVP_INSERT_USERS",
                        new Dictionary<string, object> { { "Users", userTable } },
                        isReturn: true
                    );

                    Assert.True(result.IsSuccess, $"트랜잭션 내 TVP INSERT 실패: {result.Message}");
                    Assert.Equal(2, result.ReturnValue);

                    tx.Commit();
                }

                // Assert - 커밋 후 데이터 확인
                var selectResult = await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_SELECT_USERS",
                    new Dictionary<string, object> { { "NamePrefix", testPrefix } },
                    isReturn: true
                );

                Assert.True(selectResult.IsSuccess);
                Assert.NotNull(selectResult.DataSet);
                Assert.Equal(2, selectResult.DataSet.Tables[0].Rows.Count);

                _outputHelper.WriteLine($"트랜잭션 + TVP 테스트 성공: {selectResult.DataSet.Tables[0].Rows.Count}행 확인됨");
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TVP_DELETE_USERS",
                    new Dictionary<string, object> { { "NamePrefix", testPrefix } }
                );
            }
        }

        /// <summary>
        /// 트랜잭션 롤백 시 TVP INSERT도 롤백되는지 테스트
        /// </summary>
        [Fact]
        public async Task Transaction_Rollback_WithTvp_ShouldRollback()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testPrefix = $"TX_TVP_RB_{Guid.NewGuid():N}";

            var users = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = $"{testPrefix}_User1" },
                new UserTvpItem { Id = 2, Name = $"{testPrefix}_User2" }
            };

            DataTable userTable = users.ToDataTable();

            // Act - 트랜잭션 내에서 TVP INSERT 후 롤백
            using (var tx = dal.BeginTransaction(connectionInfo.ToConnectionString()))
            {
                var result = tx.ExecuteProcedure(
                    "USP_TVP_INSERT_USERS",
                    new Dictionary<string, object> { { "Users", userTable } },
                    isReturn: true
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(2, result.ReturnValue);

                tx.Rollback();
            }

            // Assert - 롤백 후 데이터 없어야 함
            var selectResult = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TVP_SELECT_USERS",
                new Dictionary<string, object> { { "NamePrefix", testPrefix } },
                isReturn: true
            );

            Assert.True(selectResult.IsSuccess);
            if (selectResult.DataSet != null && selectResult.DataSet.Tables.Count > 0)
            {
                Assert.Equal(0, selectResult.DataSet.Tables[0].Rows.Count);
            }

            _outputHelper.WriteLine($"트랜잭션 롤백 + TVP 테스트 성공: 데이터가 롤백됨");
        }

        #endregion
    }
}
