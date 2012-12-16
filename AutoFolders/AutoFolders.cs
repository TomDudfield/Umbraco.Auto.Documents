using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using umbraco;
using umbraco.BusinessLogic;
using umbraco.NodeFactory;
using umbraco.cms.businesslogic.web;

namespace AutoFolders
{
    public class AutoFolders
    {
        public AutoFolders(List<string> itemDocTypes, string itemDateProperty, string dateFolderDocType, bool createDayFolders)
        {
            ItemDocTypes = itemDocTypes;
            ItemDateProperty = itemDateProperty;
            DateFolderDocType = dateFolderDocType;
            CreateDayFolders = createDayFolders;
        }

        public List<string> ItemDocTypes { get; private set; }

        public string ItemDateProperty { get; private set; }

        public string DateFolderDocType { get; private set; }

        public bool CreateDayFolders { get; private set; }

        public void SetDocumentDate(Document document)
        {
            if (!ItemDocTypes.Contains(document.ContentType.Alias))
                return;
            if (document.getProperty(ItemDateProperty) == null)
                return;

            document.getProperty(ItemDateProperty).Value = document.CreateDateTime.Date;
        }

        public void BeforeDocumentPublish(Document document)
        {
            if (!ItemDocTypes.Contains(document.ContentType.Alias) || document.Parent == null)
                return;
            if (document.getProperty(ItemDateProperty) == null || document.getProperty(ItemDateProperty).Value == null)
                return;

            Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Start Date Folders Before Publish Event for Document {0}", document.Id));

            try
            {
                if (!HasDateChanged(document))
                    return;

                DateTime itemDate = Convert.ToDateTime(document.getProperty(ItemDateProperty).Value);
                Node parent = GetParentDocument(document);

                if (parent == null)
                    return;

                var year = itemDate.Year.ToString(CultureInfo.InvariantCulture);
                Node yearNode = parent.Children.Cast<Node>().Where(y => y.Name == year).Select(y => new Node(y.Id)).FirstOrDefault();

                if (yearNode == null)
                {
                    yearNode = CreateFolder(document, year, parent);
                }

                var month = itemDate.ToString("MM");
                Node monthNode = yearNode.Children.Cast<Node>().Where(m => m.Name == month).Select(m => new Node(m.Id)).FirstOrDefault();

                if (monthNode == null)
                {
                    Document monthDocument = Document.MakeNew(month, DocumentType.GetByAlias(DateFolderDocType), document.User, yearNode.Id);
                    monthDocument.Publish(document.User);
                    library.UpdateDocumentCache(monthDocument.Id);
                    monthNode = new Node(monthDocument.Id);
                }

                Node dayNode = null;

                if (CreateDayFolders)
                {
                    var day = itemDate.ToString("dd");
                    dayNode = monthNode.Children.Cast<Node>().Where(d => d.Name == day).Select(d => new Node(d.Id)).FirstOrDefault();

                    if (dayNode == null)
                    {
                        Document dayDocument = Document.MakeNew(day, DocumentType.GetByAlias(DateFolderDocType), document.User, monthNode.Id);
                        dayDocument.Publish(document.User);
                        library.UpdateDocumentCache(dayDocument.Id);
                        dayNode = new Node(dayDocument.Id);
                    }
                }

                var parentNode = monthNode;

                if (CreateDayFolders)
                    parentNode = dayNode;

                if (parentNode != null && document.Parent.Id != parentNode.Id)
                {
                    document.Move(parentNode.Id);
                    Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Item {0} moved uder node {1}", document.Id, parentNode.Id));
                }
            }
            catch (Exception ex)
            {
                Log.Add(LogTypes.Error, document.User, document.Id, string.Format("Error in Date Folders Before Publish: {0}", ex.Message));
            }

            library.RefreshContent();
        }

        private Node GetParentDocument(Document document)
        {
            var parent = new Node(document.Parent.Id);

            while (parent != null && parent.NodeTypeAlias == DateFolderDocType)
            {
                parent = parent.Parent == null ? null : new Node(parent.Parent.Id);
            }

            if (parent == null)
                Log.Add(LogTypes.Debug, document.User, document.Id, string.Format("Unable to determine parent document for {0}", document.Id));

            return parent;
        }

        private Node CreateFolder(Document document, string year, Node parent)
        {
            Node yearNode;
            Document yearDocument = Document.MakeNew(year, DocumentType.GetByAlias(DateFolderDocType), document.User, parent.Id);
            yearDocument.Publish(document.User);
            library.UpdateDocumentCache(yearDocument.Id);
            yearNode = new Node(yearDocument.Id);
            return yearNode;
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
