using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic;
using umbraco.cms.businesslogic.web;
using Umbraco.Core;
using Umbraco.Web;

namespace AutoDocuments
{
    public class AutoDocumentsApplicationEventHandler : IApplicationEventHandler
    {
        private AutoDocuments _autoDocuments;

        public void OnApplicationInitialized(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarting(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
        }

        public void OnApplicationStarted(UmbracoApplication httpApplication, ApplicationContext applicationContext)
        {
            var itemDocumentTypes = ConfigurationManager.AppSettings["AutoDocuments:ItemDocumentTypes"];
            var dateDocumentType = ConfigurationManager.AppSettings["AutoDocuments:DateDocumentType"];
            var itemDatePropertyAlias = ConfigurationManager.AppSettings["AutoDocuments:ItemDatePropertyAlias"];

            List<string> itemDocTypes = null;
            bool createDayDocuments = false;

            if (!string.IsNullOrEmpty(itemDocumentTypes))
                itemDocTypes = itemDocumentTypes.Split(',').ToList();

            string createDayDocumentsSetting = ConfigurationManager.AppSettings["AutoDocuments:CreateDayDocuments"];

            if (!string.IsNullOrEmpty(createDayDocumentsSetting))
                bool.TryParse(createDayDocumentsSetting, out createDayDocuments);

            if (itemDocTypes == null || itemDocTypes.Count == 0 || string.IsNullOrEmpty(dateDocumentType) || string.IsNullOrEmpty(itemDatePropertyAlias))
            {
                Log.Add(LogTypes.Debug, 0, string.Format("Auto Documents configuration invalid, ItemDocumentTypes:{0} DateDocumentType:{1} ItemDatePropertyAlias:{2}", itemDocumentTypes, dateDocumentType, itemDatePropertyAlias));
                return;
            }

            _autoDocuments = new AutoDocuments(itemDocTypes, itemDatePropertyAlias, dateDocumentType, createDayDocuments);

            Document.New += DocumentNew;
            Document.BeforePublish += DocumentBeforePublish;
        }
        
        private void DocumentNew(Document document, NewEventArgs e)
        {
            _autoDocuments.SetDocumentDate(document);
        }

        private void DocumentBeforePublish(Document document, PublishEventArgs e)
        {
            _autoDocuments.BeforeDocumentPublish(document);
        }
    }
}
