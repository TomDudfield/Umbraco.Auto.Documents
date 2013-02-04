using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace AutoDocuments
{
    public class AutoDocuments
    {
        public AutoDocuments(List<string> itemDocumentTypes, string itemDateProperty, string dateDocumentType, bool createDayDocuments)
        {
            ItemDocumentTypes = itemDocumentTypes;
            ItemDateProperty = itemDateProperty;
            DateDocumentType = dateDocumentType;
            CreateDayDocuments = createDayDocuments;
        }

        public List<string> ItemDocumentTypes { get; private set; }

        public string ItemDateProperty { get; private set; }

        public string DateDocumentType { get; private set; }

        public bool CreateDayDocuments { get; private set; }

        public void SetDocumentDate(IContentService contentService, IContent content)
        {
            if (!ItemDocumentTypes.Contains(content.ContentType.Alias))
                return;
            if (content.Properties[ItemDateProperty] == null)
                return;

            content.Properties[ItemDateProperty].Value = DateTime.Today.Date;
            contentService.Save(content);
        }

        public void BeforeDocumentPublish(IContentService contentService, IContent content)
        {
            if (!ItemDocumentTypes.Contains(content.ContentType.Alias))
                return;
            if (content.Properties[ItemDateProperty] == null || content.Properties[ItemDateProperty].Value == null)
                return;
                
            LogHelper.Debug<AutoDocuments>(string.Format("Start Auto Documents Before Publish Event for Document {0}", content.Id));

            try
            {
                if (!HasDateChanged(contentService, content))
                    return;

                DateTime itemDate = Convert.ToDateTime(content.Properties[ItemDateProperty].Value);
                IContent parent = GetParentDocument(content, contentService);

                if (parent == null)
                    return;

                var yearContent = GetOrCreateContent(contentService, parent, itemDate.Year.ToString(CultureInfo.InvariantCulture));
                var monthContent = GetOrCreateContent(contentService, yearContent, itemDate.ToString("MM"));
                var parentContent = monthContent;

                if (CreateDayDocuments)
                {
                    var dayContent = GetOrCreateContent(contentService, monthContent, itemDate.ToString("dd"));
                    parentContent = dayContent;
                }

                if (parentContent != null && content.ParentId != parentContent.Id)
                {
                    contentService.Move(content, parentContent.Id);
                    LogHelper.Debug<AutoDocuments>(string.Format("Item {0} moved uder content {1}", content.Id, parentContent.Id));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<AutoDocuments>(string.Format("Error in Auto Documents Before Publish: {0}", ex.Message), ex);
            }
        }

        private IContent GetParentDocument(IContent content, IContentService contentService)
        {
            if (content == null || (content.ContentType.Alias != DateDocumentType && !ItemDocumentTypes.Contains(content.ContentType.Alias)))
                return content;

            var parent = contentService.GetById(content.ParentId);
            return GetParentDocument(parent, contentService);
        }

        private IContent GetOrCreateContent(IContentService contentService, IContent parentContent, string name)
        {
            IContent content = contentService.GetChildren(parentContent.Id).FirstOrDefault(c => c.Name == name);

            if (content == null)
            {
                content = contentService.CreateContent(name, parentContent.Id, DateDocumentType);
                contentService.Save(content);
                contentService.Publish(content);
            }

            return content;
        }

        private bool HasDateChanged(IContentService contentService, IContent content)
        {
            var versions = contentService.GetVersions(content.Id).OrderBy(v => v.UpdateDate).ToList();
            bool dateHasChanged = true;
            DateTime itemDate = Convert.ToDateTime(content.Properties[ItemDateProperty].Value);

            if (versions.Count > 1)
            {
                var previousVersion = versions[versions.Count - 2];
                DateTime oldItemDate = Convert.ToDateTime(previousVersion.Properties[ItemDateProperty].Value);
                dateHasChanged = itemDate != oldItemDate;
            }

            return dateHasChanged;
        }
    }
}
