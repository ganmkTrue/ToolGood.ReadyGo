﻿using System;

namespace ToolGood.ReadyGo3.DataCentxt.Exceptions
{
    /// <summary>
    /// 列类型异常
    /// </summary>
    public class ColumnTypeException : Exception
    {
        /// <summary>
        /// 列类型异常
        /// </summary>
        public ColumnTypeException():base("The column is not table's column.")
        {
            
        }

    }
}
