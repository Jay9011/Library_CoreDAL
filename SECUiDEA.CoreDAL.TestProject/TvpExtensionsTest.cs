using System.Data;
using CoreDAL.ORM;
using CoreDAL.ORM.Extensions;
using Xunit.Abstractions;

namespace SECUiDEA.CoreDAL.TestProject
{
    /// <summary>
    /// TVP (Table-Valued Parameter) 관련 확장 메서드 테스트
    /// DB 연결 없이 실행 가능한 단위 테스트
    /// </summary>
    public class TvpExtensionsTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public TvpExtensionsTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #region 테스트용 엔티티 클래스

        /// <summary>
        /// TVP 전용 클래스 (TvpColumn만 사용)
        /// </summary>
        public class UserTvpItem
        {
            [TvpColumn("UserId", Order = 0)]
            public int Id { get; set; }

            [TvpColumn("UserName", Order = 1, MaxLength = 100)]
            public string Name { get; set; }

            [TvpColumn("Email", Order = 2)]
            public string? Email { get; set; }
        }

        /// <summary>
        /// DbParameter와 TvpColumn 혼합 사용 클래스
        /// - SEQ, PW: 일반 프로시저 파라미터로만 사용
        /// - ID, NAME: 일반 파라미터 + TVP 둘 다 사용
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

        /// <summary>
        /// TvpColumn 없는 일반 클래스 (하위 호환성 테스트)
        /// </summary>
        public class SimpleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// Nullable 타입 테스트용 클래스
        /// </summary>
        public class NullableItem
        {
            [TvpColumn("Id", Order = 0)]
            public int? Id { get; set; }

            [TvpColumn("Name", Order = 1)]
            public string? Name { get; set; }

            [TvpColumn("Score", Order = 2)]
            public decimal? Score { get; set; }

            [TvpColumn("CreatedAt", Order = 3)]
            public DateTime? CreatedAt { get; set; }
        }

        #endregion

        #region ToDataTable 테스트

