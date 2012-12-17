using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using umbraco;
using umbraco.BusinessLogic;
using umbraco.NodeFactory;
using umbraco.cms.businesslogic.web;

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

        public void SetDocumentDate(Document document)
        {
            if (!ItemDocumentTypes.Contains(document.ContentType.Alias))
                return;
            if (document.getProperty(ItemDateProperty) == null)
                return;

            document.getProperty(ItemDateProperty).Value = document.CreateDateTime.Date;
        }

        public void BeforeDocumentPublish(Document document)
        {
            if (!ItemDocumentTypes.Contains(document.ContentType.Alias) || document.Parent == null)
                return;
            if (document.getProperty(ItemDateProperty) == null || document.getProperty(ItemDateProperty).Value == null)
                return;

            Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Start Auto Documents Before Publish Event for Document {0}", document.Id));

            try
            {
                if (!HasDateChanged(document))
                    return;

                DateTime itemDate = Convert.ToDateTime(document.getProperty(ItemDateProperty).Value);
                Node parent = GetParentDocument(document);

                if (parent == null)
                    return;

                var yearNode = GetOrCreateNode(document.User, parent, itemDate.Year.ToString(CultureInfo.InvariantCulture));
                var monthNode = GetOrCreateNode(document.User, yearNode, itemDate.ToString("MM"));
                var parentNode = monthNode;

                if (CreateDayDocuments)
                {
                    var dayNode = GetOrCreateNode(document.User, monthNode, itemDate.ToString("dd"));
                    parentNode = dayNode;
                }

                if (parentNode != null && document.Parent.Id != parentNode.Id)
                {
                    document.Move(parentNode.Id);
                    Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Item {0} moved uder node {1}", document.Id, parentNode.Id));
                }
            }
            catch (Exception ex)
            {
                Log.Add(LogTypes.Error, document.User, document.Id, string.Format("Error in Auto Documents Before Publish: {0}", ex.Message));
            }

            library.RefreshContent();
        }

        private Node GetParentDocument(Document document)
        {
            var parent = new Node(document.Parent.Id);

            while (parent != null && parent.NodeTypeAlias == DateDocumentType)
            {
                parent = parent.Parent == null ? null : new Node(parent.Parent.Id);
            }

            if (parent == null)
                Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Unable to determine parent document for {0}", document.Id));

            return parent;
        }

        private Node GetOrCreateNode(User user, Node parentNode, string nodeName)
        {
            Node node = parentNode.Children.Cast<Node>().Where(n => n.Name == nodeName).Select(n => new Node(n.Id)).FirstOrDefault();

            if (node == null)
            {
                Document document = Document.MakeNew(nodeName, DocumentType.GetByAlias(DateDocumentType), user, parentNode.Id);
                document.Publish(user);
                library.UpdateDocumentCache(document.Id);
                node = new Node(document.Id);
            }

            return node;
        }
        
        private bool HasDateChanged(Document document)
        {
            DocumentVersionList[] versions = document.GetVersions();
            bool dateHasChanged = true;
            DateTime itemDate = Convert.ToDateTime(document.getProperty(ItemDateProperty).Value);

            if (versions.Length > 1)
            {
                Guid version = versions[versions.Length - 2].Version;
                DateTime oldItemDate = Convert.ToDateTime(new Document(document.Id, version).getProperty(ItemDateProperty).Value);
                dateHasChanged = itemDate != oldItemDate;
            }

            return dateHasChanged;
        }
    }
}
