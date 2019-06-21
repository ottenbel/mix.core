﻿using Microsoft.EntityFrameworkCore.Storage;
using Mix.Cms.Lib.Models.Cms;
using Mix.Cms.Lib.ViewModels.MixAttributeSets;
using Mix.Domain.Core.ViewModels;
using Mix.Domain.Data.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mix.Cms.Lib.ViewModels.MixArticleAttributeDatas
{
    public class ReadMvcViewModel
       : ViewModelBase<MixCmsContext, MixArticleAttributeData, ReadMvcViewModel>
    {
        #region Properties
        #region Models
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("attributeSetId")]
        public int AttributeSetId { get; set; }
        [JsonProperty("articleId")]
        public int ArticleId { get; set; }
        #endregion

        #region Views
        [JsonProperty("data")]
        public List<MixArticleAttributeValues.ReadMvcViewModel> Data{ get; set; }
        #endregion

        #endregion
        public ReadMvcViewModel(MixArticleAttributeData model, MixCmsContext _context = null, IDbContextTransaction _transaction = null)
            : base(model, _context, _transaction)
        {
        }

        public ReadMvcViewModel() : base()
        {
        }
        
        #region overrides

        public override void ExpandView(MixCmsContext _context = null, IDbContextTransaction _transaction = null)
        {
            Data = MixArticleAttributeValues.ReadMvcViewModel.Repository.GetModelListBy(
                    v => v.DataId == Id && v.Specificulture == Specificulture, _context, _transaction).Data;
        }

        #endregion overrides
        #region Helper

        public string GetStringValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.StringValue;
        }

        public DateTime? GetDateTimeValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.DateTimeValue;
        }

        public bool? GetBooleanValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.BooleanValue;
        }

        public int? GetIntegerValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.IntegerValue;
        }

        public double? GetDoubleValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.DoubleValue;
        }

        public string GetDecryptValue(string name)
        {
            return Data.FirstOrDefault(v => v.AttributeName == name)?.DecryptValue;
        }
        #endregion

    }
}
