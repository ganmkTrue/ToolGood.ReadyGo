﻿using System;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using ToolGood.ReadyGo3.PetaPoco.Core;
using ToolGood.ReadyGo3.PetaPoco.Utilities;

namespace ToolGood.ReadyGo3.PetaPoco.Providers
{
    public class SqlServerDatabaseProvider : DatabaseProvider
    {
        public override DbProviderFactory GetFactory()
        {
#if NETSTANDARD2_0
            return GetFactory(
                "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient"
                );
#else
            return GetFactory(
                "System.Data.SqlClient.SqlClientFactory, System.Data.SqlClient",
                "System.Data.SqlClient.SqlClientFactory, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.SqlClient.SqlClientFactory, System.Data"
                );
#endif
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
    }
}