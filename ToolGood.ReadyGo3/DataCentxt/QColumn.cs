﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolGood.ReadyGo3.DataCentxt.Enums;
using ToolGood.ReadyGo3.DataCentxt.Exceptions;

using ToolGood.ReadyGo3.DataCentxt.Internals;

namespace ToolGood.ReadyGo3.DataCentxt
{
    public partial class QColumn
    {
        protected internal ColumnType _columnType;
        // Column 信息
        protected internal QTable _table;
        protected internal string _columnName;
        protected internal bool _isResultColumn;
        protected internal string _resultSql;

        protected internal SqlFunction _function;
        protected internal object[] _functionArgs;
        // Code 信息
        protected internal string _code;
        // As 信息
        protected internal string _asName;

        internal QColumn() { }
    }

    public partial class QSqlColumn : QColumn
    {
        public QSqlColumn As(string name) { _asName = name; return this; }
        internal QSqlColumn() { }
        internal QSqlColumn(SqlFunction function, params object[] args)
        {
            _columnType = ColumnType.Function;
            _function = function;
            _functionArgs = args;
        }
    }


    public abstract class QTableColumn : QColumn
    {
        internal bool _isPrimaryKey;
        internal bool _isAutoIncrement;
        internal ColumnChangeType _changeType;
        internal string _fieldType;

        internal abstract object GetValue();
        protected internal abstract void SetValue(object value);
        internal abstract QTableColumn GetNewValue();
        protected internal abstract void ClearValue();
    }

    public class QTableColumn<T> : QTableColumn
    {
        internal T _value;
        internal QTableColumn<T> _newValue;

        internal QTableColumn() : base() { _fieldType = typeof(T).Name.ToLower(); }

        public QTableColumn<T> NewValue
        {
            get { return (QTableColumn<T>)_newValue; }
            set {
                _newValue = value;
                if (value._columnType == ColumnType.Value) {
                    _value = ((QTableColumn<T>)value)._value;
                    _changeType = ColumnChangeType.NewValue;
                    return;
                }
                _changeType = ColumnChangeType.NewSql;
            }
        }
        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public QTableColumn<T> As(string name) { _asName = name; return this; }

        internal override object GetValue()
        {
            return _value;
        }

        internal override QTableColumn GetNewValue()
        {
            return _newValue;
        }

        protected internal override void ClearValue()
        {
            _value = default(T);
            _changeType = ColumnChangeType.None;
        }


        protected internal override void SetValue(object value)
        {
            object obj = ChangeType(value, typeof(T));
            _value = (T)obj;
            _changeType = ColumnChangeType.NewValue;
        }

        public static implicit operator QTableColumn<T>(QSqlColumn value)
        {
            return new QTableColumn<T>() {
                _asName = value._asName,
                _code = value._code,
                _columnName = value._columnName,
                _columnType = value._columnType,
                _function = value._function,
                _isResultColumn = value._isResultColumn,
                _resultSql = value._resultSql,
                _table = value._table,
                _functionArgs = value._functionArgs,
            };
        }

        public static QCondition operator !(QTableColumn<T> col)
        {
            if (typeof(T) != typeof(bool)) { throw new ColumnTypeException(); }
            return new QColumnValueCondition(col, "<>", true);
        }

        public static implicit operator QTableColumn<T>(T value)
        {
            return new QTableColumn<T>() { _value = value, _columnType = Enums.ColumnType.Value };
        }

        private static object ChangeType(object value, Type type)
        {
            if (value == null && type.IsGenericType) return Activator.CreateInstance(type);
            if (value == null) return null;
            if (type == value.GetType()) return value;
            if (type.IsEnum) {
                if (value is string)
                    return Enum.Parse(type, value as string);
                return Enum.ToObject(type, value);
            }
            if (!type.IsInterface && type.IsGenericType) {
                Type innerType = type.GetGenericArguments()[0];
                object innerValue = ChangeType(value, innerType);
                return Activator.CreateInstance(type, new object[] { innerValue });
            }
            if (value is string && type == typeof(Guid)) return new Guid(value as string);
            if (value is string && type == typeof(Version)) return new Version(value as string);
            if (!(value is IConvertible)) return value;
            return Convert.ChangeType(value, type);
        }


    }
}
