using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using umbraco;
using umbraco.NodeFactory;

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

            content.Properties[ItemDateProperty].Value = content.CreateDate.Date;
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
                Node parent = GetParentDocument(new Node(content.ParentId));

                if (parent == null)
                    return;

                var yearNode = GetOrCreateNode(contentService, parent, itemDate.Year.ToString(CultureInfo.InvariantCulture));
                var monthNode = GetOrCreateNode(contentService, yearNode, itemDate.ToString("MM"));
                var parentNode = monthNode;

                if (CreateDayDocuments)
                {
                    var dayNode = GetOrCreateNode(contentService, monthNode, itemDate.ToString("dd"));
                    parentNode = dayNode;
                }

                if (parentNode != null && content.ParentId != parentNode.Id)
                {
                    contentService.Move(content, parentNode.Id);
                    LogHelper.Debug<AutoDocuments>(string.Format("Item {0} moved uder node {1}", content.Id, parentNode.Id));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<AutoDocuments>(string.Format("Error in Auto Documents Before Publish: {0}", ex.Message), ex);
            }

            library.RefreshContent();
        }

        private Node GetParentDocument(Node node)
        {
            if (node == null || node.Parent == null || node.NodeTypeAlias != DateDocumentType)
                return node;

            var parent = new Node(node.Parent.Id);
            return GetParentDocument(parent);
        }

        private Node GetOrCreateNode(IContentService contentService, Node parentNode, string nodeName)
        {
            Node node = parentNode.Children.Cast<Node>().Where(n => n.Name == nodeName).Select(n => new Node(n.Id)).FirstOrDefault();

            if (node == null)
            {
                var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(DateDocumentType);
                var content = new Content(nodeName, parentNode.Id, contentType);
                contentService.Publish(content);
                library.UpdateDocumentCache(content.Id);
                node = new Node(content.Id);
            }

            return node;
        }

        private bool HasDateChanged(IContentService contentService, IContent content)
        {
            var versions = contentService.GetVersions(content.Id).ToList();
            bool dateHasChanged = true;
            DateTime itemDate = Convert.ToDateTime(content.Properties[ItemDateProperty].Value);

            if (versions.Any())
            {
                Guid version = versions[versions.Count() - 2].Version;
                var previousVersion = contentService.GetByVersion(version);
                DateTime oldItemDate = Convert.ToDateTime(previousVersion.Properties[ItemDateProperty].Value);
                dateHasChanged = itemDate != oldItemDate;
            }

            return dateHasChanged;
        }
    }
}
