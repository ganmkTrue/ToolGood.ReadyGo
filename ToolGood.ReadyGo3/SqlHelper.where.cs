﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ToolGood.ReadyGo3.DataCentxt;
using ToolGood.ReadyGo3.DataCentxt.Exceptions;

using ToolGood.ReadyGo3.DataCentxt.Internals;
using ToolGood.ReadyGo3.Mosaic;

namespace ToolGood.ReadyGo3
{
    partial class SqlHelper
    {
        /// <summary>
        /// 动态Sql拼接，不支持Linq;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public SqlHelper<T> Where<T>() where T : class
        {
            return new SqlHelper<T>(this);
        }

        /// <summary>
        /// 动态Sql拼接，不支持Linq;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public SqlHelper<T> Where<T>(string where) where T : class
        {
            return new SqlHelper<T>(this, where);
        }

        /// <summary>
        /// 动态Sql拼接，不支持Linq;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SqlHelper<T> Where<T>(string where, params object[] args) where T : class
        {
            return new SqlHelper<T>(this, where, args);
        }
    }
}

namespace ToolGood.ReadyGo3.Mosaic
{
    /// <summary>
    /// SqlHelper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlHelper<T> where T : class
    {
        private SqlHelper _helper;
        private DatabaseProvider _provider;
        private string _where;
        private string _joinOnText;
        private string _orderBys;
        private string _groupBy;
        private string _having;
        private bool _useDistinct = false;
        private bool _jump = false;

        internal SqlHelper(SqlHelper helper)
        {
            _helper = helper;
            _provider = DatabaseProvider.Resolve(helper._sqlType);
        }
        internal SqlHelper(SqlHelper helper, string where)
        {
            _helper = helper;
            _provider = DatabaseProvider.Resolve(helper._sqlType);
            if (where.StartsWith("where ", StringComparison.CurrentCultureIgnoreCase)) {
                where = where.Substring(6);
            }
            _where = where;
        }
        internal SqlHelper(SqlHelper helper, string where, object[] args)
        {
            _helper = helper;
            _provider = DatabaseProvider.Resolve(helper._sqlType);
            if (where.StartsWith("where ", StringComparison.CurrentCultureIgnoreCase)) {
                where = where.Substring(6);
            }
            _where = _provider.FormatSql(where, args);
        }

        #region IfTrue IfSet IfNullOrEmpty IfNull
        /// <summary>
        /// 判断为真 ,否则跳过
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public SqlHelper<T> IfTrue(bool b)
        {
            _jump = !b;
            return this;
        }
        /// <summary>
        /// 判断为假 ,否则跳过
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public SqlHelper<T> IfFalse(bool b)
        {
            _jump = b;
            return this;
        }
        /// <summary>
        /// 判断为已设置 ,否则跳过
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public SqlHelper<T> IfSet(string txt)
        {
            if (string.IsNullOrEmpty(txt)) {
                _jump = true;
            }
            return this;
        }
        /// <summary>
        /// 判断为未设置 ,否则跳过
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public SqlHelper<T> IfNotSet(string txt)
        {
            if (string.IsNullOrEmpty(txt) == false) {
                _jump = true;
            }
            return this;
        }
        /// <summary>
        /// 判断为NULL 或 空 ,否则跳过
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SqlHelper<T> IfNullOrEmpty(string value)
        {
            if (string.IsNullOrEmpty(value) == false) {
                _jump = true;
            }
            return this;
        }
        /// <summary>
        /// 判断为NULL 或 空格,否则跳过
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public SqlHelper<T> IfNullOrWhiteSpace(string value)
        {
            if (string.IsNullOrWhiteSpace(value) == false) {
                _jump = true;
            }
            return this;
        }
        /// <summary>
        /// 判断为NULL,否则跳过
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public SqlHelper<T> IfNull(object obj)
        {
            if (Object.Equals(null, obj) == false) {
                _jump = true;
            }
            return this;
        }
        /// <summary>
        /// 判断为非NULL,否则跳过
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public SqlHelper<T> IfNotNull(object obj)
        {
            if (Object.Equals(null, obj)) {
                _jump = true;
            }
            return this;
        }
        #endregion

        #region Where JoinWithOn GroupBy Having OrderBy Distinct
        /// <summary>
        /// Where ... Exists  语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SqlHelper<T> WhereExists(string sql, params object[] args)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(sql)) {
                throw new ArgumentNullException("sql");
            }
            if (sql[0] != '(' || sql[sql.Length - 1] != ')') {
                sql = "(" + sql + ")";
            }
            sql = "EXISTS " + _provider.FormatSql(sql, args);
            if (_where == null) {
                _where = sql;
            } else {
                _where += " AND " + sql;
            }
            return this;
        }
        /// <summary>
        /// Where ...NOT Exists  语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SqlHelper<T> WhereNotExists(string sql, params object[] args)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(sql)) {
                throw new ArgumentNullException("sql");
            }
            if (sql[0] != '(' || sql[sql.Length - 1] != ')') {
                sql = "(" + sql + ")";
            }
            sql = "NOT EXISTS " + _provider.FormatSql(sql, args);
            if (_where == null) {
                _where = sql;
            } else {
                _where += " AND " + sql;
            }
            return this;
        }
        /// <summary>
        /// Where语句
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public SqlHelper<T> Where(string sql, params object[] args)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(sql)) {
                throw new ArgumentNullException("sql");
            }
            if (sql.StartsWith("where ", StringComparison.CurrentCultureIgnoreCase)) {
                sql = sql.Substring(6);
            }
            if (sql[0] != '(' || sql[sql.Length - 1] != ')') {
                if (sql.IndexOf(" or ", StringComparison.CurrentCultureIgnoreCase) >= 0) {
                    sql = "(" + sql + ")";
                }
            }

            sql = _provider.FormatSql(sql, args);
            if (_where == null) {
                _where = sql;
            } else {
                _where += " AND " + sql;
            }
            return this;
        }
        /// <summary>
        /// join On 语句
        /// </summary>
        /// <param name="joinWithOn"></param>
        /// <returns></returns>
        public SqlHelper<T> JoinWithOn(string joinWithOn)
        {
            if (_jump) { _jump = false; return this; }
            if (joinWithOn == null) {
                throw new ArgumentNullException("joinWithOn");
            }
            joinWithOn = _provider.FormatSql(joinWithOn, null);
            if (_joinOnText == null) {
                _joinOnText = joinWithOn;
            } else {
                _joinOnText += " " + joinWithOn;
            }
            return this;
        }
        /// <summary>
        /// Group By 语句
        /// </summary>
        /// <param name="groupBy"></param>
        /// <returns></returns>
        public SqlHelper<T> GroupBy(string groupBy)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(groupBy)) {
                throw new ArgumentNullException("groupBy");
            }
            if (groupBy.StartsWith("group By ", StringComparison.CurrentCultureIgnoreCase)) {
                groupBy = groupBy.Substring(9);
            }
            groupBy = _provider.FormatSql(groupBy, null);

            if (_groupBy == null) {
                _groupBy = groupBy;
            } else {
                _groupBy += "," + groupBy;
            }
            return this;
        }
        /// <summary>
        /// Having语句
        /// </summary>
        /// <param name="having"></param>
        /// <returns></returns>
        public SqlHelper<T> Having(string having)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(having)) {
                throw new ArgumentNullException("having");
            }
            if (having.StartsWith("having ", StringComparison.CurrentCultureIgnoreCase)) {
                having = having.Substring(7);
            }
            having = _provider.FormatSql(having, null);

            if (_having == null) {
                _having = having;
            } else {
                _having += "," + having;
            }
            return this;

        }
        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="orderBy"></param>
        /// <returns></returns>
        public SqlHelper<T> OrderBy(string orderBy)
        {
            if (_jump) { _jump = false; return this; }
            if (string.IsNullOrWhiteSpace(orderBy)) {
                throw new ArgumentNullException("orderBy");
            }
            if (orderBy.StartsWith("order By ", StringComparison.CurrentCultureIgnoreCase)) {
                orderBy = orderBy.Substring(9);
            }
            orderBy = _provider.FormatSql(orderBy, null);

            if (_orderBys == null) {
                _orderBys = orderBy;
            } else {
                _orderBys += "," + orderBy;
            }
            return this;
        }

        /// <summary>
        /// 查询语句添加【Distinct】 
        /// </summary>
        /// <returns></returns>
        public SqlHelper<T> Distinct()
        {
            _useDistinct = true;
            return this;
        }
        #endregion

        private string GetFullSelectSql(int limit, int offset, List<string> selectColumns)
        {
            var pd = PetaPoco.Core.PocoData.ForType(typeof(T), null);
            var fromTable = _provider.EscapeSqlIdentifier(pd.TableInfo.TableName);

            if (limit <= 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                if (_useDistinct) sb.Append("DISTINCT ");
                sb.Append(string.Join(",", selectColumns));
                sb.Append(" FROM ");
                sb.Append(fromTable);
                if (string.IsNullOrEmpty(_joinOnText) == false) {
                    sb.Append(" ");
                    sb.Append(_joinOnText);
                }
                if (string.IsNullOrEmpty(_where) == false) {
                    sb.Append(" WHERE ");
                    sb.Append(_where);
                }
                if (string.IsNullOrEmpty(_groupBy) == false) {
                    sb.Append(" GROUP BY ");
                    sb.Append(_groupBy);
                }
                if (string.IsNullOrEmpty(_having) == false) {
                    sb.Append(" HAVING ");
                    sb.Append(_having);
                }
                if (string.IsNullOrEmpty(_orderBys) == false) {
                    sb.Append(" ORDER BY ");
                    sb.Append(_orderBys);
                }
                return sb.ToString();
            }
            return _provider.Select(null, _useDistinct, limit, offset, selectColumns, fromTable, _joinOnText, _where,
                _orderBys, _groupBy, _having);
        }

        private List<string> GetSelectColumns<Table>()
        {
            var type1 = typeof(T);
            var type2 = typeof(Table);

            if (type1 == type2) {
                var pd = PetaPoco.Core.PocoData.ForType(type1, null);
                var tableName = _provider.EscapeSqlIdentifier(pd.TableInfo.TableName);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var item in pd.Columns) {
                    var col = item.Value;
                    stringBuilder.Append(",");

                    if (col.ResultColumn) {
                        if (string.IsNullOrEmpty(col.ResultSql) == false) {
                            stringBuilder.AppendFormat(col.ResultSql, tableName + ".");
                            stringBuilder.Append(" AS ");
                            stringBuilder.Append(col.ColumnName);
                        }
                    } else {
                        stringBuilder.AppendFormat("{0}.{1}", tableName, _provider.EscapeSqlIdentifier(col.ColumnName));
                    }
                }
                if (stringBuilder.Length == 0) throw new NoColumnException();
                stringBuilder.Remove(0, 1);
                return new List<string>() { stringBuilder.ToString() };
            } else {
                var pd = PetaPoco.Core.PocoData.ForType(type1, null);
                var tableName = _provider.EscapeSqlIdentifier(pd.TableInfo.TableName);

                var cols = PetaPoco.Core.PocoData.ForType(type2, null).Columns.Select(q => q.Key).ToString();

                StringBuilder stringBuilder = new StringBuilder();
                foreach (var item in pd.Columns) {
                    var col = item.Value;
                    if (cols.Contains(item.Key) == false) continue;

                    stringBuilder.Append(",");

                    if (col.ResultColumn) {
                        if (string.IsNullOrEmpty(col.ResultSql) == false) {
                            stringBuilder.AppendFormat(col.ResultSql, tableName + ".");
                            stringBuilder.Append(" AS ");
                            stringBuilder.Append(col.ColumnName);
                        }
                    } else {
                        stringBuilder.AppendFormat("{0}.{1}", tableName, _provider.EscapeSqlIdentifier(col.ColumnName));
                    }
                }
                if (stringBuilder.Length == 0) throw new NoColumnException();
                stringBuilder.Remove(0, 1);
                return new List<string>() { stringBuilder.ToString() };
            }
        }


        #region SelectCount
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <returns></returns>
        public int SelectCount()
        {
            return getCount(null);
        }
        /// <summary>
        /// 获取数量
        /// </summary>
        /// <param name="distinctColumn"></param>
        /// <returns></returns>
        public int SelectCount(string distinctColumn)
        {
            return getCount(distinctColumn);
        }
        private int getCount(string column)
        {
            var t = _useDistinct;
            if (_useDistinct) _useDistinct = false;

            List<string> columns = new List<string>();
            if (string.IsNullOrEmpty(column)) {
                columns.Add("COUNT(1)");
            } else {
                columns.Add("COUNT(DISTINCT " + column + ")");
            }
            var sql = GetFullSelectSql(0, 0, columns);
            var count = _helper.ExecuteScalar<int>(sql);

            if (t) _useDistinct = true;
            return count;
        }
        #endregion

        #region Single SingleOrDefault First FirstOrDefault
        /// <summary>
        /// 获取唯一一个类型，若数量不为1，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <returns></returns>
        public Table Single<Table>()
        {
            return getSingle<Table>(GetSelectColumns<Table>());
        }
        /// <summary>
        /// 获取唯一一个类型，若数量不为1，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Table Single<Table>(params string[] columns)
        {
            return getSingle<Table>(columns.ToList());
        }
        private Table getSingle<Table>(List<string> columns)
        {
            var sql = GetFullSelectSql(2, 0, columns);
            _useDistinct = false;
            return _helper._Single<Table>(sql);
        }
        /// <summary>
        /// 获取唯一一个类型，若数量不为1，则抛出异常
        /// </summary>
        /// <returns></returns>
        public T Single()
        {
            return getSingle<T>(GetSelectColumns<T>());
        }
        /// <summary>
        /// 获取唯一一个类型，若数量不为1，则抛出异常
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T Single(params string[] columns)
        {
            return getSingle<T>(columns.ToList());
        }
        /// <summary>
        /// 获取唯一一个类型，可以为空，若数量不为1，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <returns></returns>
        public Table SingleOrDefault<Table>()
        {
            return getSingleOrDefault<Table>(GetSelectColumns<Table>());
        }
        /// <summary>
        /// 获取唯一一个类型，可以为空，若数量不为1，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Table SingleOrDefault<Table>(params string[] columns)
        {
            return getSingleOrDefault<Table>(columns.ToList());
        }
        private Table getSingleOrDefault<Table>(List<string> columns)
        {
            var sql = GetFullSelectSql(2, 0, columns);
            _useDistinct = false;
            return _helper._SingleOrDefault<Table>(sql);
        }
        /// <summary>
        /// 获取唯一一个类型，可以为空，若数量不为1，则抛出异常
        /// </summary>
        /// <returns></returns>
        public T SingleOrDefault()
        {
            return getSingleOrDefault<T>(GetSelectColumns<T>());
        }
        /// <summary>
        /// 获取唯一一个类型，可以为空，若数量不为1，则抛出异常
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T SingleOrDefault(params string[] columns)
        {
            return getSingleOrDefault<T>(columns.ToList());
        }

        /// <summary>
        /// 获取第一个类型，若数量为0，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <returns></returns>
        public Table First<Table>()
        {
            return getFirst<Table>(GetSelectColumns<Table>());
        }
        /// <summary>
        /// 获取第一个类型，若数量为0，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Table First<Table>(params string[] columns)
        {
            return getFirst<Table>(columns.ToList());
        }
        private Table getFirst<Table>(List<string> columns)
        {
            var sql = GetFullSelectSql(1, 0, columns);
            _useDistinct = false;
            return _helper._First<Table>(sql);
        }
        /// <summary>
        /// 获取第一个类型，若数量为0，则抛出异常
        /// </summary>
        /// <returns></returns>
        public T First()
        {
            return getFirst<T>(GetSelectColumns<T>());
        }
        /// <summary>
        /// 获取第一个类型，若数量为0，则抛出异常
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T First(params string[] columns)
        {
            return getFirst<T>(columns.ToList());
        }

        /// <summary>
        /// 获取第一个类型，可为空，若数量为0，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <returns></returns>
        public Table FirstOrDefault<Table>()
        {
            return getFirstOrDefault<Table>(GetSelectColumns<Table>());
        }
        /// <summary>
        /// 获取第一个类型，可为空，若数量为0，则抛出异常
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Table FirstOrDefault<Table>(params string[] columns)
        {
            return getFirstOrDefault<Table>(columns.ToList());
        }
        private Table getFirstOrDefault<Table>(List<string> columns)
        {
            var sql = GetFullSelectSql(1, 0, columns);
            _useDistinct = false;
            return _helper._FirstOrDefault<Table>(sql);
        }
        /// <summary>
        /// 获取第一个类型，可为空，若数量为0，则抛出异常
        /// </summary>
        /// <returns></returns>
        public T FirstOrDefault()
        {
            return getFirstOrDefault<T>(GetSelectColumns<T>());
        }
        /// <summary>
        /// 获取第一个类型，可为空，若数量为0，则抛出异常
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public T FirstOrDefault(params string[] columns)
        {
            return getFirstOrDefault<T>(columns.ToList());
        }
        #endregion

        #region Select
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <returns></returns>
        public List<Table> Select<Table>()
        {
            var columns = GetSelectColumns<Table>();
            return getList<Table>(-1, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Table> Select<Table>(params string[] columns)
        {
            return getList<Table>(-1, -1, columns.ToList());
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="limit"></param>
        /// <returns></returns>
        public List<Table> Select<Table>(int limit)
        {
            var columns = GetSelectColumns<Table>();
            return getList<Table>(limit, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="limit"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Table> Select<Table>(int limit, params string[] columns)
        {
            return getList<Table>(limit, -1, columns.ToList());
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public List<Table> Select<Table>(int limit, int offset)
        {
            var columns = GetSelectColumns<Table>();
            return getList<Table>(limit, offset, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Table> Select<Table>(int limit, int offset, params string[] columns)
        {
            return getList<Table>(limit, offset, columns.ToList());
        }

        private List<Table> getList<Table>(int limit, int offset, List<string> columns)
        {
            var sql = GetFullSelectSql(limit, offset, columns);
            _useDistinct = false;
            return _helper.Select<Table>(sql);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <returns></returns>
        public List<T> Select()
        {
            var columns = GetSelectColumns<T>();
            return getList<T>(-1, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<T> Select(params string[] columns)
        {
            return getList<T>(-1, -1, columns.ToList());
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public List<T> Select(int limit)
        {
            var columns = GetSelectColumns<T>();
            return getList<T>(limit, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<T> Select(int limit, params string[] columns)
        {
            return getList<T>(limit, -1, columns.ToList());
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public List<T> Select(int limit, int offset)
        {
            var columns = GetSelectColumns<T>();
            return getList<T>(limit, offset, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回集合
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<T> Select(int limit, int offset, params string[] columns)
        {
            return getList<T>(limit, offset, columns.ToList());
        }

        #endregion

        #region Page
        /// <summary>
        /// 执行SQL 查询,返回Page类型
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Page<Table> Page<Table>(int page, int size)
        {
            var columns = GetSelectColumns<Table>();
            return getPage<Table>(page, size, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回Page类型
        /// </summary>
        /// <typeparam name="Table"></typeparam>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Page<Table> Page<Table>(int page, int size, params string[] columns)
        {
            return getPage<Table>(page, size, columns.ToList());
        }
        private Page<Table> getPage<Table>(int page, int size, List<string> columns)
        {
            var offset = (page - 1) * size;
            var limit = size;
            var sql = GetFullSelectSql(limit, offset, columns);
            _useDistinct = false;

            var count = getCount(null);
            Page<Table> pt = new Page<Table> {
                TotalItems = count,
                CurrentPage = page,
                PageSize = size,
                Items = _helper.Select<Table>(sql)
            };
            return pt;
        }

        /// <summary>
        /// 执行SQL 查询,返回Page类型
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Page<T> Page(int page, int size)
        {
            var columns = GetSelectColumns<T>();
            return getPage<T>(page, size, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回Page类型
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public Page<T> Page(int page, int size, params string[] columns)
        {
            return getPage<T>(page, size, columns.ToList());
        }
        #endregion

        #region ExecuteDataTable
        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <returns></returns>
        public DataTable ExecuteDataTable()
        {
            var columns = GetSelectColumns<T>();
            return getDataTable(-1, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(params string[] columns)
        {
            return getDataTable(-1, -1, columns.ToList());
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(int limit)
        {
            var columns = GetSelectColumns<T>();
            return getDataTable(limit, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(int limit, params string[] columns)
        {
            return getDataTable(limit, -1, columns.ToList());
        }


        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(int limit, int offset)
        {
            var columns = GetSelectColumns<T>();
            return getDataTable(limit, offset, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataTable
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(int limit, int offset, params string[] columns)
        {
            return getDataTable(limit, offset, columns.ToList());
        }



        private DataTable getDataTable(int limit, int offset, List<string> columns)
        {
            var sql = GetFullSelectSql(limit, offset, columns);
            _useDistinct = false;
            return _helper.ExecuteDataTable(sql);
        }

        #endregion

        #region ExecuteDataSet
        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <returns></returns>
        public DataSet ExecuteDataSet()
        {
            var columns = GetSelectColumns<T>();
            return getDataSet(-1, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(params string[] columns)
        {
            return getDataSet(-1, -1, columns.ToList());
        }

        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(int limit)
        {
            var columns = GetSelectColumns<T>();
            return getDataSet(limit, -1, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(int limit, params string[] columns)
        {
            return getDataSet(limit, -1, columns.ToList());
        }

        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(int limit, int offset)
        {
            var columns = GetSelectColumns<T>();
            return getDataSet(limit, offset, columns);
        }
        /// <summary>
        /// 执行SQL 查询,返回 DataSet
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public DataSet ExecuteDataSet(int limit, int offset, params string[] columns)
        {
            return getDataSet(limit, offset, columns.ToList());
        }

 
        private DataSet getDataSet(int limit, int offset, List<string> columns)
        {
            var sql = GetFullSelectSql(limit, offset, columns);
            _useDistinct = false;
            return _helper.ExecuteDataSet(sql);
        }
        #endregion

    }
}
