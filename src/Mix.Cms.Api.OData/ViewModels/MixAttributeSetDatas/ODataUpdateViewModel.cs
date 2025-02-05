﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib.Models.Cms;
using Mix.Cms.Lib.Repositories;
using Mix.Common.Helper;
using Mix.Domain.Core.ViewModels;
using Mix.Domain.Data.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mix.Cms.Api.OData.ViewModels.MixAttributeSetDatas
{
    public class ODataUpdateViewModel
      : ODataViewModelBase<MixCmsContext, MixAttributeSetData, ODataUpdateViewModel>
    {
        #region Properties

        #region Models

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("attributeSetId")]
        public int AttributeSetId { get; set; }

        [JsonProperty("attributeSetName")]
        public string AttributeSetName { get; set; }

        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonProperty("createdBy")]
        public string CreatedBy { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        #endregion Models

        #region Views

        [JsonProperty("values")]
        public List<Lib.ViewModels.MixAttributeSetValues.UpdateViewModel> Values { get; set; }

        [JsonProperty("fields")]
        public List<Lib.ViewModels.MixAttributeFields.UpdateViewModel> Fields { get; set; }

        [JsonProperty("parentId")]
        public string ParentId { get; set; }
        [JsonProperty("parentType")]
        public MixEnums.MixAttributeSetDataType ParentType { get; set; }
        //[JsonProperty("dataNavs")]
        //public List<MixRelatedAttributeDatas.UpdateViewModel> DataNavs { get; set; }

        #endregion Views

        #endregion Properties

        #region Contructors

        public ODataUpdateViewModel() : base()
        {
        }

        public ODataUpdateViewModel(MixAttributeSetData model, MixCmsContext _context = null, IDbContextTransaction _transaction = null) : base(model, _context, _transaction)
        {
        }

        #endregion Contructors

        #region Overrides

        public override void ExpandView(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            // Related Datas
            //DataNavs = MixRelatedAttributeDatas.UpdateViewModel.Repository.GetModelListBy(
            //    n => n.ParentId == Id && n.ParentType == (int)MixEnums.MixAttributeSetDataType.Set && n.Specificulture == Specificulture,
            //    _context, _transaction).Data;

            Values = Lib.ViewModels.MixAttributeSetValues.UpdateViewModel
                .Repository.GetModelListBy(a => a.DataId == Id && a.Specificulture == Specificulture, _context, _transaction).Data.OrderBy(a => a.Priority).ToList();
            Fields = Lib.ViewModels.MixAttributeFields.UpdateViewModel.Repository.GetModelListBy(f => (f.AttributeSetId == AttributeSetId || f.AttributeSetName == AttributeSetName), _context, _transaction).Data;
            foreach (var field in Fields.OrderBy(f => f.Priority))
            {
                var val = Values.FirstOrDefault(v => v.AttributeFieldId == field.Id);
                if (val == null)
                {
                    val = new Lib.ViewModels.MixAttributeSetValues.UpdateViewModel(
                        new MixAttributeSetValue() { AttributeFieldId = field.Id }
                        , _context, _transaction)
                    {
                        Field = field,
                        DataType = field.DataType,
                        AttributeFieldName = field.Name,
                        AttributeSetName = field.AttributeSetName,
                        StringValue = field.DefaultValue,
                        Priority = field.Priority
                    };
                    Values.Add(val);
                }
                val.DataId = Id;
                val.Priority = field.Priority;
                val.Field = field;
            }
        }

        public override MixAttributeSetData ParseModel(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString();
                CreatedDateTime = DateTime.UtcNow;
            }
            var attr = _context.MixAttributeSet.FirstOrDefault(m => m.Id == AttributeSetId || m.Name == AttributeSetName);

            if (attr != null)
            {
                AttributeSetName = attr.Name;
                AttributeSetId = attr.Id;
            }
            
            HandleEdm(_context, _transaction);

            return base.ParseModel(_context, _transaction);
        }
        public override async Task<RepositoryResponse<ODataUpdateViewModel>> SaveModelAsync(bool isSaveSubModels = false, MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            UnitOfWorkHelper<MixCmsContext>.InitTransaction(_context, _transaction, out MixCmsContext context, out IDbContextTransaction transaction, out bool isRoot);
            try
            {
                var result = await base.SaveModelAsync(isSaveSubModels, context, transaction);
                // if save current data success and there is related parent data
                if (result.IsSucceed && !string.IsNullOrEmpty(ParentId))
                {
                    Lib.ViewModels.MixRelatedAttributeDatas.UpdateViewModel nav = new Lib.ViewModels.MixRelatedAttributeDatas.UpdateViewModel()
                    {
                        Id = result.Data.Id,
                        Specificulture = Specificulture,
                        AttributeSetId = result.Data.AttributeSetId,
                        AttributeSetName = result.Data.AttributeSetName,
                        ParentId = ParentId,
                        ParentType = ParentType
                    };
                    var saveNav = await nav.SaveModelAsync(true, context, transaction);
                    result.IsSucceed = result.IsSucceed && saveNav.IsSucceed;
                    result.Errors = saveNav.Errors;
                    result.Exception = saveNav.Exception;
                }
                UnitOfWorkHelper<MixCmsContext>.HandleTransaction(result.IsSucceed, isRoot, transaction);
                return result;
            }
            catch (Exception ex)
            {
                return UnitOfWorkHelper<MixCmsContext>.HandleException<ODataUpdateViewModel>(ex, isRoot, transaction);
            }
            finally
            {
                if (isRoot)
                {
                    context.Database.CloseConnection();transaction.Dispose();context.Dispose();
                }

            }
        }
        public override async Task<RepositoryResponse<bool>> SaveSubModelsAsync(MixAttributeSetData parent, MixCmsContext _context, IDbContextTransaction _transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };
            if (result.IsSucceed)
            {
                foreach (var item in Values)
                {
                    if (result.IsSucceed)
                    {
                        item.Priority = item?.Field?.Priority ?? item.Priority;
                        item.DataId = parent.Id;
                        item.AttributeSetName = parent.AttributeSetName;
                        item.Specificulture = parent.Specificulture;
                        var saveResult = await item.SaveModelAsync(false, _context, _transaction);
                        ViewModelHelper.HandleResult(saveResult, ref result);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public override RepositoryResponse<bool> SaveSubModels(MixAttributeSetData parent, MixCmsContext _context, IDbContextTransaction _transaction)
        {
            var result = new RepositoryResponse<bool>() { IsSucceed = true };
            if (result.IsSucceed)
            {
                foreach (var item in Values)
                {
                    if (result.IsSucceed)
                    {
                        item.Priority = item.Field.Priority;
                        item.DataId = parent.Id;
                        item.Specificulture = parent.Specificulture;
                        var saveResult = item.SaveModel(false, _context, _transaction);
                        ViewModelHelper.HandleResult(saveResult, ref result);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return result;
        }

        //public override List<Task> GenerateRelatedData(MixCmsContext context, IDbContextTransaction transaction)
        //{
        //    var tasks = new List<Task>();
        //    //var attrDatas = context.MixAttributeSetData.Include(m => m.MixRelatedAttributeData).Where(m => m.MixRelatedAttributeData
        //    //    .Any(d => d.Specificulture == Specificulture && d.Id == Id));
        //    var relatedData = context.MixRelatedAttributeData.Include(m => m.MixAttributeSetData).Where(m => m.Specificulture == Specificulture && (m.Id == Id || m.ParentId == Id));
        //    foreach (var item in relatedData)
        //    {
        //        tasks.Add(Task.Run(() =>
        //        {
        //            ReadViewModel.Repository.RemoveCache(item.MixAttributeSetData, context, transaction);
        //            MixRelatedAttributeDatas.ReadViewModel.Repository.RemoveCache(item, context, transaction);
        //        }));
        //    }
        //    foreach (var item in Values)
        //    {
        //        tasks.Add(Task.Run(() =>
        //        {
        //            item.RemoveCache(item.Model);
        //        }));
        //    }
        //    return tasks;
        //}

        private void HandleEdm(MixCmsContext _context, IDbContextTransaction _transaction)
        {
            var getAttrSet = Lib.ViewModels.MixAttributeSets.ReadViewModel.Repository.GetSingleModel(m => m.Name == AttributeSetName || m.Id == AttributeSetId, _context, _transaction);
            if (!string.IsNullOrEmpty(getAttrSet.Data.EdmSubject))
            {
                var getEdm = Lib.ViewModels.MixTemplates.UpdateViewModel.GetTemplateByPath(getAttrSet.Data.EdmTemplate, Specificulture);
                if (getEdm.IsSucceed && !string.IsNullOrEmpty(getEdm.Data.Content))
                {
                    string body = getEdm.Data.Content;
                    foreach (var prop in Fields)
                    {
                        var val = GetValue(prop.Name);
                        body = body.Replace($"[[{prop.Name}]]", val.StringValue);
                    }
                    var edmFile = new Lib.ViewModels.FileViewModel()
                    {
                        Content = body,
                        Extension = ".html",
                        FileFolder = "edms",
                        Filename = $"{getAttrSet.Data.EdmSubject}-{Id}"
                    };
                    if (FileRepository.Instance.SaveWebFile(edmFile))
                    {
                        var val = GetValue("edm");
                        if (val != null)
                        {
                            val.StringValue = edmFile.WebPath;
                        }
                    }
                }
            }
        }

        public Lib.ViewModels.MixAttributeSetValues.UpdateViewModel GetValue(string fieldName)
        {
            return Values?.FirstOrDefault(v => v.AttributeFieldName == fieldName);
        }

        #endregion Overrides
    }
}