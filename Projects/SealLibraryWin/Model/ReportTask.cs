﻿//
// Copyright (c) Seal Report (sealreport@gmail.com), http://www.sealreport.org.
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. http://www.apache.org/licenses/LICENSE-2.0..
//
using System;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.ComponentModel;
using Seal.Helpers;
using System.Data.Common;
using System.Threading;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
#if WINDOWS
using DynamicTypeDescriptor;
using System.Drawing.Design;
using Seal.Forms;
#endif

namespace Seal.Model
{
    /// <summary>
    /// A ReportTask defines the exection of a task: SQL statement or Razor script
    /// </summary>
    public class ReportTask : ReportComponent
#if WINDOWS
        , ITreeSort 
#endif
    {
#if WINDOWS
        #region Editor

        protected override void UpdateEditorAttributes()
        {
            if (_dctd != null)
            {
                //Disable all properties
                foreach (var property in Properties) property.SetIsBrowsable(false);
                //Then enable
                GetProperty("SourceGUID").SetIsBrowsable(true);
                GetProperty("ConnectionGUID").SetIsBrowsable(true);
                GetProperty("SQL").SetIsBrowsable(true);
                GetProperty("Enabled").SetIsBrowsable(true);
                GetProperty("IgnoreError").SetIsBrowsable(true);
                GetProperty("Script").SetIsBrowsable(true);
                GetProperty("ExecuteForEachConnection").SetIsBrowsable(true);
                GetProperty("Step").SetIsBrowsable(true);

                //Helpers
                //GetProperty("Information").SetIsBrowsable(true);
                //GetProperty("Error").SetIsBrowsable(true);

                //Readonly

                TypeDescriptor.Refresh(this);
            }
        }
        #endregion

#endif

        /// <summary>
        /// Creates a basic ReportTask
        /// </summary>
        public static ReportTask Create()
        {
            return new ReportTask() { GUID = Guid.NewGuid().ToString() };
        }


        /// <summary>
        /// Init all references of the task
        /// </summary>
        public void InitReferences()
        {
        }

        /// <summary>
        /// Identifier of the current report source
        /// </summary>
#if WINDOWS
        [DefaultValue(null)]
        [Category("Definition"), DisplayName("Source"), Description("The source used by the task."), Id(1, 1)]
        [TypeConverter(typeof(MetaSourceConverter))]
#endif
        public string SourceGUID { get; set; }

        protected string _connectionGUID = ReportSource.DefaultReportConnectionGUID;

        /// <summary>
        /// The connection identifier used by the task
        /// </summary>
#if WINDOWS
        [DefaultValue(ReportSource.DefaultReportConnectionGUID)]
        [DisplayName("Connection"), Description("The connection used by the task."), Category("Definition"), Id(2, 1)]
        [TypeConverter(typeof(SourceConnectionConverter))]
#endif
        public string ConnectionGUID
        {
            get
            {
                if (_connectionGUID != ReportSource.DefaultReportConnectionGUID && _connectionGUID != ReportSource.DefaultRepositoryConnectionGUID)
                {
                    //reset it if not found in current connections
                    if (Source != null && !Source.Connections.Exists(i => i.GUID == _connectionGUID) && !Source.TempConnections.Exists(i => i.GUID == _connectionGUID)) _connectionGUID = ReportSource.DefaultReportConnectionGUID;
                }
                return _connectionGUID;
            }
            set { _connectionGUID = value; }
        }

        /// <summary>
        /// If false, the task is ignored and not executed
        /// </summary>
#if WINDOWS
        [DefaultValue(true)]
        [Category("Definition"), DisplayName("Is Enabled"), Description("If false, the task is ignored and not executed."), Id(3, 1)]
#endif
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The Report Execution Step to execute the task. By default, tasks are executed before the models generation.
        /// </summary>
#if WINDOWS
        [DefaultValue(ExecutionStep.BeforeModel)]
        [Category("Definition"), DisplayName("Execution Step"), Description("The Report Execution Step to execute the task. By default, tasks are executed before the models generation."), Id(4, 1)]
        [TypeConverter(typeof(NamedEnumConverter))]
#endif
        public ExecutionStep Step { get; set; } = ExecutionStep.BeforeModel;

        /// <summary>
        /// Current MetaConnection
        /// </summary>
        [XmlIgnore]
        public MetaConnection Connection
        {
            get
            {
                MetaConnection result = Source.Connection;
                if (_connectionGUID == ReportSource.DefaultReportConnectionGUID) result = Source.Connection;
                else if (_connectionGUID == ReportSource.DefaultRepositoryConnectionGUID) result = Source.RepositoryConnection;
                else result = Source.Connections.FirstOrDefault(i => i.GUID == _connectionGUID);
                if (result == null && Source.Connections.Count > 0)
                {
                    result = Source.Connections[0];
                }
                return result;
            }
        }

        /// <summary>
        /// Current ReportSource
        /// </summary>
        [XmlIgnore]
        public ReportSource Source
        {
            get
            {
                ReportSource result = _report.Sources.FirstOrDefault(i => i.GUID == SourceGUID);
                if (result == null)
                {
                    if (_report.Sources.Count == 0) throw new Exception("This report has no source defined");
                    result = _report.Sources[0];
                    SourceGUID = result.GUID;
                }
                return result;
            }
        }

        /// <summary>
        /// Current Repository
        /// </summary>
        [XmlIgnore]
        public Repository Repository
        {
            get
            {
                if (_report != null) return _report.Repository;
                return null;
            }
        }

        /// <summary>
        /// SQL Statement executed for the task. It may be empty if a Razor Script is defined. The statement may contain Razor script if it starts with '@'. If the SQL result returns 0, the report is cancelled and the next tasks are not executed.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("SQL Statement"), Description("SQL Statement executed for the task. It may be empty if a Razor Script is defined. The statement may contain Razor script if it starts with '@'. If the SQL result returns 0, the report is cancelled and the next tasks are not executed."), Id(5, 1)]
        [Editor(typeof(SQLEditor), typeof(UITypeEditor))]
#endif
        public string SQL { get; set; }

        /// <summary>
        /// Razor script executed for the Task. It may be empty if the SQL Script is defined. If the script returns 0, the report is cancelled and the next tasks are not executed.
        /// </summary>
#if WINDOWS
        [Category("Definition"), DisplayName("Script"), Description("Razor script executed for the Task. It may be empty if the SQL Script is defined. If the script returns 0, the report is cancelled and the next tasks are not executed."), Id(6, 1)]
        [Editor(typeof(TemplateTextEditor), typeof(UITypeEditor))]
#endif
        public string Script { get; set; }

        /// <summary>
        /// If true, errors occuring during the task execution are ignored and the report execution continues
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Ignore Errors"), Description("If true, errors occuring during the task execution are ignored and the report execution continues."), Id(2, 2)]
#endif
        public bool IgnoreError { get; set; } = false;

        /// <summary>
        /// If true, the task will be executed for each connection defined in the Data Source. If false, only the current connection is used.
        /// </summary>
#if WINDOWS
        [DefaultValue(false)]
        [Category("Options"), DisplayName("Execute for each connection"), Description("If true, the task will be executed for each connection defined in the Data Source. If false, only the current connection is used."), Id(3, 2)]
#endif
        public bool ExecuteForEachConnection { get; set; } = false;

        /// <summary>
        /// Order of the task amongst the tasks of the report
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Custom Tag the can be used at execution time to store any object
        /// </summary>
        [XmlIgnore]
        public object Tag;

        /// <summary>
        /// Returns the order of the task
        /// </summary>
        public int GetSort()
        {
            return SortOrder;
        }

        /// <summary>
        /// Current progression of the task in percentage
        /// </summary>
        [XmlIgnore]
        public int Progression = 0;

        /// <summary>
        /// The current report execution executing the task
        /// </summary>
        [XmlIgnore]
        public ReportExecution Execution;

#region Helpers

        /// <summary>
        /// Last information message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Information"), Description("Last information message."), Id(20, 20)]
        [EditorAttribute(typeof(InformationUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Information { get; set; }

        /// <summary>
        /// Last error message
        /// </summary>
#if WINDOWS
        [Category("Helpers"), DisplayName("Error"), Description("Last error message."), Id(20, 20)]
        [EditorAttribute(typeof(ErrorUITypeEditor), typeof(UITypeEditor))]
#endif
        [XmlIgnore]
        public string Error { get; set; }
        public bool CancelReport = false;

#endregion

#region Database

        DbCommand _command = null;
        Mutex _commandMutex = new Mutex();

        /// <summary>
        /// Information message for database
        /// </summary>
        [XmlIgnore]
        public StringBuilder DbInfoMessage = new StringBuilder();

        /// <summary>
        /// Log Interface implementation
        /// </summary>
        public void LogMessage(string message, params object[] args)
        {
            Report.LogMessage(message, args);
        }

        /// <summary>
        /// Cancel the task
        /// </summary>
        public void Cancel()
        {
            CancelReport = true;
            LogMessage("Cancelling task and report...");
            if (_commandMutex.WaitOne(1000))
            {
                try
                {
                    if (_command != null)
                    {
                        _command.Cancel();
                    }
                }
                finally
                {
                    _commandMutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Returns a DbCommand from a MetaConnection
        /// </summary>
        public DbCommand GetDbCommand(MetaConnection metaConnection)
        {
            DbCommand result = null;
            if (_commandMutex.WaitOne(1000))
            {
                try
                {
                    DbConnection connection = metaConnection.GetOpenConnection();
                    if (connection is OdbcConnection)
                    {
                        ((OdbcConnection)connection).InfoMessage += new OdbcInfoMessageEventHandler(OdbcInfoMessage);
                        result = ((OdbcConnection)connection).CreateCommand();
                    }
                    else if (connection is SqlConnection)
                    {
                        ((SqlConnection)connection).InfoMessage += new SqlInfoMessageEventHandler(SqlInfoMessage);
                        result = ((SqlConnection)connection).CreateCommand();
                    }
                    else
                    {
                        ((OleDbConnection)connection).InfoMessage += new OleDbInfoMessageEventHandler(OleDbInfoMessage);
                        result = ((OleDbConnection)connection).CreateCommand();
                    }
                    result.CommandTimeout = 0;
                }
                finally
                {
                    _commandMutex.ReleaseMutex();
                }
            }
            else
            {
                throw new Exception("Unable to get task command mutex...");
            }
            return result;
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        public void Execute()
        {
            CancelReport = false;
            DbInfoMessage = new StringBuilder();
            //Temp list to avoid change of connections during a task...
            var connections = Source.Connections.Where(i => ExecuteForEachConnection || i.GUID == Connection.GUID).ToList();
            foreach (var connection in connections)
            {
                Execute(connection);
            }
        }

        /// <summary>
        /// Executes the task with a given connection
        /// </summary>
        /// <param name="currentConnection"></param>
        public void Execute(MetaConnection currentConnection)
        {
            LogMessage("Starting task with connection '{0}'", currentConnection.Name);
            Progression = 0;
            if (!Report.Cancel && !string.IsNullOrEmpty(SQL))
            {
                _command = GetDbCommand(currentConnection);
                object sqlResult = null;
                try
                {
                    string finalSql = RazorHelper.CompileExecute(SQL, this);
                    LogMessage("Executing SQL: {0}", finalSql);
                    _command.CommandText = finalSql;
                    sqlResult = _command.ExecuteScalar();
                }
                finally
                {
                    _command.Connection.Close();
                }

                if (sqlResult != null && !(sqlResult is DBNull))
                {
                    if (sqlResult.ToString().Trim() == "0")
                    {
                        LogMessage("SQL returns 0, the report is cancelled.");
                        CancelReport = true;
                    }
                }
            }

            if (!Report.Cancel && !string.IsNullOrEmpty(Script))
            {
                LogMessage("Executing Script...");
                string result = RazorHelper.CompileExecute(Script, this);
                if (result.Trim() == "0")
                {
                    LogMessage("Script returns 0, the report is cancelled.");
                    CancelReport = true;
                }
            }

            Progression = 100; //100%
        }

        void OleDbInfoMessage(object sender, OleDbInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }

        void OdbcInfoMessage(object sender, OdbcInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }
        void SqlInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            DbInfoMessage.Append(e.Message);
        }
#endregion
    }
}

