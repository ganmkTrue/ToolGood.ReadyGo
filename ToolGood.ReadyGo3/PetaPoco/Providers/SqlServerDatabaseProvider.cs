﻿using System;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ToolGood.ReadyGo3.PetaPoco.Core;
using ToolGood.ReadyGo3.PetaPoco.Utilities;

namespace ToolGood.ReadyGo3.PetaPoco.Providers
{
    public class SqlServerDatabaseProvider : DatabaseProvider
    {
        public override DbProviderFactory GetFactory()
        {
            return GetFactory(
                "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient",
                "System.Data.SqlClient.SqlClientFactory, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.SqlClient.SqlClientFactory, System.Data"
                );
        }
        protected static readonly Regex SelectTopRegex = new Regex(@"^SELECT +TOP(\d+)", RegexOptions.IgnoreCase);


        public override string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            if (SelectTopRegex.IsMatch(parts.Sql)) return parts.Sql;
            if (skip == 0) {
                if (parts.Sql.StartsWith("SELECT ", StringComparison.InvariantCultureIgnoreCase)) {
                    var sql = $"SELECT TOP(@{args.Length}) " + parts.Sql.Substring(7/*"SELECT ".Length*/);
                    args = args.Concat(new object[] { take }).ToArray();
                    return sql;
                }
            }

            var helper = PagingUtility;
            // when the query does not contain an "order by", it is very slow
            if (helper.SimpleRegexOrderBy.IsMatch(parts.SqlSelectRemoved)) {
                var m = helper.SimpleRegexOrderBy.Match(parts.SqlSelectRemoved);
                if (m.Success) {
                    var g = m.Groups[0];
                    parts.SqlSelectRemoved = parts.SqlSelectRemoved.Substring(0, g.Index);
                }
            }
            if (helper.RegexDistinct.IsMatch(parts.SqlSelectRemoved)) {
                parts.SqlSelectRemoved = $"peta_inner.* FROM (SELECT {parts.SqlSelectRemoved}) peta_inner";
            }
            var sqlPage =
              $"SELECT * FROM (SELECT ROW_NUMBER() OVER ({parts.SqlOrderBy ?? "ORDER BY (SELECT NULL)"}) peta_rn, " +
              $"{parts.SqlSelectRemoved}) peta_paged " +
              $"WHERE peta_rn > @{args.Length} AND peta_rn <= @{args.Length + 1}";
            args = args.Concat(new object[] { skip, skip + take }).ToArray();
            return sqlPage;
        }

        public override object ExecuteInsert(Database db, System.Data.IDbCommand cmd, string primaryKeyName)
        {
            return db.ExecuteScalarHelper(cmd);
        }

        public override string GetExistsSql()
        {
            return "IF EXISTS (SELECT 1 FROM {0} WHERE {1}) SELECT 1 ELSE SELECT 0";
        }

        public override string GetInsertOutputClause(string primaryKeyName)
        {
            return $" OUTPUT INSERTED.[{primaryKeyName}]";
        }
        public override string CreateSql(int limit, int offset, string columnSql, string fromtable, string order, string where)
        {
            if (offset <= 0) {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
                sb.Append("TOP ");
                sb.Append(limit);
                sb.Append(" ");
                sb.Append(columnSql);
                sb.Append(" FROM ");
                sb.Append(fromtable);
                if (string.IsNullOrEmpty(where) == false) {
                    sb.Append(" WHERE ");
                    sb.Append(where);
                }
                if (string.IsNullOrEmpty(order) == false) {
                    sb.Append(" ORDER BY ");
                    sb.Append(order);
                }
                return sb.ToString();
            } else {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("SELECT * FROM (SELECT ROW_NUMBER() OVER (ORDER BY {0})", string.IsNullOrWhiteSpace(order) ? "(SELECT NULL)" : order);
                sb.AppendFormat(" peta_rn,{0} ", columnSql);
                sb.Append(" FROM ");
                sb.Append(fromtable);
                if (string.IsNullOrEmpty(where) == false) {
                    sb.Append(" WHERE ");
                    sb.Append(where);
                }
                sb.AppendFormat(")  peta_paged WHERE peta_rn>{0} AND peta_rn<={1}", offset, limit + offset);
                return sb.ToString();
            }

        }
    }
}