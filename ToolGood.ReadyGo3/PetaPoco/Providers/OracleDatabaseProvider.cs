﻿using System;
using System.Data;
using System.Data.Common;
using System.Text;
using ToolGood.ReadyGo3.PetaPoco.Core;
using ToolGood.ReadyGo3.PetaPoco.Internal;
using ToolGood.ReadyGo3.PetaPoco.Utilities;

namespace ToolGood.ReadyGo3.PetaPoco.Providers
{
    public class OracleDatabaseProvider : DatabaseProvider
    {
        public override string GetParameterPrefix(string connectionString)
        {
            return ":";
        }

        public override void PreExecute(IDbCommand cmd)
        {
            cmd.GetType().GetProperty("BindByName").SetValue(cmd, true, null);
            cmd.GetType().GetProperty("InitialLONGFetchSize").SetValue(cmd, -1, null);
        }

        public override string BuildPageQuery(long skip, long take, SQLParts parts, ref object[] args)
        {
            if (parts.SqlSelectRemoved.StartsWith("*"))
                throw new Exception("Query must alias '*' when performing a paged query.\neg. select t.* from table t order by t.id");

            // Same deal as SQL Server
            return Singleton<SqlServerDatabaseProvider>.Instance.BuildPageQuery(skip, take, parts, ref args);
        }

        public override DbProviderFactory GetFactory()
        {
            // "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess" is for Oracle.ManagedDataAccess.dll
            // "Oracle.DataAccess.Client.OracleClientFactory, Oracle.DataAccess" is for Oracle.DataAccess.dll
            return GetFactory(
                "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess, Culture=neutral, PublicKeyToken=89b483f429c47342",
                "Oracle.ManagedDataAccess.Client.OracleClientFactory, Oracle.ManagedDataAccess",

                "Oracle.DataAccess.Client.OracleClientFactory, Oracle.DataAccess, Culture=neutral, PublicKeyToken=89b483f429c47342",
                "Oracle.DataAccess.Client.OracleClientFactory, Oracle.DataAccess",

                "System.Data.OracleClient.OracleClientFactory, System.Data.OracleClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.OracleClient.OracleClientFactory, System.Data.OracleClient, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.OracleClient.OracleClientFactory, System.Data.OracleClient"
                );
        }

        public override string EscapeSqlIdentifier(string sqlIdentifier)
        {
            return $"\"{sqlIdentifier.ToUpperInvariant()}\"";
        }
        public override string GetTableName(string databaseName, string schemaName, string tableName)
        {
            if (string.IsNullOrEmpty(databaseName) == false) {
                if (string.IsNullOrEmpty(schemaName) == false) {
                    return $"\"{databaseName}\".\"{schemaName}\".\"{tableName}\"";
                }
                return $"\"{databaseName}\".\"{tableName}\"";
            }
            if (string.IsNullOrEmpty(schemaName) == false) {
                return $"\"{schemaName}\".\"{tableName}\"";
            }
            return $"\"{tableName}\"";
        }

        public override string GetAutoIncrementExpression(TableInfo ti)
        {
            if (!string.IsNullOrEmpty(ti.SequenceName))
                return $"{ti.SequenceName}.nextval";

            return null;
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string primaryKeyName)
        {
            if (primaryKeyName != null)
            {
                cmd.CommandText += $" returning {EscapeSqlIdentifier(primaryKeyName)} into :newid";
                var param = cmd.CreateParameter();
                param.ParameterName = ":newid";
                param.Value = DBNull.Value;
                param.Direction = ParameterDirection.ReturnValue;
                param.DbType = DbType.Int64;
                cmd.Parameters.Add(param);
                db.ExecuteNonQueryHelper(cmd);
                return param.Value;
            }
            else
            {
                db.ExecuteNonQueryHelper(cmd);
                return -1;
            }
        }
        public override string CreateSql(int limit, int offset, string columnSql, string fromtable, string order, string where)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
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
            var sql = sb.ToString();
            sb.Clear();
            if (offset <= 0) {
                sb.Append("SELECT * FROM (");
                sb.Append(sql);
                sb.Append(" ) WHERE rownum <= ");
                sb.Append(limit.ToString());
                sb.Append(";");
            } else {
                sb.Append("SELECT * FROM (");
                sb.Append("SELECT outtable.*, rownum rn FROM (");
                sb.Append(sql);
                sb.Append(" ) outtable WHERE rownum > ");
                sb.Append(offset.ToString());
                sb.Append(" ) WHERE rn <= ");
                sb.Append((limit + offset).ToString());
                sb.Append(";");
            }
            return sb.ToString();
        }

    }
}