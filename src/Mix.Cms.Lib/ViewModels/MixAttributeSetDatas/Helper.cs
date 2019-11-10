﻿using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib.Helpers;
using Mix.Cms.Lib.Models.Cms;
using Mix.Common.Helper;
using Mix.Domain.Core.ViewModels;
using Mix.Domain.Data.Repository;
using Mix.Domain.Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Mix.Cms.Lib.ViewModels.MixAttributeSetDatas
{
    public class Helper
    {
        public static Task<RepositoryResponse<List<TView>>> FilterByValueAsync<TView>(string culture, string attributeSetName
            , Dictionary<string, Microsoft.Extensions.Primitives.StringValues> queryDictionary
            , MixCmsContext _context = null, IDbContextTransaction _transaction = null)
            where TView : ODataViewModelBase<MixCmsContext, MixAttributeSetData, TView>
        {
            UnitOfWorkHelper<MixCmsContext>.InitTransaction(_context, _transaction, out MixCmsContext context, out IDbContextTransaction transaction, out bool isRoot);
            try
            {
                Expression<Func<MixAttributeSetValue, bool>> valPredicate = m => m.Specificulture == culture && m.AttributeSetName == attributeSetName;
                RepositoryResponse<List<TView>> result = new RepositoryResponse<List<TView>>() {
                    IsSucceed = true,
                    Data = new List<TView>()
                };
                var tasks = new List<Task<RepositoryResponse<TView>>>();

                // Loop queries string => predicate
                foreach (var q in queryDictionary)
                {
                    if (!string.IsNullOrEmpty(q.Key) && !string.IsNullOrEmpty(q.Value))
                    {
                        Expression<Func<MixAttributeSetValue, bool>> pre = m => m.AttributeFieldName == q.Key && m.StringValue == q.Value;
                        valPredicate = ODataHelper<MixAttributeSetValue>.CombineExpression(valPredicate, pre, Microsoft.OData.UriParser.BinaryOperatorKind.And);
                    }
                }
                var query = context.MixAttributeSetValue.Where(valPredicate).Select(m => m.DataId).Distinct().ToList();
                if (query!=null)
                {
                    foreach (var item in query)
                    {
                        tasks.Add(Task.Run(async () => {
                            var resp = await ODataDefaultRepository<MixCmsContext, MixAttributeSetData, TView>.Instance.GetSingleModelAsync(
                                m => m.Id == item && m.Specificulture == culture);
                            return resp;
                        }));

                    }
                    var continuation = Task.WhenAll(tasks);
                    continuation.Wait();
                    if (continuation.Status == TaskStatus.RanToCompletion)
                    {
                        foreach (var data in continuation.Result)
                        {
                            if (data.IsSucceed)
                            {
                                result.Data.Add(data.Data);
                            }
                            else
                            {
                                result.Errors.AddRange(data.Errors);
                            }
                        }
                    }
                    // Display information on faulted tasks.
                    else
                    {
                        foreach (var t in tasks)
                        {
                            result.Errors.Add($"Task {t.Id}: {t.Status}");
                        }
                    }

                }
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromResult(UnitOfWorkHelper<MixCmsContext>.HandleException<List<TView>>(ex, isRoot, transaction));
            }
            finally
            {
                if (isRoot)
                {
                    //if current Context is Root
                    context.Dispose();
                }
            }
        }

        public static async Task<RepositoryResponse<PaginationModel<TView>>> FilterByKeywordAsync<TView>(string culture, string attributeSetName
            , RequestPaging request, string keyword
            , Dictionary<string, Microsoft.Extensions.Primitives.StringValues> queryDictionary = null
            , MixCmsContext _context = null, IDbContextTransaction _transaction = null)
            where TView : ViewModelBase<MixCmsContext, MixAttributeSetData, TView>
        {
            UnitOfWorkHelper<MixCmsContext>.InitTransaction(_context, _transaction, out MixCmsContext context, out IDbContextTransaction transaction, out bool isRoot);
            try
            {
                Expression<Func<MixAttributeSetValue, bool>> attrPredicate = m => m.Specificulture == culture && m.AttributeSetName == attributeSetName;
                Expression<Func<MixAttributeSetValue, bool>> valPredicate = null;
                RepositoryResponse<PaginationModel<TView>> result = new RepositoryResponse<PaginationModel<TView>>()
                {
                    IsSucceed = true,
                    Data = new PaginationModel<TView>()
                };
                var tasks = new List<Task<RepositoryResponse<TView>>>();
                if (queryDictionary != null)
                {
                    foreach (var q in queryDictionary)
                    {
                        if (!string.IsNullOrEmpty(q.Key) && q.Key != "attributeSetId" && q.Key != "attributeSetName" && !string.IsNullOrEmpty(q.Value))
                        {
                            Expression<Func<MixAttributeSetValue, bool>> pre = m => m.AttributeFieldName == q.Key && m.StringValue.Contains(q.Value.ToString());
                            if (valPredicate != null)
                            {
                                valPredicate = ODataHelper<MixAttributeSetValue>.CombineExpression(valPredicate, pre, Microsoft.OData.UriParser.BinaryOperatorKind.Or);
                            }
                            else
                            {
                                valPredicate = pre;
                            }
                        }
                    }
                    if (valPredicate != null)
                    {
                        attrPredicate = ODataHelper<MixAttributeSetValue>.CombineExpression(valPredicate, attrPredicate, Microsoft.OData.UriParser.BinaryOperatorKind.And);
                    }
                }
                // Loop queries string => predicate
                if (!string.IsNullOrEmpty(keyword))
                {
                    Expression<Func<MixAttributeSetValue, bool>> pre = m => m.AttributeSetName == attributeSetName && m.Specificulture == culture && m.StringValue.Contains(keyword);
                    attrPredicate = ODataHelper<MixAttributeSetValue>.CombineExpression(attrPredicate, pre, Microsoft.OData.UriParser.BinaryOperatorKind.And);
                }

                var query = context.MixAttributeSetValue.Where(attrPredicate).Select(m => m.DataId).Distinct();
                if (query != null)
                {
                    Expression<Func<MixAttributeSetData, bool>> predicate = m => query.Any(id => m.Id == id); 
                    result = await DefaultRepository<MixCmsContext, MixAttributeSetData, TView>.Instance.GetModelListByAsync(
                        predicate, request.OrderBy, request.Direction, request.PageSize, request.PageIndex, null, null, context, transaction);

                }
                return result;
            }
            catch (Exception ex)
            {
                return UnitOfWorkHelper<MixCmsContext>.HandleException<PaginationModel<TView>>(ex, isRoot, transaction);
            }
            finally
            {
                if (isRoot)
                {
                    //if current Context is Root
                    context.Dispose();
                }
            }
        }
    }
}
