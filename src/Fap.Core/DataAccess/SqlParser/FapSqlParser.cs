﻿using Fap.Core.Extensions;
using Fap.Core.Infrastructure.Domain;
using Fap.Core.Infrastructure.Metadata;
using Fap.Core.MultiLanguage;
using SQLGeneration.Builders;
using SQLGeneration.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fap.Core.DataAccess.SqlParser
{
    /// <summary>
    /// 简单SQL语句解析器
    /// </summary>
    public class FapSqlParser
    {
        private string _sql = "";
        private bool _withMC = false; //是否取参照的名称
        private bool _withId = false; //是否取参照的ID

        private IFapPlatformDomain _appDomain;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="sql">要解析的SQL语句</param>
        /// <param name="withMC">是否要计算MC字段</param>
        /// <param name="needEnabledFilter">是否启用Enable过滤，默认为启用</param>
        public FapSqlParser(IFapPlatformDomain platformDomain, string sql, bool withMC = false, bool withId = false)
        {
            this._sql = sql;
            _withId = withId;
            _withMC = withMC;
            _appDomain = platformDomain;
        }
        /// <summary>
        /// jqGrid查询的时候不用关联dict
        /// </summary>
        public bool IsGridQuery { get; set; }
        public string CurrentLang { get; set; } = MultiLanguageEnum.ZhCn.ToString();
        /// <summary>
        /// 获得解析完整的SQL语句
        /// </summary>
        /// <returns></returns>
        public string ParserSqlStatement()
        {
            //解析SQL语句
            CommandBuilder commandBuilder = new CommandBuilder();
            ICommand command = commandBuilder.GetCommand(_sql);

            if (command is SelectBuilder)
            {
                SelectBuilder select = command as SelectBuilder;
                RepackSelectSQL(select);
            }
            else if (command is UpdateBuilder)
            {
                UpdateBuilder update = command as UpdateBuilder;
                RepackUpdateSQL(update);
            }
            else if (command is DeleteBuilder)
            {
                DeleteBuilder delete = command as DeleteBuilder;
                RepackDeleteSQL(delete);
            }
            else if (command is InsertBuilder)
            {
                InsertBuilder insert = command as InsertBuilder;
                RepackInsertSQL(insert);
            }
            Formatter formatter = new Formatter();
            string newSql = formatter.GetCommandText(command);

            return newSql;
        }
        /// <summary>
        /// 获得解析select语句不在主where部分加有效期限制
        /// </summary>
        /// <returns></returns>
        public string ParserSelectSqlNoWhere()
        {
            //解析SQL语句
            CommandBuilder commandBuilder = new CommandBuilder();
            ICommand command = commandBuilder.GetCommand(_sql);

            if (command is SelectBuilder)
            {
                SelectBuilder select = command as SelectBuilder;
                var projections = select.Projection;
                int fieldCount = projections.Count();
                if (fieldCount > 0)
                {
                    var arryProjections = projections.ToArray();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        IProjectionItem item = arryProjections[i].ProjectionItem;
                        if (item is AllColumns)//*
                        {
                            AllColumns currItem = item as AllColumns;
                            //移除掉重新构建
                            select.RemoveProjection(arryProjections[i]);
                            HandleSelectStarStatement(select, currItem);

                        }
                        else if (item is Column)
                        {
                            Column currItem = item as Column;
                            HandleSelectStatement(select, arryProjections[i], currItem);
                            i++;
                        }
                    }
                }
                Formatter formatter = new Formatter();
                string newSql = formatter.GetCommandText(command);

                return newSql;
            }
            return string.Empty;
        }


        private void RepackInsertSQL(InsertBuilder insert)
        {
            var provider = insert.Values;
            if (provider != null && provider is SelectBuilder)
            {
                var selectBuilder = provider as SelectBuilder;
                RepackSelectSQL(selectBuilder);
            }
        }
        private void RepackDeleteSQL(DeleteBuilder delete)
        {
            var table = delete.Table;
            var where = delete.Where;
            HandlerInFilterWhere(where);
            //添加有效期验证
            FilterGroup validFilter = new FilterGroup(Conjunction.And,
               new LessThanEqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
               new GreaterThanEqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
               new EqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
            delete.AddWhere(validFilter);
        }
        private void RepackUpdateSQL(UpdateBuilder update)
        {
            var table = update.Table;
            var where = update.Where;
            HandlerInFilterWhere(where);
            //添加有效期验证
            FilterGroup validFilter = new FilterGroup(Conjunction.And,
               new LessThanEqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
               new GreaterThanEqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
               new EqualToFilter(table.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
            update.AddWhere(validFilter);
        }
        /// <summary>
        /// 处理In中的select
        /// </summary>
        /// <param name="where"></param>
        private void HandlerInFilterWhere(IEnumerable<IFilter> where)
        {
            if (where.Any())
            {
                foreach (IFilter filter in where)
                {
                    if (filter is InFilter)
                    {
                        InFilter infilter = filter as InFilter;
                        if (infilter.Values is SelectBuilder)
                        {
                            SelectBuilder inSel = infilter.Values as SelectBuilder;
                            var intb = inSel.Sources.Sources;
                            if (intb.Any())
                            {
                                foreach (AliasedSource tb in intb)
                                {
                                    //添加有效期验证
                                    FilterGroup inFilter = new FilterGroup(Conjunction.And,
                                       new LessThanEqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                                       new GreaterThanEqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                                       new EqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
                                    inSel.AddWhere(inFilter);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 重新组合SQL语句(带上有效时间和删除标识)
        /// </summary>
        ///<param name="select"></param>
        private void RepackSelectSQL(SelectBuilder select)
        {
            var projections = select.Projection;
            var tables = select.Sources;
            var where = select.Where;
            int fieldCount = projections.Count();
            if (fieldCount > 0)
            {
                var arryProjections = projections.ToArray();
                for (int i = 0; i < fieldCount; i++)
                {
                    IProjectionItem item = arryProjections[i].ProjectionItem;
                    if (item is AllColumns)//*
                    {
                        AllColumns currItem = item as AllColumns;
                        //移除掉重新构建
                        select.RemoveProjection(arryProjections[i]);
                        HandleSelectStarStatement(select, currItem);

                    }
                    else if (item is Column)
                    {
                        Column currItem = item as Column;
                        HandleSelectStatement(select, arryProjections[i], currItem);
                    }
                }
            }
            HandlerInFilterWhere(where);

            ParserWhere(select);
        }

        private static void ParserWhere(SelectBuilder select)
        {
            foreach (AliasedSource tb in select.Sources.Sources)
            {
                //添加有效期验证
                FilterGroup validFilter = new FilterGroup(Conjunction.And,
                   new LessThanEqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                   new GreaterThanEqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                   new EqualToFilter(tb.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
                select.AddWhere(validFilter);
            }
        }



        /// <summary>
        /// 处理Select语句：select *
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        private void HandleSelectStarStatement(SelectBuilder select, AllColumns aliSource)
        {
            AliasedSource tableSource = select.Sources.Sources.First();
            if (aliSource.Source != null) //有字段的前缀
            {
                tableSource = select.Sources.Sources.First();
            }
            string tableName = tableSource.Source.GetSourceName();
            IEnumerable<FapColumn> columns = GetColumnsOfTable(tableName);
            if (columns != null)
            {
                this.MakeSelectStarPartition(columns, tableSource, select);
            }
        }

        /// <summary>
        ///  处理Select语句：有明确字段的SELECT语句
        /// </summary>
        /// <param name="tables"></param>
        /// <param name="field"></param>
        /// <param name="builder"></param>
        private void HandleSelectStatement(SelectBuilder select, AliasedProjection currProjection, Column column)
        {
            string colName = column.Name;
            //string tableAlias = column.Source.Alias;
            string tableName = column.Source.Source.GetSourceName();
            FapColumn fCol = GetSingleColumnOfTable(tableName, colName);
            if (fCol.IsMultiLang == 1 && IsGridQuery)
            {
                select.RemoveProjection(currProjection);
                select.AddProjection(column.Source.Column($"{colName}{CurrentLang}"), colName);
            }
            if (_withMC)
            {
                this.MakeSelectPartition(fCol, select, column);

            }

        }

        /// <summary>
        /// 获取表的所有字段集合
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private IEnumerable<FapColumn> GetColumnsOfTable(string table)
        {
            _appDomain.ColumnSet.TryGetValueByTable(table, out IEnumerable<FapColumn> fapCols);
            return fapCols;
        }

        /// <summary>
        /// 获取指定字段对象
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        private FapColumn GetSingleColumnOfTable(string table, string column)
        {
            return GetColumnsOfTable(table).FirstOrDefault<FapColumn>(c => c.ColName.Equals(column, StringComparison.OrdinalIgnoreCase));
        }



        /// <summary>
        /// 处理SELECT部分
        /// </summary>
        /// <param name="columnList"></param>
        /// <param name="table"></param>
        /// <param name="selectBuilder"></param>
        private void MakeSelectStarPartition(IEnumerable<FapColumn> columnList, AliasedSource aliaseSource, SelectBuilder select)
        {
            string tableAlias = aliaseSource.Alias;
            int mcIndex = 0;
            foreach (var column in columnList)
            {
                //AliasedSource table = select.AddTable(new Table($"{column.TableName}"), tableAlias);
                if (column.IsMultiLang == 1)
                {
                    if (IsGridQuery)
                    {
                        select.AddProjection(aliaseSource.Column($"{column.ColName}{CurrentLang}"), column.ColName);
                    }
                    else
                    {
                        select.AddProjection(aliaseSource.Column(column.ColName));
                    }
                    var langs = typeof(MultiLanguageEnum).EnumItems();
                    foreach (var lang in langs)
                    {
                        select.AddProjection(aliaseSource.Column($"{column.ColName}{lang.Value}"), $"{column.ColName}{lang.Value}");
                    }
                }
                else
                {
                    select.AddProjection(aliaseSource.Column(column.ColName));
                }
                if (this._withMC)
                {
                    //处理MC字段
                    if (FapColumn.CTRL_TYPE_COMBOBOX == column.CtrlType && !IsGridQuery)
                    {
                        string refAlias = "b" + (mcIndex++);
                        BuildFapDict(select, column, aliaseSource, refAlias);

                    }
                    else if (FapColumn.CTRL_TYPE_REFERENCE == column.CtrlType)
                    {
                        string refAlias = "b" + (mcIndex++);
                        BuildRefrenceName(select, column, aliaseSource, refAlias);
                    }
                }

                //处理MC字段的ID字段
                if (this._withId)
                {
                    if (FapColumn.CTRL_TYPE_REFERENCE == column.CtrlType)
                    {
                        string refAlias = "b" + (mcIndex++);
                        BuildRefrenceCode(select, column, aliaseSource, refAlias);
                    }
                }
            }
        }

        private static void BuildRefrenceCode(SelectBuilder select, FapColumn column, AliasedSource table, string refAlias)
        {
            string colName = column.RefCode;
            if (colName.IsPresent())
            {
                colName = "Id";
            }
            SelectBuilder inner = new SelectBuilder();
            AliasedSource innerTable = inner.AddTable(new Table($"{column.RefTable}"), refAlias);
            inner.AddProjection(innerTable.Column($"{colName}"));
            if (column.MultiAble == 1) //是否多选
            {

            }
            FilterGroup joinFilter = new FilterGroup(Conjunction.And,
                new EqualToFilter(innerTable.Column($"{column.RefID}"), table.Column(column.ColName)),
                new LessThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new GreaterThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new EqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
            if (column.RefTable.EqualsWithIgnoreCase(nameof(FapColumn))&&column.RefID!="Fid")
            {
                IFilter filter = new EqualToFilter(innerTable.Column("TableName"), table.Column("TableName"));
                joinFilter.AddFilter(filter);
            }
            inner.AddWhere(joinFilter);
            select.AddProjection(inner, $"{column.ColName}MCID");
        }

        private static void BuildRefrenceName(SelectBuilder select, FapColumn column, AliasedSource table, string refAlias)
        {
            SelectBuilder inner = new SelectBuilder();
            AliasedSource innerTable = inner.AddTable(new Table($"{column.RefTable}"), refAlias);
            inner.AddProjection(innerTable.Column($"{column.RefName}"));

            FilterGroup joinFilter = new FilterGroup(Conjunction.And,
                new EqualToFilter(innerTable.Column($"{column.RefID}"), table.Column(column.ColName)),
                new LessThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new GreaterThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new EqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));

            if (column.RefTable.EqualsWithIgnoreCase(nameof(FapColumn)) && column.RefID != "Fid")
            {
                //fapcolumn存在重复colName，加一个去重
                IFilter filter = new EqualToFilter(innerTable.Column("TableName"), table.Column("RefTable"));
                joinFilter.AddFilter(filter);
            }
            inner.AddWhere(joinFilter);
            select.AddProjection(inner, $"{column.ColName}MC");


        }

        private static void BuildFapDict(SelectBuilder select, FapColumn column, AliasedSource table, string refAlias)
        {
            SelectBuilder inner = new SelectBuilder();
            AliasedSource innerTable = inner.AddTable(new Table("FapDict"), refAlias);
            inner.AddProjection(innerTable.Column("Name"));
            if (column.MultiAble == 1) //是否多选
            {

            }
            FilterGroup joinFilter = new FilterGroup(Conjunction.And,
                new EqualToFilter(innerTable.Column("Code"), table.Column(column.ColName)),
                new EqualToFilter(innerTable.Column("Category"), new StringLiteral(column.ComboxSource)),
                new LessThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_EnableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new GreaterThanEqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_DisableDate), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_CurrentDate)),
                new EqualToFilter(innerTable.Column(FapDbConstants.FAPCOLUMN_FIELD_Dr), new ParameterLiteral(FapDbConstants.FAPCOLUMN_PARAM_Dr)));
            inner.AddWhere(joinFilter);
            select.AddProjection(inner, $"{column.ColName}MC");
            if (column.MultiAble == 1) //是否多选
            {
                //selectBuilder.Append(" for xml path('')"); // 逗号分隔的字符串，比如，以“aaa,bbb,ccc,”显示
            }
        }

        /// <summary>
        /// 处理SELECT部分
        /// </summary>
        /// <param name="columnList"></param>
        /// <param name="table"></param>
        /// <param name="selectBuilder"></param>
        private void MakeSelectPartition(FapColumn fCol, SelectBuilder select, Column column)
        {
            string tableAlias = column.Source.Alias;
            int mcIndex = 0;
            //AliasedSource table = select.AddTable(new Table($"{fCol.TableName}"), tableAlias);
            //select.AddProjection(table.Column(fCol.ColName));
            //处理MC字段
            if (FapColumn.CTRL_TYPE_COMBOBOX == fCol.CtrlType && !IsGridQuery)
            {
                string refAlias = "b" + (mcIndex++);

                BuildFapDict(select, fCol, column.Source, refAlias);
            }
            else if (FapColumn.CTRL_TYPE_REFERENCE == fCol.CtrlType)
            {
                string refAlias = "b" + (mcIndex++);
                BuildRefrenceName(select, fCol, column.Source, refAlias);
            }

            //处理MC字段的ID字段
            if (this._withId)
            {
                if (FapColumn.CTRL_TYPE_REFERENCE == fCol.CtrlType)
                {
                    string refAlias = "b" + (mcIndex++);
                    BuildRefrenceCode(select, fCol, column.Source, refAlias);
                }
            }
        }

    }


}