        /// <summary>
        /// List → DataTable 변환 테스트 (TvpColumn 사용)
        /// </summary>
        [Fact]
        public void ToDataTable_WithTvpColumnAttribute_ShouldCreateCorrectSchema()
        {
            // Arrange
            var items = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = "홍길동", Email = "hong@test.com" },
                new UserTvpItem { Id = 2, Name = "김철수", Email = "kim@test.com" },
                new UserTvpItem { Id = 3, Name = "이영희", Email = null }
            };

            // Act
            var table = items.ToDataTable();

            // Assert - 스키마 확인
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal("UserId", table.Columns[0].ColumnName);
            Assert.Equal("UserName", table.Columns[1].ColumnName);
            Assert.Equal("Email", table.Columns[2].ColumnName);

            Assert.Equal(typeof(int), table.Columns[0].DataType);
            Assert.Equal(typeof(string), table.Columns[1].DataType);
            Assert.Equal(typeof(string), table.Columns[2].DataType);

            // Assert - 데이터 확인
            Assert.Equal(3, table.Rows.Count);
            Assert.Equal(1, table.Rows[0]["UserId"]);
            Assert.Equal("홍길동", table.Rows[0]["UserName"]);
            Assert.Equal("hong@test.com", table.Rows[0]["Email"]);

            Assert.Equal(DBNull.Value, table.Rows[2]["Email"]); // null → DBNull

            _outputHelper.WriteLine($"ToDataTable 테스트 성공: {table.Rows.Count}행, {table.Columns.Count}컬럼");
        }

        /// <summary>
        /// 혼합 Attribute 사용 시 TvpColumn만 포함되는지 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_WithMixedAttributes_ShouldOnlyIncludeTvpColumns()
        {
            // Arrange
            var users = new List<UserEntity>
            {
                new UserEntity { SEQ = 1, ID = "hong", PW = "pass1", NAME = "홍길동" },
                new UserEntity { SEQ = 2, ID = "kim", PW = "pass2", NAME = "김철수" }
            };

            // Act
            var table = users.ToDataTable();

            // Assert - TvpColumn이 있는 ID, NAME만 포함되어야 함
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal("ID", table.Columns[0].ColumnName);
            Assert.Equal("NAME", table.Columns[1].ColumnName);

            // SEQ, PW는 포함되지 않아야 함
            Assert.False(table.Columns.Contains("SEQ"));
            Assert.False(table.Columns.Contains("PW"));

            // 데이터 확인
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal("hong", table.Rows[0]["ID"]);
            Assert.Equal("홍길동", table.Rows[0]["NAME"]);

            _outputHelper.WriteLine($"혼합 Attribute 테스트 성공: TvpColumn만 포함됨 ({table.Columns.Count}컬럼)");
            _outputHelper.WriteLine($"컬럼 목록: {string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
        }

        /// <summary>
        /// TvpColumn 없는 클래스는 모든 프로퍼티 포함 (하위 호환성)
        /// </summary>
        [Fact]
        public void ToDataTable_WithoutTvpColumnAttribute_ShouldIncludeAllProperties()
        {
            // Arrange
            var items = new List<SimpleItem>
            {
                new SimpleItem { Id = 1, Name = "테스트", CreatedAt = DateTime.Now }
            };

            // Act
            var table = items.ToDataTable();

            // Assert - 모든 프로퍼티가 포함되어야 함
            Assert.Equal(3, table.Columns.Count);
            Assert.True(table.Columns.Contains("Id"));
            Assert.True(table.Columns.Contains("Name"));
            Assert.True(table.Columns.Contains("CreatedAt"));

            _outputHelper.WriteLine($"하위 호환성 테스트 성공: 모든 프로퍼티 포함됨 ({table.Columns.Count}컬럼)");
        }

        /// <summary>
        /// Nullable 타입 처리 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_WithNullableTypes_ShouldHandleNullsCorrectly()
        {
            // Arrange
            var items = new List<NullableItem>
            {
                new NullableItem { Id = 1, Name = "테스트", Score = 95.5m, CreatedAt = DateTime.Now },
                new NullableItem { Id = null, Name = null, Score = null, CreatedAt = null }
            };

            // Act
            var table = items.ToDataTable();

            // Assert
            Assert.Equal(2, table.Rows.Count);

            // 첫 번째 행: 정상 값
            Assert.Equal(1, table.Rows[0]["Id"]);
            Assert.Equal("테스트", table.Rows[0]["Name"]);

            // 두 번째 행: 모든 값이 DBNull
            Assert.Equal(DBNull.Value, table.Rows[1]["Id"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["Name"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["Score"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["CreatedAt"]);

            // 컬럼 타입 확인 (Nullable 해제됨)
            Assert.Equal(typeof(int), table.Columns["Id"].DataType);
            Assert.Equal(typeof(decimal), table.Columns["Score"].DataType);
            Assert.Equal(typeof(DateTime), table.Columns["CreatedAt"].DataType);

            _outputHelper.WriteLine($"Nullable 타입 테스트 성공: null → DBNull 변환됨");
        }

        /// <summary>
        /// 빈 컬렉션 처리 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_WithEmptyCollection_ShouldReturnEmptyTableWithSchema()
        {
            // Arrange
            var items = new List<UserTvpItem>();

            // Act
            var table = items.ToDataTable();

            // Assert - 스키마는 있지만 데이터는 없음
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);

            _outputHelper.WriteLine($"빈 컬렉션 테스트 성공: 스키마만 있는 빈 테이블 생성");
        }

        /// <summary>
        /// 테이블 이름 지정 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_WithTableName_ShouldSetTableName()
        {
            // Arrange
            var items = new List<UserTvpItem> { new UserTvpItem { Id = 1, Name = "Test" } };

            // Act
            var table = items.ToDataTable("MyTableName");

            // Assert
            Assert.Equal("MyTableName", table.TableName);

            _outputHelper.WriteLine($"테이블 이름 테스트 성공: {table.TableName}");
        }

        #endregion

        #region AddRows 테스트

        /// <summary>
        /// 기존 DataTable에 행 추가 테스트
        /// </summary>
        [Fact]
        public void AddRows_ToExistingTable_ShouldMatchColumnsByName()
        {
            // Arrange - 수동으로 스키마 생성
            var table = new DataTable();
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("UserName", typeof(string));
            table.Columns.Add("Email", typeof(string));

            var items = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = "홍길동", Email = "hong@test.com" },
                new UserTvpItem { Id = 2, Name = "김철수", Email = "kim@test.com" }
            };

            // Act
            int addedCount = table.AddRows(items);

            // Assert
            Assert.Equal(2, addedCount);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(1, table.Rows[0]["UserId"]);
            Assert.Equal("홍길동", table.Rows[0]["UserName"]);

            _outputHelper.WriteLine($"AddRows 테스트 성공: {addedCount}행 추가됨");
        }

        /// <summary>
        /// 컬럼 순서가 다른 테이블에 추가 테스트
        /// </summary>
        [Fact]
        public void AddRows_WithDifferentColumnOrder_ShouldMapByName()
        {
            // Arrange - 컬럼 순서가 클래스와 다름
            var table = new DataTable();
            table.Columns.Add("Email", typeof(string));      // 세 번째가 아닌 첫 번째
            table.Columns.Add("UserName", typeof(string));   // 두 번째 그대로
            table.Columns.Add("UserId", typeof(int));        // 첫 번째가 아닌 세 번째

            var items = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = "홍길동", Email = "hong@test.com" }
            };

            // Act
            table.AddRows(items);

            // Assert - 이름으로 매핑되므로 올바른 위치에 값이 들어가야 함
            Assert.Equal("hong@test.com", table.Rows[0]["Email"]);
            Assert.Equal("홍길동", table.Rows[0]["UserName"]);
            Assert.Equal(1, table.Rows[0]["UserId"]);

            _outputHelper.WriteLine($"컬럼 순서 무관 테스트 성공: 이름으로 매핑됨");
        }

        /// <summary>
        /// 일부 컬럼만 있는 테이블에 추가 테스트
        /// </summary>
        [Fact]
        public void AddRows_WithPartialColumns_ShouldOnlyMapExistingColumns()
        {
            // Arrange - UserId와 UserName만 있음 (Email 없음)
            var table = new DataTable();
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("UserName", typeof(string));

            var items = new List<UserTvpItem>
            {
                new UserTvpItem { Id = 1, Name = "홍길동", Email = "hong@test.com" }
            };

            // Act
            table.AddRows(items);

            // Assert - 있는 컬럼만 매핑
            Assert.Equal(1, table.Rows[0]["UserId"]);
            Assert.Equal("홍길동", table.Rows[0]["UserName"]);
            Assert.Equal(2, table.Columns.Count); // Email 컬럼은 없음

            _outputHelper.WriteLine($"부분 컬럼 테스트 성공: 존재하는 컬럼만 매핑됨");
        }

        /// <summary>
        /// 단일 행 추가 테스트
        /// </summary>
        [Fact]
        public void AddRow_SingleItem_ShouldAddOneRow()
        {
            // Arrange
            var table = DataTableExtensions.CreateSchema<UserTvpItem>();
            var item = new UserTvpItem { Id = 1, Name = "홍길동", Email = "hong@test.com" };

            // Act
            bool result = table.AddRow(item);

            // Assert
            Assert.True(result);
            Assert.Equal(1, table.Rows.Count);

            _outputHelper.WriteLine($"단일 행 추가 테스트 성공");
        }

        /// <summary>
        /// Dictionary로 행 추가 테스트
        /// </summary>
        [Fact]
        public void AddRow_WithDictionary_ShouldAddRow()
        {
            // Arrange
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));

            var values = new Dictionary<string, object>
            {
                { "Id", 1 },
                { "Name", "홍길동" }
            };

            // Act
            bool result = table.AddRow(values);

            // Assert
            Assert.True(result);
            Assert.Equal(1, table.Rows.Count);
            Assert.Equal(1, table.Rows[0]["Id"]);
            Assert.Equal("홍길동", table.Rows[0]["Name"]);

            _outputHelper.WriteLine($"Dictionary 행 추가 테스트 성공");
        }

        #endregion

        #region CreateSchema 테스트

        /// <summary>
        /// 스키마만 생성 테스트
        /// </summary>
        [Fact]
        public void CreateSchema_ShouldCreateEmptyTableWithCorrectSchema()
        {
            // Act
            var table = DataTableExtensions.CreateSchema<UserTvpItem>();

            // Assert
            Assert.Equal(3, table.Columns.Count);
            Assert.Equal(0, table.Rows.Count);
            Assert.Equal("UserId", table.Columns[0].ColumnName);
            Assert.Equal("UserName", table.Columns[1].ColumnName);
            Assert.Equal("Email", table.Columns[2].ColumnName);

            _outputHelper.WriteLine($"CreateSchema 테스트 성공: 빈 스키마 생성됨");
        }

        /// <summary>
        /// 스키마 생성 후 데이터 추가 테스트
        /// </summary>
        [Fact]
        public void CreateSchema_ThenAddRows_ShouldWork()
        {
            // Arrange
            var table = DataTableExtensions.CreateSchema<UserTvpItem>("Users");

            // Act
            table.AddRow(new UserTvpItem { Id = 1, Name = "홍길동" });
            table.AddRows(new List<UserTvpItem>
            {
                new UserTvpItem { Id = 2, Name = "김철수" },
                new UserTvpItem { Id = 3, Name = "이영희" }
            });

            // Assert
            Assert.Equal("Users", table.TableName);
            Assert.Equal(3, table.Rows.Count);

            _outputHelper.WriteLine($"스키마 생성 후 데이터 추가 테스트 성공: {table.Rows.Count}행");
        }

        #endregion

        #region Order 속성 테스트

        /// <summary>
        /// Order 순서대로 컬럼 생성되는지 테스트
        /// </summary>
        public class OrderTestItem
        {
            [TvpColumn("Third", Order = 3)]
            public string C { get; set; }

            [TvpColumn("First", Order = 1)]
            public string A { get; set; }

            [TvpColumn("Second", Order = 2)]
            public string B { get; set; }
        }

        [Fact]
        public void ToDataTable_WithOrderAttribute_ShouldRespectOrder()
        {
            // Arrange
            var items = new List<OrderTestItem>
            {
                new OrderTestItem { A = "1", B = "2", C = "3" }
            };

            // Act
            var table = items.ToDataTable();

            // Assert - Order 순서대로 정렬되어야 함
            Assert.Equal("First", table.Columns[0].ColumnName);   // Order = 1
            Assert.Equal("Second", table.Columns[1].ColumnName);  // Order = 2
            Assert.Equal("Third", table.Columns[2].ColumnName);   // Order = 3

            Assert.Equal("1", table.Rows[0][0]);  // A = "1"
            Assert.Equal("2", table.Rows[0][1]);  // B = "2"
            Assert.Equal("3", table.Rows[0][2]);  // C = "3"

            _outputHelper.WriteLine($"Order 순서 테스트 성공: {string.Join(", ", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName))}");
        }

        #endregion

        #region 성능 테스트

        /// <summary>
        /// 대량 데이터 변환 성능 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_Performance_WithLargeData()
        {
            // Arrange
            const int count = 10000;
            var items = Enumerable.Range(1, count)
                .Select(i => new UserTvpItem { Id = i, Name = $"User_{i}", Email = $"user{i}@test.com" })
                .ToList();

            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var table = items.ToDataTable();
            sw.Stop();

            // Assert
            Assert.Equal(count, table.Rows.Count);

            _outputHelper.WriteLine($"대량 데이터 성능 테스트: {count}건 변환 시간 = {sw.ElapsedMilliseconds}ms");

            // 두 번째 호출 (캐시 효과)
            sw.Restart();
            var table2 = items.ToDataTable();
            sw.Stop();

            _outputHelper.WriteLine($"캐시 적용 후: {count}건 변환 시간 = {sw.ElapsedMilliseconds}ms");
        }

        #endregion

        #region 예외 처리 테스트

        /// <summary>
        /// null 컬렉션 예외 테스트
        /// </summary>
        [Fact]
        public void ToDataTable_WithNullCollection_ShouldThrowException()
        {
            // Arrange
            List<UserTvpItem> items = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => items.ToDataTable());
        }

        /// <summary>
        /// null 테이블에 AddRows 예외 테스트
        /// </summary>
        [Fact]
        public void AddRows_WithNullTable_ShouldThrowException()
        {
            // Arrange
            DataTable table = null;
            var items = new List<UserTvpItem>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => table.AddRows(items));
        }

        #endregion
    }
}
