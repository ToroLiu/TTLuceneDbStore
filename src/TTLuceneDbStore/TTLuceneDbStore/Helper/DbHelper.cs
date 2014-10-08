using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TTLuceneDbStore.Helper
{
    /// <summary>
    /// Db help tool. For Update, and Delete convinient operations.
    /// </summary>
    internal static class DbHelper
    {
        private static string GetTableName(Type table)
        {
            var tableAttributes = (TableAttribute[])table.GetCustomAttributes(typeof(TableAttribute), false);
            if (tableAttributes.Count() > 0)
            {
                return tableAttributes[0].Name;
            }

            return null;
        }

        public static int TTUpdate(this DbContext context, Type table, string syntax, params object[] objects)
        {
            string tableName = GetTableName(table);
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("it needs Table attribute for working.");
            }

            return context.Database.ExecuteSqlCommand("UPDATE " + tableName + " " + syntax, objects);
        }

        public static int TTDelete(this DbContext context, Type table, string syntax, params object[] objects)
        {
            string tableName = GetTableName(table);
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("it needs Table attribute for working.");
            }

            return context.Database.ExecuteSqlCommand("DELETE FROM " + tableName + " " + syntax, objects);
        }

        /// <summary>
        /// 用來取得Column的名稱。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static string ColumnName<T>(Expression<Func<T, object>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member != null)
            {
                ColumnAttribute[] columnsAttributes = (ColumnAttribute[])member.Member.GetCustomAttributes(typeof(ColumnAttribute), false);
                if (columnsAttributes.Length == 0 || string.IsNullOrEmpty(columnsAttributes[0].Name) == true)
                    return member.Member.Name;

                return columnsAttributes[0].Name;
            }

            throw new ArgumentException("expression should be a proeprty field.");
        }


        public static int TTTruncateAll(this DbContext context, Type table)
        {
            string tableName = GetTableName(table);
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("it needs Table attribute for working.");
            }
            return context.Database.ExecuteSqlCommand("DELETE FROM " + tableName);
        }
    }
}

