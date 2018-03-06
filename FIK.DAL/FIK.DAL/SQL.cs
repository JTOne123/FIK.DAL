﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FIK.DAL
{
    public class SQL
    {
        private SqlConnection connection;

        public SQL(string connectionString)
        {
            connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Traditional sql query for data insert update delete. where parmater not implemented. so 
        /// sql injection is possible. transaction is implemented
        /// </summary>
        /// <param name="SQL">List of sql string </param>
        /// <param name="ErrorMsg">if any error occured then provide the output</param>
        /// <returns></returns>
        public bool ExecuteQuery(List<string> SQL,ref string ErrorMsg)
        {
            ErrorMsg = "";
            bool result = false;
            SqlTransaction oTransaction = null;
            SqlCommand oCmd = null;

            string errorQuery="";
            try
            {

                if (connection != null)
                {
                    connection.Open();
                    oTransaction = connection.BeginTransaction();
                    foreach (string s in SQL)
                    {
                        errorQuery = s;
                        oCmd = new SqlCommand(s, connection);
                        oCmd.Transaction = oTransaction;
                        oCmd.ExecuteNonQuery();
                    }
                    oTransaction.Commit();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                ErrorMsg = ex.Message + "\r\n" + errorQuery;
                oTransaction.Rollback();
            }
            finally
            {
                connection.Close();
            }
            return result;
        }



        /// <summary>
        /// Geneerate Insert query based on Model passed as parameter 
        /// using SqlCommand  Parameter add , sql injection not possible
        /// </summary>
        /// <param name="dataObject"> pass a single object or list of object </param>
        /// <param name="specificProperty"> when need only some specific property to insert sample ( Id,Name,Amount ) </param>
        /// <param name="ExlcudeAutogeneratePrimaryKey"> If database identity property is set then pass the property name for not add to insert query  sample ( Id,Id2)  </param>
        /// <param name="ErrorMsg">if any error occured then provide the output</param>
        /// <returns></returns>
        public bool Insert<T>(object dataObject,string specificProperty,string ExlcudeAutogeneratePrimaryKey, ref string ErrorMsg)
        {
            bool result = false;
            ErrorMsg = "";

            #region
            PropertyDescriptorCollection props =
               TypeDescriptor.GetProperties(typeof(T));
           

            //try to parse list of object data model
            List<T> ListTob = dataObject as List<T>;
            if (ListTob == null)
            {
                ListTob = new List<T>();

                //try to parse single data model
                T Tob = (T)dataObject;
                if (Tob == null)
                {
                    result = false;
                    ErrorMsg = "Invalid Object";
                    return result;
                }

                ListTob.Add(Tob);
            }

            #region query generate
            string tableName = ListTob[0].GetType().Name;
            StringBuilder queryProperty = new StringBuilder();
            StringBuilder queryValue = new StringBuilder();

            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];

                if (!string.IsNullOrEmpty(ExlcudeAutogeneratePrimaryKey) )
                {
                    if(ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()) )
                    {
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(specificProperty))
                {
                    if (specificProperty.ToUpper().Contains(prop.Name.ToUpper()))
                    {
                        queryProperty.Append(prop.Name);
                        queryProperty.Append(",");

                        queryValue.Append("@" + prop.Name);
                        queryValue.Append(",");
                    }
                }
                else
                {
                    queryProperty.Append(prop.Name);
                    queryProperty.Append(",");

                    queryValue.Append("@" + prop.Name);
                    queryValue.Append(",");
                }


                //oPropertyValue
                //table.Columns.Add(prop.Name, prop.PropertyType);
            }

            queryProperty.Remove(queryProperty.Length - 1, 1);
            queryValue.Remove(queryValue.Length - 1, 1);


            #endregion

            string dynamicQuery = string.Format("insert into {0} ( {1} ) values ({2}) ", tableName, queryProperty.ToString(), queryValue.ToString());


            SqlTransaction oTransaction = null;
            SqlCommand oCmd = null;

            try
            {
                if (connection != null)
                {
                    connection.Open();
                    oTransaction = connection.BeginTransaction();
                    foreach (T obj in ListTob)
                    {
                        oCmd = new SqlCommand(dynamicQuery, connection);

                        for (int i = 0; i < props.Count; i++)
                        {
                            PropertyDescriptor prop = props[i];

                            var value = prop.GetValue(obj) == null ? DBNull.Value : prop.GetValue(obj);

                            if (!string.IsNullOrEmpty(ExlcudeAutogeneratePrimaryKey))
                            {
                                if (ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()))
                                {
                                    continue;
                                }
                            }

                            if (!string.IsNullOrEmpty(specificProperty))
                            {
                                if (specificProperty.ToUpper().Contains(prop.Name.ToUpper()))
                                {
                                    oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                }
                            }
                            else
                            {
                                oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                            }

                            //oPropertyValue
                            //table.Columns.Add(prop.Name, prop.PropertyType);
                        }

                        oCmd.Transaction = oTransaction;
                        oCmd.ExecuteNonQuery();
                    }
                    oTransaction.Commit();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                result = false;
                   ErrorMsg = ex.Message + "\r\n" + dynamicQuery;
                if(oTransaction != null)
                    oTransaction.Rollback();
            }
            finally
            {
                if(connection != null)
                    connection.Close();
            }


            #endregion


            return result;
        }



        /// <summary>
        /// Geneerate update query based on Model passed as parameter ,where clause paramater data not used for update
        /// using SqlCommand  Parameter add , sql injection not possible
        /// </summary>
        /// <param name="dataObject"> pass a single object or list of object </param>
        /// <param name="specificProperty"> when need only some specific property to insert sample ( Id,Name,Amount, +Qty ) , for update if existing data need to increment or decrement then use + or - </param>
        /// <param name="WhereClasseParameter"> generate And operation based where simple clause sample ( Id,Id2)  </param>
        /// <param name="ErrorMsg"> if any error occured then provide the output</param>
        /// <returns></returns>
        public bool Update<T>(object dataObject, string specificProperty, string WhereClasseParameter, ref string ErrorMsg)
        {
            bool result = false;
            ErrorMsg = "";

            #region
            PropertyDescriptorCollection props =
               TypeDescriptor.GetProperties(typeof(T));

            //try to parse list of object data model
            List<T> ListTob = dataObject as List<T>;
            if (ListTob == null)
            {
                ListTob = new List<T>();

                //try to parse single data model
                T Tob = (T)dataObject;
                if (Tob == null)
                {
                    result = false;
                    ErrorMsg = "Invalid Object";
                    return result;
                }

                ListTob.Add(Tob);
            }

            #region query generate
            string tableName = ListTob[0].GetType().Name;
            StringBuilder queryData = new StringBuilder();
            StringBuilder queryWhereClause = new StringBuilder();

            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];

                int index = specificProperty.ToUpper().IndexOf(prop.Name.ToUpper());
                string updateModifier = "";
                if (index - 1 >= 0)
                {
                    updateModifier = specificProperty.Substring(index - 1, 1);
                }

                // recrod set update only which is not available in where clause
                if (!WhereClasseParameter.ToUpper().Contains(prop.Name.ToUpper()))
                {
                    if (!string.IsNullOrEmpty(specificProperty))
                    {
                        if (specificProperty.ToUpper().Contains(prop.Name.ToUpper()))
                        {
                            queryData.Append(prop.Name);
                            queryData.Append("=");

                            if (updateModifier.Contains("+"))
                            {
                                queryData.Append("ISNULL(" + prop.Name + ",0)");
                                queryData.Append("+");
                            }
                            else if (updateModifier.Contains("-"))
                            {
                                queryData.Append("ISNULL(" + prop.Name + ",0)");
                                queryData.Append("+");
                            }


                            queryData.Append("@" + prop.Name);
                            queryData.Append(",");

                        }
                    }
                    else
                    {
                        queryData.Append(prop.Name);
                        queryData.Append("=");

                        if (updateModifier.Contains("+"))
                        {
                            queryData.Append("ISNULL(" +prop.Name+",0)");
                            queryData.Append("+");
                        }
                        else if (updateModifier.Contains("-"))
                        {
                            queryData.Append("ISNULL(" + prop.Name + ",0)");
                            queryData.Append("+");
                        }


                        queryData.Append("@" + prop.Name);
                        queryData.Append(",");
                    }
                }


                if (!string.IsNullOrEmpty(WhereClasseParameter))
                {
                    if (WhereClasseParameter.ToUpper().Contains(prop.Name.ToUpper()))
                    {
                        queryWhereClause.Append(prop.Name);
                        queryWhereClause.Append("=");
                        queryWhereClause.Append("@" + prop.Name);
                        queryWhereClause.Append(" and ");

                    }
                }
                else
                {
                    queryWhereClause.Append(prop.Name);
                    queryWhereClause.Append("=");
                    queryWhereClause.Append("@" + prop.Name);
                    queryWhereClause.Append(" and ");
                }

                //oPropertyValue
                //table.Columns.Add(prop.Name, prop.PropertyType);
            }

            queryData.Remove(queryData.Length - 1, 1);
            queryWhereClause.Remove(queryWhereClause.Length - 4, 4);


            #endregion

            string dynamicQuery = string.Format("update {0} set  {1}  where  ({2}) ", tableName, queryData.ToString(), queryWhereClause.ToString());


            SqlTransaction oTransaction = null;
            SqlCommand oCmd = null;

            try
            {
                if (connection != null)
                {
                    int rowCount = 0;

                    connection.Open();
                    oTransaction = connection.BeginTransaction();
                    foreach (T obj in ListTob)
                    {
                        oCmd = new SqlCommand(dynamicQuery, connection);

                        for (int i = 0; i < props.Count; i++)
                        {
                            PropertyDescriptor prop = props[i];

                            var value = prop.GetValue(obj) == null ? DBNull.Value : prop.GetValue(obj);


                            if (!WhereClasseParameter.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                if (!string.IsNullOrEmpty(specificProperty))
                                {
                                    if (specificProperty.ToUpper().Contains(prop.Name.ToUpper()))
                                    {
                                        oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                    }
                                }
                                else
                                {
                                    oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                }
                            }

                            if (!string.IsNullOrEmpty(WhereClasseParameter))
                            {
                                if (WhereClasseParameter.ToUpper().Contains(prop.Name.ToUpper()))
                                {
                                    
                                        oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                    
                                }
                            }
                            else
                            {
                               
                                    oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                
                            }


                            //oPropertyValue
                            //table.Columns.Add(prop.Name, prop.PropertyType);
                        }

                        oCmd.Transaction = oTransaction;
                        rowCount =  oCmd.ExecuteNonQuery();
                    }
                    oTransaction.Commit();
                    result = true;
                    if(rowCount <= 0)
                    {
                        result = false;
                        ErrorMsg = "No record found for update";
                    }
                   

                }
            }
            catch (Exception ex)
            {
                result = false;
                ErrorMsg = ex.Message + "\r\n" + dynamicQuery;
                if (oTransaction != null)
                    oTransaction.Rollback();
            }
            finally
            {
                if (connection != null)
                    connection.Close();
            }


            #endregion


            return result;
        }


        /// <summary>
        /// Geneerate Insert or update query based on CompositeModel passed as parameter 
        /// using SqlCommand  Parameter add , sql injection not possible
        /// </summary>
        /// <param name="dataObject"> pass a CompositeModel object which can be build by access CompositeModel class </param>
        /// <param name="ErrorMsg"> if any error occured then provide the output</param>
        /// <returns></returns>
        public bool InsertUpdateComposite(CompositeModel dataObject,  ref string ErrorMsg)
        {
            bool result = false;
            ErrorMsg = "";


            List<string> sqlList = new List<string>();

            #region query generation

            foreach(CompositeModel c in dataObject.GetRecordSet())
            {
                PropertyDescriptorCollection props =
               TypeDescriptor.GetProperties(c.ObjectType);

                //var inst = Activator.CreateInstance(c.ObjectType);
                if(c.Model.Count == 0)
                {
                    ErrorMsg = "No object pass to operation";
                    return false;
                }

                //string tableName = c.ObjectType.GetType().Name;
                string tableName = c.ObjectName;
                StringBuilder queryData = new StringBuilder();
                StringBuilder queryWhereClause = new StringBuilder();
                StringBuilder queryProperty = new StringBuilder();
                StringBuilder queryData2 = new StringBuilder();

                string dynamicQuery = "";
                if (c.OperationMode == OperationMode.Update)
                {

                    #region query generate

                    for (int i = 0; i < props.Count; i++)
                    {
                        PropertyDescriptor prop = props[i];

                        // recrod set update only which is not available in where clause
                        if (!c.WhereClauseParamForUpdate.ToUpper().Contains(prop.Name.ToUpper()))
                        {

                            int index = c.SlectiveProperty.ToUpper().IndexOf(prop.Name.ToUpper());
                            string updateModifier = "";
                            if (index - 1 >= 0)
                            {
                                updateModifier = c.SlectiveProperty.Substring(index - 1, 1);
                            }

                            if (!string.IsNullOrEmpty(c.SlectiveProperty))
                            {

                                if (c.SlectiveProperty.ToUpper().Contains(prop.Name.ToUpper()))
                                {
                                    

                                    queryData.Append(prop.Name);
                                    queryData.Append("=");

                                    if (updateModifier.Contains("+"))
                                    {
                                        queryData2.Append("ISNULL(" + prop.Name + ",0)");
                                        queryData2.Append("+");
                                    }
                                    else if (updateModifier.Contains("-"))
                                    {
                                        queryData2.Append("ISNULL(" + prop.Name + ",0)");
                                        queryData2.Append("+");
                                    }

                                    queryData.Append("@" + prop.Name);
                                    queryData.Append(",");

                                }
                            }
                            else
                            {
                                queryData.Append(prop.Name);
                                queryData.Append("=");

                                if (updateModifier.Contains("+"))
                                {
                                    queryData2.Append(prop.Name);
                                    queryData2.Append("+");
                                }
                                else if (updateModifier.Contains("-"))
                                {
                                    queryData2.Append(prop.Name);
                                    queryData2.Append("+");
                                }

                                queryData.Append("@" + prop.Name);
                                queryData.Append(",");
                            }
                        }


                        if (!string.IsNullOrEmpty(c.WhereClauseParamForUpdate))
                        {
                            if (c.WhereClauseParamForUpdate.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                queryWhereClause.Append(prop.Name);
                                queryWhereClause.Append("=");
                                queryWhereClause.Append("@" + prop.Name);
                                queryWhereClause.Append(" and ");

                            }
                        }
                        else
                        {
                            queryWhereClause.Append(prop.Name);
                            queryWhereClause.Append("=");
                            queryWhereClause.Append("@" + prop.Name);
                            queryWhereClause.Append(" and ");
                        }

                        //oPropertyValue
                        //table.Columns.Add(prop.Name, prop.PropertyType);
                    }

                    queryData.Remove(queryData.Length - 1, 1);
                    queryWhereClause.Remove(queryWhereClause.Length - 4, 4);


                    #endregion

                     dynamicQuery = string.Format("update {0} set  {1}  where  ({2}) ", tableName, queryData.ToString(), queryWhereClause.ToString());

                    sqlList.Add(dynamicQuery);
                }
                else if(c.OperationMode == OperationMode.Insert)
                {
                    #region query generate

                    for (int i = 0; i < props.Count; i++)
                    {
                        PropertyDescriptor prop = props[i];


                        if (!string.IsNullOrEmpty(c.ExlcudeAutogeneratePrimaryKey))
                        {
                            if (c.ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                continue;
                            }
                        }

                        if (!string.IsNullOrEmpty(c.SlectiveProperty))
                        {
                            if (c.SlectiveProperty.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                queryProperty.Append(prop.Name);
                                queryProperty.Append(",");

                                queryData.Append("@" + prop.Name);
                                queryData.Append(",");
                            }
                        }
                        else
                        {
                            queryProperty.Append(prop.Name);
                            queryProperty.Append(",");

                            queryData.Append("@" + prop.Name);
                            queryData.Append(",");
                        }



                        //oPropertyValue
                        //table.Columns.Add(prop.Name, prop.PropertyType);
                    }

                    queryProperty.Remove(queryProperty.Length - 1, 1);
                    queryData.Remove(queryData.Length - 1, 1);


                    #endregion

                     dynamicQuery = string.Format("insert into {0} ( {1} ) values ({2}) ", tableName, queryProperty.ToString(), queryData.ToString());

                    sqlList.Add(dynamicQuery);
                }
                else if (c.OperationMode == OperationMode.InsertOrUpdaet)
                {

                    #region query generate

                    for (int i = 0; i < props.Count; i++)
                    {
                        PropertyDescriptor prop = props[i];

                        #region insert query
                        if (!string.IsNullOrEmpty(c.ExlcudeAutogeneratePrimaryKey))
                        {
                            if (c.ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                continue;
                            }
                        }

                        if (!string.IsNullOrEmpty(c.SlectiveProperty))
                        {
                            if (c.SlectiveProperty.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                queryProperty.Append(prop.Name);
                                queryProperty.Append(",");

                                queryData.Append("@" + prop.Name);
                                queryData.Append(",");
                            }
                        }
                        else
                        {
                            queryProperty.Append(prop.Name);
                            queryProperty.Append(",");

                            queryData.Append("@" + prop.Name);
                            queryData.Append(",");
                        }

                        #endregion

                        #region update query

                        if (!c.WhereClauseParamForUpdate.ToUpper().Contains(prop.Name.ToUpper()))
                        {
                            if (!string.IsNullOrEmpty(c.SlectiveProperty))
                            {
                                if (c.SlectiveProperty.ToUpper().Contains(prop.Name.ToUpper()))
                                {
                                    int index = c.SlectiveProperty.ToUpper().IndexOf(prop.Name.ToUpper());
                                    string updateModifier = "";
                                    if (index - 1 >= 0)
                                    {
                                        updateModifier = c.SlectiveProperty.Substring(index - 1, 1);     
                                    }


                                    queryData2.Append(prop.Name);
                                    queryData2.Append("=");
                                    if (updateModifier.Contains("+"))
                                    {
                                        queryData2.Append("ISNULL(" + prop.Name + ",0)");
                                        queryData2.Append("+");
                                    }
                                    else if (updateModifier.Contains("-"))
                                    {
                                        queryData2.Append("ISNULL(" + prop.Name + ",0)");
                                        queryData2.Append("+");
                                    }

                                    queryData2.Append("@" + prop.Name);
                                    queryData2.Append(",");

                                }
                            }
                            else
                            {
                                queryData2.Append(prop.Name);
                                queryData2.Append("=");
                                queryData2.Append("@" + prop.Name);
                                queryData2.Append(",");
                            }
                        }


                        if (!string.IsNullOrEmpty(c.WhereClauseParamForUpdate))
                        {
                            if (c.WhereClauseParamForUpdate.ToUpper().Contains(prop.Name.ToUpper()))
                            {
                                queryWhereClause.Append(prop.Name);
                                queryWhereClause.Append("=");
                                queryWhereClause.Append("@" + prop.Name);
                                queryWhereClause.Append(" and ");

                            }
                        }
                        else
                        {
                            queryWhereClause.Append(prop.Name);
                            queryWhereClause.Append("=");
                            queryWhereClause.Append("@" + prop.Name);
                            queryWhereClause.Append(" and ");
                        }



                        #endregion


                        //oPropertyValue
                        //table.Columns.Add(prop.Name, prop.PropertyType);
                    }

                    queryData2.Remove(queryData2.Length - 1, 1);
                    queryWhereClause.Remove(queryWhereClause.Length - 4, 4);

                    queryProperty.Remove(queryProperty.Length - 1, 1);
                    queryData.Remove(queryData.Length - 1, 1);


                    #endregion

                    string dynamicUpdateQuery = string.Format("update {0} set  {1}  where  ({2}) ", tableName, queryData2.ToString(), queryWhereClause.ToString());

                    string dynamicInsertQuery = string.Format("insert into {0} ( {1} ) values ({2}) ", tableName, queryProperty.ToString(), queryData.ToString());


                     dynamicQuery = string.Format(@"
                                        if exists(select * from {0} where {1} )
                                        begin
                                         {2}
                                        end
                                        else
                                        begin
                                           {3}
                                        end ", tableName, queryWhereClause.ToString(), dynamicUpdateQuery, dynamicInsertQuery);


                    sqlList.Add(dynamicQuery);

                }

                if (string.IsNullOrEmpty(dynamicQuery))
                {
                    ErrorMsg = "Query Parse failed";
                    return false;
                }






            }
            #endregion


            

                SqlTransaction oTransaction = null;
                SqlCommand oCmd = null;


                    string queryError = "";
            try
            {
                    if (connection != null)
                    {
                        int rowCount = 0;

                        connection.Open();
                        oTransaction = connection.BeginTransaction();

                    int index = 0;
                    foreach (CompositeModel c in dataObject.GetRecordSet())
                    {
                        PropertyDescriptorCollection props =
                       TypeDescriptor.GetProperties(c.ObjectType);

                        foreach (object obj in c.Model)
                        {
                            oCmd = new SqlCommand(sqlList[index], connection);
                            queryError = sqlList[index];

                            for (int i = 0; i < props.Count; i++)
                            {
                                PropertyDescriptor prop = props[i];
                                var value = prop.GetValue(obj) == null ? DBNull.Value : prop.GetValue(obj);


                                if (!string.IsNullOrEmpty(c.SlectiveProperty))
                                {
                                    if (!string.IsNullOrEmpty(c.ExlcudeAutogeneratePrimaryKey))
                                    {
                                        if (c.ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()))
                                        {
                                            continue;
                                        }
                                    }
                                    if (c.SlectiveProperty.ToUpper().Contains(prop.Name.ToUpper()))
                                    {
                                        oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                        continue;
                                    }
                                    if (c.WhereClauseParamForUpdate.ToUpper().Contains(prop.Name.ToUpper()))
                                    {
                                        oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(c.ExlcudeAutogeneratePrimaryKey))
                                    {
                                        if (c.ExlcudeAutogeneratePrimaryKey.ToUpper().Contains(prop.Name.ToUpper()))
                                        {
                                            continue;
                                        }
                                    }
                                    oCmd.Parameters.AddWithValue("@" + prop.Name, value);
                                }

                                //oPropertyValue
                                //table.Columns.Add(prop.Name, prop.PropertyType);
                            }

                            oCmd.Transaction = oTransaction;
                            rowCount = oCmd.ExecuteNonQuery();
                        }
                        index++;

                    }
                    oTransaction.Commit();
                        result = true;
                        if (rowCount <= 0)
                        {
                            result = false;
                            ErrorMsg = "No record found for update";
                        }


                    }
                }
                catch (Exception ex)
                {
                    result = false;
                    ErrorMsg = ex.Message + "\r\n" + queryError;
                    if (oTransaction != null)
                        oTransaction.Rollback();
                }
                finally
                {
                    if (connection != null)
                        connection.Close();
                }




            return result;
        }


        /// <summary>
        /// Input a sql query and provide output datatable
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="msg"> if any error occured then provide the output</param>
        /// <returns>DataTable</returns>
        public DataTable Select(string SQL,ref string msg)
        {
            DataTable dataTable =null;
            SqlCommand oCmd = null;
            try
            {

                if (connection != null)
                {
                    connection.Open();
                    oCmd = new SqlCommand(SQL, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(oCmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    dataTable = dt;
                }
            }
            catch (Exception ex)
            {
                dataTable = null;
                msg = ex.Message;
            }
            finally
            {
                connection.Close();
            }
            return dataTable;
        }


        /// <summary>
        /// Input a sql query and provide output as Generic List
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="msg"> if any error occured then provide the output</param>
        /// <returns>List<T> </returns>
        public List<T> Select<T>(string SQL, ref string msg) where T : class, new()
        {
            try
            {
                DataTable dataTable = Select(SQL, ref msg);
                if (dataTable == null)
                    return null;

                List<T> list = new List<T>();

                foreach (var row in dataTable.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);

                            var value = row[prop.Name];
                            if (value == DBNull.Value)
                            {
                                value = null;
                            }
                            //propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                            prop.SetValue(obj, value, null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }



        /// <summary>
        /// Input a sql query and provide output as Single Object
        /// </summary>
        /// <param name="SQL"></param>
        /// <param name="msg"> if any error occured then provide the output</param>
        /// <returns> T </returns>
        public T SelectFirstOrDefault<T>(string SQL, ref string msg) where T : class, new()
        {
            try
            {
                DataTable dataTable = Select(SQL, ref msg);
                if (dataTable == null || dataTable.Rows.Count ==0)
                    return null;

                //List<T> list = new List<T>();

                var row = dataTable.Rows[0];
               
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);

                            var value = row[prop.Name];
                            if (value == DBNull.Value)
                            {
                                value = null;
                            }
                            //propertyInfo.SetValue(obj, Convert.ChangeType(row[prop.Name], propertyInfo.PropertyType), null);
                            prop.SetValue(obj, value, null);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                

                return obj;
            }
            catch
            {
                return null;
            }
        }



        /// <summary>
        ///
        /// </summary>
        /// <param name="coloumName">Table Column name where to get max value </param>
        /// <param name="rightStringLength"> sample data like 000 or 0000 or 00 </param>
        /// <param name="tableName"> Table name for get max </param>
        /// <param name="prefix"> Serail Prefix </param>
        /// <returns> string result of max data </returns>
        public string GetMaxId(string coloumName, string rightStringLength, string tableName, string prefix)
        {
            string id = "";

            string
            selectQuery = @"SELECT ISNULL(MAX( CAST(SUBSTRING( " + coloumName + " ," + (prefix.Length + 1) + ", LEN(" + coloumName + @") ) AS INT) ),0)
                            FROM " + tableName + @"
                            WHERE " + coloumName + " LIKE '" + prefix + "%' ";

            string msg = "";
            DataTable dataTable = Select(selectQuery, ref msg);
            if(dataTable==null || dataTable.Rows.Count == 0)
            {
                throw new Exception("Max generation fail, no record ,table or " + msg);
            }

            id = dataTable.Rows[0][0].ToString();
            //id = (decimal.Parse(initialValue) + decimal.Parse(id)).ToString();
            if (string.IsNullOrEmpty(id))
            {
                return prefix + decimal.Parse("1").ToString(rightStringLength);
            }
            else
            {
                //id = id.Substring(prefix.Length, id.Length - 1);
                decimal _id = decimal.Parse(id) + 1;
                return prefix + _id.ToString(rightStringLength);
            }


        }


    }
}