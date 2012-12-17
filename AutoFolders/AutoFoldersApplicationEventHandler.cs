using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.web;
using Umbraco.Core;
using Umbraco.Web;

namespace AutoFolders
{
    public class AutoFoldersApplicationEventHandler : IApplicationEventHandler
    {
        private AutoFolders _autoFolders;

        public void OnApplicationInitialized(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarted(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
            var itemDocType = ConfigurationManager.AppSettings["autofolders:ItemDocType"];
            var dateFolderDocType = ConfigurationManager.AppSettings["autofolders:DateFolderDocType"];
            var itemDateProperty = ConfigurationManager.AppSettings["autofolders:ItemDateProperty"];

            List<string> itemDocTypes = null;
            bool createDayFolders = false;

            if (!string.IsNullOrEmpty(itemDocType))
                itemDocTypes = itemDocType.Split(',').ToList();

            string createDayFoldersSetting = ConfigurationManager.AppSettings["autofolders:CreateDayFolders"];

            if (!string.IsNullOrEmpty(createDayFoldersSetting))
                bool.TryParse(createDayFoldersSetting, out createDayFolders);

            if (itemDocTypes == null || itemDocTypes.Count == 0 || string.IsNullOrEmpty(dateFolderDocType) || string.IsNullOrEmpty(itemDateProperty))
            {
                Log.Add(LogTypes.Debug, 0, string.Format("Date Folders configuration invalid, ItemDocType:{0} DateFolderDocType:{1} ItemDateProperty:{2}", itemDocType, dateFolderDocType, itemDateProperty));
                return;
            }

            _autoFolders = new AutoFolders(itemDocTypes, itemDateProperty, dateFolderDocType, createDayFolders);

            Document.New += DocumentNew;
            Document.BeforePublish += DocumentBeforePublish;
        }
        
        private void DocumentNew(Document document, NewEventArgs e)
        {
            _autoFolders.SetDocumentDate(document);
        }

        private void DocumentBeforePublish(Document document, PublishEventArgs e)
        {
            _autoFolders.BeforeDocumentPublish(document);
        }
    }
}
