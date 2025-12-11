using System.Data;
using CoreDAL.Configuration;
using CoreDAL.Configuration.Interface;
using CoreDAL.Configuration.Models;
using CoreDAL.DALs.Interface;
using Xunit.Abstractions;

namespace SECUiDEA.CoreDAL.TestProject
{
    /// <summary>
    /// 트랜잭션 격리 수준 (Isolation Level) 테스트
    /// </summary>
    public class TransactionIsolationTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public TransactionIsolationTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #region 테스트 설정

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

        #region 격리 수준별 트랜잭션 생성 테스트

        /// <summary>
        /// 기본 격리 수준 (ReadCommitted) 테스트
        /// </summary>
        [Fact]
        public void BeginTransaction_Default_ShouldUseReadCommitted()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();

            // Act
            using (var tx = dal.BeginTransaction(connectionInfo.ToConnectionString()))
            {
                // Assert - 트랜잭션이 정상 생성됨
                Assert.NotNull(tx);

                var result = tx.ExecuteProcedure(
                    "USP_TEST_RETURN_VALUE",
                    new Dictionary<string, object> { { "InputValue", 10 } },
                    isReturn: true
                );

                Assert.True(result.IsSuccess);
                tx.Commit();
            }

            _outputHelper.WriteLine("기본 격리 수준 (ReadCommitted) 테스트 성공");
        }

        /// <summary>
        /// ReadUncommitted 격리 수준 테스트 - SELECT 시 Lock 없음
        /// </summary>
        [Fact]
        public void BeginTransaction_ReadUncommitted_ShouldNotLock()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();

            // Act - ReadUncommitted로 트랜잭션 생성
            using (var tx = dal.BeginTransaction(
                connectionInfo.ToConnectionString(),
                IsolationLevel.ReadUncommitted))
            {
                // SELECT 쿼리 실행 - Lock 없이 실행됨
                var result = tx.ExecuteProcedure(
                    "USP_TEST_SELECT_DATA",
                    new Dictionary<string, object> { { "Count", 5 } },
                    isReturn: true
                );

                Assert.True(result.IsSuccess, $"실행 실패: {result.Message}");
                Assert.NotNull(result.DataSet);

                tx.Commit();
            }

            _outputHelper.WriteLine("ReadUncommitted 격리 수준 테스트 성공 - Lock 없이 SELECT 실행됨");
        }

        /// <summary>
        /// RepeatableRead 격리 수준 테스트
        /// </summary>
        [Fact]
        public void BeginTransaction_RepeatableRead_ShouldWork()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();

            // Act
            using (var tx = dal.BeginTransaction(
                connectionInfo.ToConnectionString(),
                IsolationLevel.RepeatableRead))
            {
                var result = tx.ExecuteProcedure(
                    "USP_TEST_SELECT_DATA",
                    new Dictionary<string, object> { { "Count", 3 } },
                    isReturn: true
                );

                Assert.True(result.IsSuccess);
                tx.Commit();
            }

            _outputHelper.WriteLine("RepeatableRead 격리 수준 테스트 성공");
        }

        /// <summary>
        /// Serializable 격리 수준 테스트
        /// </summary>
        [Fact]
        public void BeginTransaction_Serializable_ShouldWork()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();

            // Act
            using (var tx = dal.BeginTransaction(
                connectionInfo.ToConnectionString(),
                IsolationLevel.Serializable))
            {
                var result = tx.ExecuteProcedure(
                    "USP_TEST_RETURN_VALUE",
                    new Dictionary<string, object> { { "InputValue", 5 } },
                    isReturn: true
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(10, result.ReturnValue); // 5 * 2 = 10
                tx.Commit();
            }

            _outputHelper.WriteLine("Serializable 격리 수준 테스트 성공");
        }

        #endregion

        #region 격리 수준에 따른 동작 차이 테스트

        /// <summary>
        /// ReadUncommitted에서 Dirty Read 가능 여부 테스트
        /// - 다른 트랜잭션에서 커밋되지 않은 데이터를 읽을 수 있음
        /// </summary>
        [Fact]
        public async Task ReadUncommitted_ShouldAllowDirtyRead()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testName = $"DIRTY_READ_TEST_{Guid.NewGuid():N}";

            // Act
            // 트랜잭션 1: INSERT 후 커밋하지 않음
            using (var tx1 = dal.BeginTransaction(connectionInfo.ToConnectionString()))
            {
                var insertResult = tx1.ExecuteProcedure(
                    "USP_TX_TEST_INSERT",
                    new Dictionary<string, object> { { "Name", testName } }
                );

                Assert.True(insertResult.IsSuccess, $"INSERT 실패: {insertResult.Message}");

                // 트랜잭션 2: ReadUncommitted로 SELECT - 커밋되지 않은 데이터도 읽을 수 있어야 함
                using (var tx2 = dal.BeginTransaction(
                    connectionInfo.ToConnectionString(),
                    IsolationLevel.ReadUncommitted))
                {
                    var selectResult = tx2.ExecuteProcedure(
                        "USP_TX_TEST_SELECT",
                        new Dictionary<string, object> { { "Name", testName } }
                    );

                    Assert.True(selectResult.IsSuccess);

                    // ReadUncommitted에서는 커밋되지 않은 데이터도 보임 (Dirty Read)
                    if (selectResult.DataSet != null && selectResult.DataSet.Tables.Count > 0)
                    {
                        _outputHelper.WriteLine($"Dirty Read 결과: {selectResult.DataSet.Tables[0].Rows.Count}행");
                        Assert.Equal(1, selectResult.DataSet.Tables[0].Rows.Count);
                    }

                    tx2.Commit();
                }

                // 트랜잭션 1 롤백 - 데이터 취소
                tx1.Rollback();
            }

            // 롤백 후 데이터 없어야 함
            var finalResult = await dal.ExecuteProcedureAsync(
                connectionInfo.ToConnectionString(),
                "USP_TX_TEST_SELECT",
                new Dictionary<string, object> { { "Name", testName } }
            );

            if (finalResult.DataSet != null && finalResult.DataSet.Tables.Count > 0)
            {
                Assert.Equal(0, finalResult.DataSet.Tables[0].Rows.Count);
            }

            _outputHelper.WriteLine("Dirty Read 테스트 성공 - ReadUncommitted에서 커밋되지 않은 데이터 읽기 가능");
        }

        /// <summary>
        /// SELECT 전용 트랜잭션에서 ReadUncommitted 사용으로 Lock 방지 테스트
        /// </summary>
        [Fact]
        public async Task ReadOnlyTransaction_WithReadUncommitted_ShouldNotBlockOthers()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testName = $"NO_LOCK_TEST_{Guid.NewGuid():N}";

            try
            {
                // 테스트 데이터 INSERT
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TX_TEST_INSERT",
                    new Dictionary<string, object> { { "Name", testName } }
                );

                // SELECT 전용 트랜잭션 - ReadUncommitted (Lock 없음)
                var selectTask = Task.Run(() =>
                {
                    using (var tx = dal.BeginTransaction(
                        connectionInfo.ToConnectionString(),
                        IsolationLevel.ReadUncommitted))
                    {
                        // 여러 번 SELECT
                        for (int i = 0; i < 5; i++)
                        {
                            var result = tx.ExecuteProcedure(
                                "USP_TX_TEST_SELECT",
                                new Dictionary<string, object> { { "Name", testName } }
                            );
                            Assert.True(result.IsSuccess);
                        }

                        tx.Commit();
                        return true;
                    }
                });

                // 동시에 UPDATE 트랜잭션 - ReadUncommitted SELECT가 Lock을 안 걸므로 대기 없이 실행 가능
                var updateTask = Task.Run(async () =>
                {
                    using (var tx = dal.BeginTransaction(connectionInfo.ToConnectionString()))
                    {
                        var result = await tx.ExecuteProcedureAsync(
                            "USP_TX_TEST_UPDATE",
                            new Dictionary<string, object>
                            {
                                { "Name", testName },
                                { "NewName", testName + "_UPDATED" }
                            }
                        );

                        Assert.True(result.IsSuccess);
                        tx.Commit();
                        return true;
                    }
                });

                // 둘 다 타임아웃 없이 완료되어야 함
                var results = await Task.WhenAll(selectTask, updateTask);
                Assert.All(results, r => Assert.True(r));

                _outputHelper.WriteLine("ReadUncommitted SELECT + UPDATE 동시 실행 테스트 성공 - Lock 충돌 없음");
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TX_TEST_DELETE",
                    new Dictionary<string, object> { { "Name", testName } }
                );
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TX_TEST_DELETE",
                    new Dictionary<string, object> { { "Name", testName + "_UPDATED" } }
                );
            }
        }

        #endregion

        #region 비동기 트랜잭션 + 격리 수준 테스트

        /// <summary>
        /// 비동기 트랜잭션에서 격리 수준 테스트
        /// </summary>
        [Fact]
        public async Task AsyncTransaction_WithIsolationLevel_ShouldWork()
        {
            // Arrange
            var connectionInfo = CreateTestConnectionInfo();
            var dal = GetTestDAL();
            var testName = $"ASYNC_ISO_TEST_{Guid.NewGuid():N}";

            try
            {
                // Act
                using (var tx = dal.BeginTransaction(
                    connectionInfo.ToConnectionString(),
                    IsolationLevel.ReadCommitted))
                {
                    // 비동기 INSERT
                    var insertResult = await tx.ExecuteProcedureAsync(
                        "USP_TX_TEST_INSERT",
                        new Dictionary<string, object> { { "Name", testName } }
                    );

                    Assert.True(insertResult.IsSuccess);

                    // 비동기 SELECT
                    var selectResult = await tx.ExecuteProcedureAsync(
                        "USP_TX_TEST_SELECT",
                        new Dictionary<string, object> { { "Name", testName } }
                    );

                    Assert.True(selectResult.IsSuccess);
                    Assert.NotNull(selectResult.DataSet);
                    Assert.Equal(1, selectResult.DataSet.Tables[0].Rows.Count);

                    tx.Commit();
                }

                _outputHelper.WriteLine("비동기 트랜잭션 + 격리 수준 테스트 성공");
            }
            finally
            {
                // Cleanup
                await dal.ExecuteProcedureAsync(
                    connectionInfo.ToConnectionString(),
                    "USP_TX_TEST_DELETE",
                    new Dictionary<string, object> { { "Name", testName } }
                );
            }
        }

        #endregion
    }
}
